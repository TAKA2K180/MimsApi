using System.Collections.Generic;

namespace MimsApi.Core.services
{
    /// <summary>
    /// V1 API Response DTOs for product CRUD and hierarchy operations
    /// These DTOs are used to structure responses for the V1 API endpoints
    /// </summary>

    #region Hierarchy Response DTOs

    /// <summary>
    /// V1 Response: Product with hierarchy information and statistics
    /// </summary>
    public class V1ProductHierarchyResponse
    {
        public V1ProductInfo Product { get; set; }
        public List<V1PackagingNodeResponse> Packaging { get; set; }
        public object Statistics { get; set; }
    }

    /// <summary>
    /// V1 Response: Basic product information for hierarchy
    /// </summary>
    public class V1ProductInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// V1 Response: Packaging node in hierarchy with children
    /// </summary>
    public class V1PackagingNodeResponse
    {
        public int Id { get; set; }
        public string PackagingName { get; set; }
        public string PackagingType { get; set; }
        public int? ParentPackagingId { get; set; }
        public int NestingLevel { get; set; }
        public List<V1PackagingNodeResponse> Children { get; set; }
    }

    #endregion

    #region Item Location Response DTOs

    /// <summary>
    /// V1 Response: Packaging location for a specific item
    /// Used when searching for all packaging that contains a specific item
    /// </summary>
    public class V1PackagingLocationResponse
    {
        public int PackagingId { get; set; }
        public string PackagingName { get; set; }
        public string PackagingType { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int? ParentPackagingId { get; set; }
        public int NestingLevel { get; set; }
        public int Quantity { get; set; }
    }

    #endregion

    #region Packaging Items Response DTOs

    /// <summary>
    /// V1 Response: All items in a packaging with nested details
    /// </summary>
    public class V1PackagingItemsResponse
    {
        public int PackagingId { get; set; }
        public int NestedPackagings { get; set; }
        public int TotalItems { get; set; }
        public List<V1PackagingItemDetail> DetailsByPackaging { get; set; }
    }

    /// <summary>
    /// V1 Response: Items in a specific packaging
    /// </summary>
    public class V1PackagingItemDetail
    {
        public int PackagingId { get; set; }
        public List<V1ItemDetail> Items { get; set; }
    }

    /// <summary>
    /// V1 Response: Single item detail
    /// </summary>
    public class V1ItemDetail
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }

    #endregion

    #region Hierarchy Validation Response DTOs

    /// <summary>
    /// V1 Response: Result of hierarchy validation
    /// </summary>
    public class V1HierarchyValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }

    #endregion

    #region Packaging Statistics Response DTOs

    /// <summary>
    /// V1 Response: Aggregated packaging statistics for a product
    /// </summary>
    public class V1PackagingStatisticsResponse
    {
        public int ProductId { get; set; }
        public int TotalPackagings { get; set; }
        public int RootPackagings { get; set; }
        public int MaxNestingLevel { get; set; }
        public int UniqueItems { get; set; }
        public int TotalItemQuantity { get; set; }
    }

    #endregion
}
