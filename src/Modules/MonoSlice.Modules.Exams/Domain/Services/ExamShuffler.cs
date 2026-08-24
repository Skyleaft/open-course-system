namespace MonoSlice.Modules.Exams.Domain.Services;

public static class ExamShuffler
{
    public static List<T> Shuffle<T>(IEnumerable<T> source, int seed)
    {
        var list = source.ToList();
        var rng = new Random(seed);
        var n = list.Count;

        for (var i = n - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}
