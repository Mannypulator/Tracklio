using System;

namespace Tracklio.Shared.Domain.Dto;

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Data { get; set; } = null!;
    public PaginationInfo Pagination { get; set; } = null!;
}

public class PaginationInfo
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
}
