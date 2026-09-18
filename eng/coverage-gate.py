#!/usr/bin/env python3
"""Fail a build whose code coverage has dropped below a floor, and describe the result.

CI runs this over the cobertura reports produced by `dotnet test --coverage
--coverage-output-format cobertura`. It is deliberately small and dependency-free: the coverage
extension that writes the report has no threshold option, ReportGenerator's is a paid feature, and
a threshold that is implemented somewhere you cannot read is a threshold you cannot trust.

What is measured
----------------
Two numbers per assembly, taken from the report rather than recomputed:

  * line coverage   - lines executed at least once
  * branch coverage - conditional branches taken at least once

The report is trusted for those, because it is the same tool that produced the data. The totals use
the counts on the root <coverage> element, summed across every report file, so a test project that
multi-targets (net8.0 and net10.0 here, each producing its own report) is measured as one run rather
than as two averages of two.

Per-assembly rows use the worst rate across the report files. A target framework that is compiled
but whose code paths are never exercised should not be able to hide behind the framework that is.

Gate semantics
--------------
  * every assembly in the report must clear the per-assembly floor, which defaults to the overall
    thresholds. A new project therefore starts out gated instead of silently uncovered;
  * a configured assembly that is missing from the report is an error, not a skip - that is what a
    rename or a project that stopped being tested looks like;
  * finding no reports at all is an error. A coverage gate that passes when it measures nothing is
    worse than no gate.

Exit codes
----------
  0  every threshold is met
  1  a threshold is not met
  2  coverage could not be measured (no reports, unreadable XML, no lines)

Usage
-----
  eng/coverage-gate.py --reports 'TestResults/**/*.cobertura.xml' \\
      --line 80 --branch 70 \\
      --assembly TypeSafe.Sdk:78:70 \\
      --assembly TypeSafe.Sdk.DependencyInjection:90:90 \\
      --markdown coverage-summary.md
"""

from __future__ import annotations

import argparse
import glob
import pathlib
import sys
import xml.etree.ElementTree as ET

EXIT_PASS = 0
EXIT_BELOW_THRESHOLD = 1
EXIT_NOT_MEASURED = 2


class NotMeasured(Exception):
    """Coverage could not be read, so no verdict can be given."""


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        prog="coverage-gate.py",
        description="Fail when code coverage is below a threshold.",
        allow_abbrev=False,
    )
    parser.add_argument(
        "--reports",
        action="append",
        required=True,
        metavar="GLOB",
        help="Glob for cobertura reports. Repeatable.",
    )
    parser.add_argument(
        "--line",
        type=float,
        required=True,
        metavar="PERCENT",
        help="Minimum line coverage for the whole run, and the default floor per assembly.",
    )
    parser.add_argument(
        "--branch",
        type=float,
        required=True,
        metavar="PERCENT",
        help="Minimum branch coverage for the whole run, and the default floor per assembly.",
    )
    parser.add_argument(
        "--assembly",
        action="append",
        default=[],
        metavar="NAME:LINE:BRANCH",
        help=(
            "Per-assembly floor, overriding the overall thresholds for that assembly. "
            "Repeatable. Naming an assembly the report does not contain is an error."
        ),
    )
    parser.add_argument(
        "--markdown",
        metavar="PATH",
        help="Also write the markdown summary to this path.",
    )
    return parser.parse_args(argv)


def parse_assembly_floors(specs: list[str], default_line: float, default_branch: float):
    floors = {}
    for spec in specs:
        parts = spec.rsplit(":", 2)
        if len(parts) != 3:
            raise NotMeasured(f"--assembly expects NAME:LINE:BRANCH, got {spec!r}")
        name, line, branch = parts
        try:
            floors[name] = (float(line), float(branch))
        except ValueError as exc:
            raise NotMeasured(f"--assembly {spec!r} has a non-numeric threshold") from exc
    return floors


def percent(covered: int, valid: int) -> float:
    return 100.0 * covered / valid if valid else 0.0


