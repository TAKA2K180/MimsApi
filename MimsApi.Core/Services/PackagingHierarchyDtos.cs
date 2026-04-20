using System.Collections.Generic;

namespace MimsApi.Core.services
{
    /// <summary>
    /// Enhanced packaging header DTO with hierarchy and details
    /// </summary>
    public class PackagingHierarchyDto
    {
        public int Id { get; set; }
        public string PackagingName { get; set; }
        public string PackagingType { get; set; }
        public int? ParentPackagingId { get; set; }
        public IEnumerable<PackagingDetailItemDto> Items { get; set; }
        public IEnumerable<PackagingHierarchyDto> ChildPackagings { get; set; }
    }

    /// <summary>
    /// Packaging detail with item information
    /// </summary>
    public class PackagingDetailItemDto
    {
        public int Id { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Enhanced product DTO with full packaging hierarchy
    /// </summary>
    public class ProductHierarchyDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public IEnumerable<PackagingHierarchyDto> PackagingHeaders { get; set; }
    }
}
