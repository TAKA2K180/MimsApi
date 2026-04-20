using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MimsApi.Core.services
{
    /// <summary>
    /// V2 API Response DTOs for enhanced product hierarchy operations
    /// These DTOs are used to structure responses for the enhanced V2 API endpoints
    /// </summary>

    /// <summary>
    /// V2 Response: Product with complete packaging hierarchy
    /// Used by GET /api/v2/products and GET /api/v2/products/{id}
    /// </summary>
    public class V2ProductWithHierarchyDto
    {
        /// <summary>
        /// Product ID
        /// </summary>
        [JsonPropertyName("productID")]
        public int ProductID { get; set; }

        /// <summary>
        /// Product name
        /// </summary>
        [JsonPropertyName("productName")]
        public string ProductName { get; set; }

        /// <summary>
        /// Complete nested packaging hierarchy with all items
        /// </summary>
        [JsonPropertyName("packages")]
        public List<V2PackageDto> Packages { get; set; } = new();
    }

    /// <summary>
    /// V2 Response: Complete packaging hierarchy with items
    /// Represents a packaging node with all nested children and items at each level
    /// </summary>
    public class V2PackageDto
    {
        [JsonPropertyName("packageID")]
        public int PackageID { get; set; }

        [JsonPropertyName("packageTypeID")]
        public int PackageTypeID { get; set; }

        [JsonPropertyName("packageTypeName")]
        public string PackageTypeName { get; set; }

        [JsonPropertyName("parentID")]
        public int? ParentID { get; set; }

        [JsonPropertyName("items")]
        public List<V2PackageItemDto> Items { get; set; } = new();

        [JsonPropertyName("packages")]
        public List<V2PackageDto> Packages { get; set; } = new();
    }

    /// <summary>
    /// V2 Response: Item detail with quantity in packaging
    /// Represents a single item contained in a packaging
    /// </summary>
    public class V2PackageItemDto
    {
        /// <summary>
        /// Item ID
        /// </summary>
        [JsonPropertyName("itemID")]
        public int ItemID { get; set; }

        /// <summary>
        /// Item name
        /// </summary>
        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }
    }
}