def read_reports(patterns: list[str]):
    """Return (total, per_assembly) where per_assembly maps a name to its worst rate seen."""
    paths = sorted({p for pattern in patterns for p in glob.glob(pattern, recursive=True)})
    if not paths:
        raise NotMeasured(f"no coverage reports matched {patterns}")

    total = {"lines_covered": 0, "lines_valid": 0, "branches_covered": 0, "branches_valid": 0}
    per_assembly: dict[str, dict[str, float]] = {}

    for path in paths:
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError as exc:
            raise NotMeasured(f"{path} is not valid XML: {exc}") from exc

        if root.tag != "coverage":
            raise NotMeasured(f"{path} has a <{root.tag}> root, expected <coverage>")

        for key, attribute in (
            ("lines_covered", "lines-covered"),
            ("lines_valid", "lines-valid"),
            ("branches_covered", "branches-covered"),
            ("branches_valid", "branches-valid"),
        ):
            value = root.get(attribute)
            if value is None:
                raise NotMeasured(f"{path} has no {attribute} attribute on <coverage>")
            total[key] += int(value)

        for package in root.findall("packages/package"):
            name = package.get("name")
            line_rate = package.get("line-rate")
            branch_rate = package.get("branch-rate")
            if not name or line_rate is None or branch_rate is None:
                raise NotMeasured(f"{path} has a <package> without a name or rates")
            worst = per_assembly.setdefault(
                name, {"line": float(line_rate) * 100.0, "branch": float(branch_rate) * 100.0}
            )
            worst["line"] = min(worst["line"], float(line_rate) * 100.0)
            worst["branch"] = min(worst["branch"], float(branch_rate) * 100.0)

    if total["lines_valid"] == 0:
        raise NotMeasured(f"the reports matched by {patterns} contain no lines")
    return total, per_assembly, len(paths)


def evaluate(total, per_assembly, floors, default_line, default_branch):
    """Return a list of failing checks, each already formatted for a human."""
    failures = []

    total_line = percent(total["lines_covered"], total["lines_valid"])
    total_branch = percent(total["branches_covered"], total["branches_valid"])
    if total_line < default_line:
        failures.append(f"line coverage {total_line:.2f}% is below {default_line:g}%")
    if total_branch < default_branch:
        failures.append(f"branch coverage {total_branch:.2f}% is below {default_branch:g}%")

    for name, floor in floors.items():
        if name not in per_assembly:
            failures.append(
                f"{name} has a configured threshold but is missing from the coverage report"
            )

    for name, rates in sorted(per_assembly.items()):
        line_floor, branch_floor = floors.get(name, (default_line, default_branch))
        if rates["line"] < line_floor:
            failures.append(f"{name} line coverage {rates['line']:.2f}% is below {line_floor:g}%")
        if rates["branch"] < branch_floor:
            failures.append(
                f"{name} branch coverage {rates['branch']:.2f}% is below {branch_floor:g}%"
            )

    return failures


def markdown(total, per_assembly, floors, default_line, default_branch, failures, report_count):
    total_line = percent(total["lines_covered"], total["lines_valid"])
    total_branch = percent(total["branches_covered"], total["branches_valid"])
    verdict = "pass" if not failures else "fail"

    lines = [
        "<!-- coverage-gate -->",
        "## Code coverage",
        "",
        f"Measured from {report_count} cobertura report(s).",
        "",
        "| Assembly | Line | Branch | Floor |",
        "| --- | ---: | ---: | --- |",
    ]
    for name, rates in sorted(per_assembly.items()):
        line_floor, branch_floor = floors.get(name, (default_line, default_branch))
        lines.append(
            f"| `{name}` | {rates['line']:.2f}% | {rates['branch']:.2f}% | "
            f"{line_floor:g}% / {branch_floor:g}% |"
        )
    lines.append(
        f"| **All assemblies (combined)** | **{total_line:.2f}%** | **{total_branch:.2f}%** | "
        f"{default_line:g}% / {default_branch:g}% |"
    )
    lines += [
        "",
        f"Coverage gate: **{verdict}**.",
    ]
    if failures:
        lines += ["", "Below threshold:", ""]
        lines += [f"- {failure}" for failure in failures]
    return "\n".join(lines) + "\n"


def main(argv: list[str]) -> int:
    args = parse_args(argv)

    try:
        floors = parse_assembly_floors(args.assembly, args.line, args.branch)
        total, per_assembly, report_count = read_reports(args.reports)
    except NotMeasured as exc:
        print(f"::error::coverage could not be measured: {exc}", file=sys.stderr)
        print(f"coverage could not be measured: {exc}", file=sys.stderr)
        return EXIT_NOT_MEASURED

    failures = evaluate(total, per_assembly, floors, args.line, args.branch)
    summary = markdown(
        total, per_assembly, floors, args.line, args.branch, failures, report_count
    )
    print(summary, end="")
    if args.markdown:
        pathlib.Path(args.markdown).write_text(summary, encoding="utf-8")

    if failures:
        for failure in failures:
            print(f"::error::{failure}", file=sys.stderr)
        return EXIT_BELOW_THRESHOLD
    return EXIT_PASS


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
