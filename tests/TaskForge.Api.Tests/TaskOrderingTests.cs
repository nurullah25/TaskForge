using TaskForge.Api.Features.Tasks;

namespace TaskForge.Api.Tests;

// Plain unit tests: no database needed for the position maths.
public class TaskOrderingTests
{
    [Fact]
    public void First_task_in_an_empty_column_gets_the_default_gap()
    {
        Assert.Equal(1000, TaskOrdering.Between(null, null));
    }

    [Fact]
    public void Task_dropped_at_the_top_goes_one_gap_above_the_first_card()
    {
        Assert.Equal(0, TaskOrdering.Between(null, 1000));
    }

    [Fact]
    public void Task_dropped_at_the_bottom_goes_one_gap_below_the_last_card()
    {
        Assert.Equal(3000, TaskOrdering.Between(2000, null));
    }

    [Fact]
    public void Task_dropped_between_two_cards_gets_the_midpoint()
    {
        Assert.Equal(1500, TaskOrdering.Between(1000, 2000));
    }

    [Fact]
    public void Rebalance_is_only_needed_once_the_neighbours_are_practically_equal()
    {
        Assert.False(TaskOrdering.NeedsRebalance(1000, 2000));
        Assert.False(TaskOrdering.NeedsRebalance(null, 1000));
        Assert.True(TaskOrdering.NeedsRebalance(1000, 1000.001));
    }

    [Fact]
    public void Repeated_drops_in_the_same_spot_stay_in_order_until_a_rebalance_is_needed()
    {
        double above = 1000;
        double below = 2000;
        var rebalances = 0;

        for (var i = 0; i < 100; i++)
        {
            if (TaskOrdering.NeedsRebalance(above, below))
            {
                // What the service does: spread the column out again.
                (above, below) = (1000, 2000);
                rebalances++;
            }

            var position = TaskOrdering.Between(above, below);
            Assert.InRange(position, above, below);
            below = position;
        }

        Assert.True(rebalances > 0, "a rebalance should have been required");
    }
}
