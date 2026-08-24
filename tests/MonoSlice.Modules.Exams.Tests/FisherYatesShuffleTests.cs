using MonoSlice.Modules.Exams.Domain.Services;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class FisherYatesShuffleTests
{
    [Fact]
    public void Shuffle_ShouldBeDeterministicWithSameSeed()
    {
        var items = new List<string> { "Q1", "Q2", "Q3", "Q4", "Q5", "Q6", "Q7", "Q8" };
        var seed = 42891;

        var shuffled1 = ExamShuffler.Shuffle(items, seed);
        var shuffled2 = ExamShuffler.Shuffle(items, seed);

        Assert.Equal(shuffled1, shuffled2);
        Assert.Equal(items.Count, shuffled1.Count);
        Assert.All(items, item => Assert.Contains(item, shuffled1));
    }

    [Fact]
    public void Shuffle_ShouldProduceDifferentOrderWithDifferentSeeds()
    {
        var items = Enumerable.Range(1, 20).Select(i => $"Question {i}").ToList();

        var shuffled1 = ExamShuffler.Shuffle(items, 1111);
        var shuffled2 = ExamShuffler.Shuffle(items, 9999);

        Assert.NotEqual(shuffled1, shuffled2);
    }
}
