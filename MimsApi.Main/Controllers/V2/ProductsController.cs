using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MimsApi.Core.models;
using MimsApi.Core.services;
using MimsApi.Data.Services;
using Microsoft.Extensions.Logging;

namespace MimsApi.Main.Controllers.V2
{
    /// <summary>
    /// Enhanced V2 Products Controller with complete packaging hierarchy
    /// GET /products returns all products WITH full packaging hierarchy details
    /// GET /products/{id} returns single product with hierarchy
    /// </summary>
    [ApiController]
    [Route("api/v2/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Get all products with complete packaging hierarchy details
        /// 
        /// Returns all products including full packaging structure (nested hierarchy)
        /// with all items at each packaging level.
        /// 
        /// This is the enhanced V2 response that shows complete product structure.
        /// Compare with V1 /api/v1/products which returns only basic product info.
        /// 
        /// Example: GET /api/v2/products
        /// </summary>
        /// <returns>List of products with complete packaging hierarchy</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<V2ProductWithHierarchyDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                _logger.LogInformation("V2: Fetching all products with packaging hierarchy");

                var products = await _productService.GetAllProductsAsync();
                var response = new List<V2ProductWithHierarchyDto>();

                foreach (var product in products)
                {
                    var hierarchy = await _productService.GetProductWithPackagingHierarchyAsync(product.Id);

                    response.Add(new V2ProductWithHierarchyDto
                    {
                        ProductID = product.Id,
                        ProductName = product.ProductName,
                        Packages = hierarchy?.RootPackagings != null
                            ? MapPackages(hierarchy.RootPackagings, hierarchy.PackagingItems ?? new Dictionary<int, List<PackagingItemData>>())
                            : new List<V2PackageDto>()
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "V2: Error retrieving all products with hierarchy");
                return StatusCode(500, new { message = $"Error retrieving products: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get product by ID with complete packaging hierarchy
        /// 
        /// Returns single product with full packaging structure (nested hierarchy)
        /// and all items at each packaging level.
        /// 
        /// Example: GET /api/v2/products/1
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product with complete packaging hierarchy</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IEnumerable<V2ProductWithHierarchyDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                _logger.LogInformation("V2: Fetching product with hierarchy by ID: {ProductId}", id);

                if (id <= 0)
                {
                    return BadRequest(new { message = "Product ID must be greater than 0" });
                }

                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = $"Product with ID {id} not found" });
                }

                var hierarchy = await _productService.GetProductWithPackagingHierarchyAsync(id);
                var response = new[]
                {
                    new V2ProductWithHierarchyDto
                    {
                        ProductID = product.Id,
                        ProductName = product.ProductName,
                        Packages = hierarchy?.RootPackagings != null
                            ? MapPackages(hierarchy.RootPackagings, hierarchy.PackagingItems ?? new Dictionary<int, List<PackagingItemData>>())
                            : new List<V2PackageDto>()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "V2: Error retrieving product with hierarchy: {ProductId}", id);
                return StatusCode(500, new { message = $"Error retrieving product: {ex.Message}" });
            }
        }

        private List<V2PackageDto> MapPackages(
            List<PackagingNode> nodes,
            Dictionary<int, List<PackagingItemData>> packagingItems)
        {
            return nodes.Select(node => new V2PackageDto
            {
                PackageID = node.Id,
                PackageTypeID = MapPackageTypeId(node.PackagingType),
                PackageTypeName = node.PackagingType,
                ParentID = node.ParentPackagingId,
                Items = packagingItems.TryGetValue(node.Id, out var items)
                    ? items.Select(item => new V2PackageItemDto
                    {
                        ItemID = item.ItemId,
                        ItemName = item.ItemName
                    }).ToList()
                    : new List<V2PackageItemDto>(),
                Packages = node.Children.Any()
                    ? MapPackages(node.Children, packagingItems)
                    : new List<V2PackageDto>()
            }).ToList();
        }

        private static int MapPackageTypeId(string packageType)
        {
            return packageType?.Trim().ToLowerInvariant() switch
            {
                "box" => 1,
                "packet" => 2,
                _ => 0
            };
        }
    }
}
