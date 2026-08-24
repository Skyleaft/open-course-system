using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;

namespace CoreLib.Common;

public class PaginatedResponse<T>
{
    public PaginatedResponse(IEnumerable<T> items, int page, int count, int pageSize)
    {
        TotalItems = count;
        CurrentPage = page;
        TotalPage = (int)Math.Ceiling(count / (double)pageSize);
        Items = items;
    }

    public PaginatedResponse()
    {
    }

    public int CurrentPage { get; set; }
    public int TotalPage { get; set; }
    public int TotalItems { get; set; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPage;
    public IEnumerable<T> Items { get; init; } = new List<T>();
}

public static class PaginatedListHelper
{
    public const int DefaultPageSize = 15;
    public const int DefaultCurrentPage = 1;

    public static async Task<PaginatedResponse<T>> ToPaginatedList<T>(this IQueryable<T> source, int currentPage,
        int pageSize, string orderBy, int direction, CancellationToken ct)
    {
        currentPage = currentPage > 0 ? currentPage : DefaultCurrentPage;
        pageSize = pageSize > 0 ? pageSize : DefaultPageSize;
        var count = source.Count();
        var items = new List<T>();
        if (!string.IsNullOrEmpty(orderBy))
        {
            var dir = direction == 1 ? " asc" : " desc";
            items = await source
                .OrderBy(orderBy + dir)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }
        else
        {
            items = await source.Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        return new PaginatedResponse<T>(items, currentPage, count, pageSize);
    }

    public static async Task<PaginatedResponse<T>> ToPagingList<T>(this IEnumerable<T> source, int currentPage,
        int pageSize)
    {
        currentPage = currentPage > 0 ? currentPage : DefaultCurrentPage;
        pageSize = pageSize > 0 ? pageSize : DefaultPageSize;
        var count = source.Count();
        var items = new List<T>();
        items = source.Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResponse<T>(items, currentPage, count, pageSize);
    }
}