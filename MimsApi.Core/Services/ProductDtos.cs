using System;
using System.Collections.Generic;

namespace MimsApi.Core.services
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public IEnumerable<PackagingHeaderDto> PackagingHeaders { get; set; }
    }

    public class CreateProductRequest
    {
        public string ProductName { get; set; }
    }

    public class UpdateProductRequest
    {
        public string ProductName { get; set; }
    }

    public class PackagingHeaderDto
    {
        public int Id { get; set; }
        public string PackagingName { get; set; }
        public string PackagingType { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class ApiErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string[]> Errors { get; set; }
    }
}
