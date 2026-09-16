namespace TypeSafe;

/// <summary>
/// One Score answer and the weight it carries in a composite judgment.
/// </summary>
/// <param name="Answer">The score answer.</param>
/// <param name="Weight">
/// The weight for this factor. Weights are relative, so only their ratios matter: <c>3</c> and
/// <c>1</c> mean the same thing as <c>0.75</c> and <c>0.25</c>.
/// </param>
public readonly record struct WeightedScore(ScoreAnswer Answer, double Weight);
