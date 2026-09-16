<!-- markdownlint-disable MD041 -- a pull request template starts with its first section, not a title -->
<!--
  Thanks for contributing. Keep this description about the change itself: what it does, why it is
  needed, and anything a reviewer could not work out from the diff. Delete the comments as you go.
-->

## Summary

<!-- One or two sentences. Then link the issue this closes, if there is one. -->

Fixes #

## Motivation

<!-- Why is this change needed? What breaks or is impossible today? -->

## What changed

<!--
  The important parts, especially anything a caller can observe: new public API, changed
  behaviour, changed defaults, or a new dependency.
-->

## Type of change

- [ ] Bug fix (non-breaking)
- [ ] New feature (non-breaking)
- [ ] Breaking change (existing behaviour or public API changes)
- [ ] Documentation only
- [ ] Build, CI, or dependencies

## Checklist

- [ ] **Build** — `dotnet build TypeSafe.slnx` succeeds with no warnings.
- [ ] **Tests** — added or updated tests covering the change, including the failure paths;
      `dotnet test --solution TypeSafe.slnx` passes.
- [ ] **Formatting** — `dotnet format TypeSafe.slnx --verify-no-changes` is clean.
- [ ] **Public API** — `PublicAPI.Unshipped.txt` updated through the RS0016 code fix if the
      public surface changed, and `PublicAPI.Shipped.txt` left alone.
- [ ] **XML docs** — every new or changed public member has `<summary>` and `<param>` docs that
      describe behaviour, not just the signature.
- [ ] **CHANGELOG** — an entry exists under `## [Unreleased]`.
- [ ] **Dependencies** — no new package, or an issue where one was agreed, with the licence and
      target framework justification.
- [ ] **Docs** — `docs/`, the README, and any affected code sample updated; every sample touched
      compiles.
- [ ] **Breaking change** — described above with a migration note, and labelled `breaking-change`.
- [ ] **Commits** — follow Conventional Commits (`feat:`, `fix:`, `docs:`, …).

## Notes for reviewers

<!--
  Anything you are unsure about, alternatives you rejected, or follow-up work you deliberately
  left out of this pull request.
-->
