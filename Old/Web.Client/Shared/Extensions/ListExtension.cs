namespace Web.Client.Shared.Extensions;

public static class ListExtension
{
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(_ => Guid.NewGuid());
    }
}