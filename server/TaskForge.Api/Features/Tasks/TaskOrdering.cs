namespace TaskForge.Api.Features.Tasks;

// Tasks are ordered inside a column by a Position number instead of an index, so moving
// a task updates one row rather than renumbering the whole column. A task dropped between
// two others gets the midpoint of their positions.
public static class TaskOrdering
{
    public const double Gap = 1000;

    // Doubles run out of room after roughly 50 splits in the same spot. Long before that,
    // the column is renumbered with even gaps again (see TaskService.MoveAsync).
    public const double MinimumGap = 0.01;

    public static double Between(double? above, double? below) => (above, below) switch
    {
        (null, null) => Gap,
        (null, { } next) => next - Gap,
        ({ } previous, null) => previous + Gap,
        var (previous, next) => (previous.Value + next.Value) / 2
    };

    public static bool NeedsRebalance(double? above, double? below) =>
        above.HasValue && below.HasValue && below.Value - above.Value < MinimumGap;
}
