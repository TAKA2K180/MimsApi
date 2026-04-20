// THIS FILE IS DEPRECATED AND SHOULD BE DELETED
// 
// Functionality has been consolidated into:
// - MimsApi.Main/Controllers/V1/ProductsController.cs (CRUD)
// - MimsApi.Main/Controllers/V1/ProductsControllerExtension.cs (Hierarchy)
//
// All endpoints are now available at: /api/v1/products
//
// This file is kept for historical reference only.
// DELETE THIS FILE AFTER VERIFICATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MimsApi.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MimsApi.Main.Controllers.V1
{
    /// <summary>
    /// Controller for packaging hierarchy operations
    /// Demonstrates usage of PackagingService with views and stored procedures
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PackagingController : ControllerBase
    {
        private readonly PackagingService _packagingService;
        private readonly ILogger<PackagingController> _logger;

        public PackagingController(PackagingService packagingService, ILogger<PackagingController> logger)
        {
            _packagingService = packagingService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a product with its complete packaging hierarchy
        /// 
        /// This endpoint demonstrates efficient hierarchical query using stored procedure
        /// and recursive CTEs to fetch all packaging levels in a single optimized call.
        /// 
        /// Example: GET /api/v1/packaging/products/1/hierarchy
        /// </summary>
        [HttpGet("products/{productId}/hierarchy")]
        [ProducesResponseType(typeof(ProductHierarchyResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProductWithPackagingHierarchy(int productId)
        {
            try
            {
                _logger.LogInformation("Fetching product hierarchy for product ID: {ProductId}", productId);

                if (productId <= 0)
                    return BadRequest("Product ID must be greater than 0");

                var result = await _packagingService.GetProductWithPackagingHierarchyAsync(productId);

                if (result == null)
                    return NotFound($"Product with ID {productId} not found");

                var response = new ProductHierarchyResponse
                {
                    Product = new ProductInfo { Id = result.ProductId, Name = result.ProductName },
                    Packaging = MapPackagingNodes(result.RootPackagings),
                    Statistics = new
                    {
                        TotalPackagings = result.RootPackagings.Sum(CountPackagings),
                        RootPackagings = result.RootPackagings.Count,
                        TotalItems = result.PackagingItems.Sum(kvp => kvp.Value.Count)
                    }
                };

                _logger.LogInformation("Successfully fetched product hierarchy");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product hierarchy");
                return StatusCode(500, new { error = "Error retrieving product hierarchy", details = ex.Message });
            }
        }

        /// <summary>
        /// Finds all packaging that contains a specific item
        /// 
        /// This demonstrates querying across the packaging hierarchy to locate items,
        /// supporting both global search and product-specific filtering.
        /// 
        /// Example: GET /api/v1/packaging/items/5/locations
        /// Example: GET /api/v1/packaging/items/5/locations?productId=1
        /// </summary>
        [HttpGet("items/{itemId}/locations")]
        [ProducesResponseType(typeof(IEnumerable<PackagingLocationResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetItemLocations(int itemId, [FromQuery] int? productId = null)
        {
            try
            {
                _logger.LogInformation("Finding packaging containing item ID: {ItemId}, ProductId: {ProductId}", itemId, productId);

                if (itemId <= 0)
                    return BadRequest("Item ID must be greater than 0");

                var packaging = await _packagingService.GetPackagingContainingItemAsync(itemId, productId);

                var response = packaging.Select(p => new PackagingLocationResponse
                {
                    PackagingId = p.PackagingId,
                    PackagingName = p.PackagingName,
                    PackagingType = p.PackagingType,
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ParentPackagingId = p.ParentPackagingId,
                    NestingLevel = p.NestingLevel,
                    Quantity = p.Quantity
                }).OrderBy(p => p.ProductName).ThenBy(p => p.NestingLevel);

                _logger.LogInformation("Found {Count} packaging locations", response.Count());
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding packaging locations");
                return StatusCode(500, new { error = "Error retrieving packaging locations", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets all items contained in a packaging, including items in nested child packaging
        /// 
        /// Demonstrates recursive retrieval of all items across nested packaging hierarchy.
        /// 
        /// Example: GET /api/v1/packaging/1/items
        /// </summary>
        [HttpGet("{packagingId}/items")]
        [ProducesResponseType(typeof(PackagingItemsResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPackagingItems(int packagingId)
        {
            try
            {
                _logger.LogInformation("Fetching all items in packaging ID: {PackagingId}", packagingId);

                if (packagingId <= 0)
                    return BadRequest("Packaging ID must be greater than 0");

                var items = await _packagingService.GetAllItemsInPackagingAsync(packagingId);

                var groupedByPackaging = items
                    .GroupBy(i => i.PackagingHeaderId)
                    .Select(g => new
                    {
                        PackagingId = g.Key,
                        Items = g.Select(i => new
                        {
                            ItemId = i.ItemId,
                            ItemName = i.ItemName,
                            Quantity = i.Quantity
                        })
                    });

                var response = new PackagingItemsResponse
                {
                    PackagingId = packagingId,
                    NestedPackagings = groupedByPackaging.Count(),
                    TotalItems = items.Count(),
                    DetailsByPackaging = groupedByPackaging
                };

                _logger.LogInformation("Retrieved items from {Count} packaging(s)", groupedByPackaging.Count());
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching packaging items");
                return StatusCode(500, new { error = "Error retrieving packaging items", details = ex.Message });
            }
        }

        /// <summary>
        /// Validates if a packaging can be added as a child to another packaging
        /// 
        /// Prevents circular references in the hierarchy before making database changes.
        /// Always call this before updating a packaging's parent.
        /// 
        /// Example: GET /api/v1/packaging/validate-hierarchy?parentId=1&childId=5
        /// </summary>
        [HttpGet("validate-hierarchy")]
        [ProducesResponseType(typeof(HierarchyValidationResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ValidateHierarchy([FromQuery] int parentId, [FromQuery] int childId)
        {
            try
            {
                _logger.LogInformation("Validating hierarchy: Parent {ParentId}, Child {ChildId}", parentId, childId);

                if (parentId <= 0 || childId <= 0)
                    return BadRequest("Both Parent ID and Child ID must be greater than 0");

                var isValid = await _packagingService.ValidatePackagingHierarchyAsync(parentId, childId);

                var response = new HierarchyValidationResponse
                {
                    IsValid = isValid,
                    Message = isValid 
                        ? "Packaging can be added as child without creating circular reference"
                        : "Cannot add packaging as child: would create circular reference or invalid hierarchy"
                };

                _logger.LogInformation("Hierarchy validation result: {IsValid}", isValid);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating packaging hierarchy");
                return StatusCode(500, new { error = "Error validating hierarchy", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets aggregated statistics for a product's packaging
        /// 
        /// Provides summary metrics for dashboard and reporting purposes.
        /// 
        /// Example: GET /api/v1/packaging/products/1/statistics
        /// </summary>
        [HttpGet("products/{productId}/statistics")]
        [ProducesResponseType(typeof(PackagingStatisticsResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPackagingStatistics(int productId)
        {
            try
            {
                _logger.LogInformation("Fetching packaging statistics for product ID: {ProductId}", productId);

                if (productId <= 0)
                    return BadRequest("Product ID must be greater than 0");

                var stats = await _packagingService.GetPackagingStatisticsAsync(productId);

                if (stats == null)
                    return NotFound($"No packaging found for product ID {productId}");

                var response = new PackagingStatisticsResponse
                {
                    ProductId = stats.ProductId,
                    TotalPackagings = stats.TotalPackagings,
                    RootPackagings = stats.RootPackagings,
                    MaxNestingLevel = stats.MaxNestingLevel,
                    UniqueItems = stats.UniqueItems,
                    TotalItemQuantity = stats.TotalItemQuantity
                };

                _logger.LogInformation("Retrieved statistics - Total: {Total}, MaxLevel: {MaxLevel}",
                    stats.TotalPackagings, stats.MaxNestingLevel);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching packaging statistics");
                return StatusCode(500, new { error = "Error retrieving statistics", details = ex.Message });
            }
        }

        #region Private Helper Methods

        private List<PackagingNodeResponse> MapPackagingNodes(List<PackagingNode> nodes)
        {
            return nodes.Select(node => new PackagingNodeResponse
            {
                Id = node.Id,
                PackagingName = node.PackagingName,
                PackagingType = node.PackagingType,
                ParentPackagingId = node.ParentPackagingId,
                NestingLevel = node.NestingLevel,
                Children = node.Children.Any() ? MapPackagingNodes(node.Children) : new List<PackagingNodeResponse>()
            }).ToList();
        }

        private int CountPackagings(PackagingNode node)
        {
            return 1 + node.Children.Sum(CountPackagings);
        }

        #endregion

        #region Response DTOs

        public class ProductHierarchyResponse
        {
            public ProductInfo Product { get; set; }
            public List<PackagingNodeResponse> Packaging { get; set; }
            public object Statistics { get; set; }
        }

        public class ProductInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class PackagingNodeResponse
        {
            public int Id { get; set; }
            public string PackagingName { get; set; }
            public string PackagingType { get; set; }
            public int? ParentPackagingId { get; set; }
            public int NestingLevel { get; set; }
            public List<PackagingNodeResponse> Children { get; set; }
        }

        public class PackagingLocationResponse
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

        public class PackagingItemsResponse
        {
            public int PackagingId { get; set; }
            public int NestedPackagings { get; set; }
            public int TotalItems { get; set; }
            public object DetailsByPackaging { get; set; }
        }

        public class HierarchyValidationResponse
        {
            public bool IsValid { get; set; }
            public string Message { get; set; }
        }

        public class PackagingStatisticsResponse
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
}
