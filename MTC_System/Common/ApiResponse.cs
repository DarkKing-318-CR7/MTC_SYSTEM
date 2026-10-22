using System;
using System.Collections.Generic;

namespace MTC_System.Common
{
    /// <summary>
    /// Chuẩn hóa định dạng JSON phản hồi API cho toàn bộ Front-end
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Thành công")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = null
            };
        }

        public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Chuẩn hóa kết quả phân trang cho danh sách (Grid, Table)
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static PagedResult<T> Create(List<T> items, int totalCount, int pageIndex, int pageSize)
        {
            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }
    }
}
