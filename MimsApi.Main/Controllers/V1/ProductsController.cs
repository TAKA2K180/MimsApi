using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MimsApi.Core.models;
using MimsApi.Core.services;
using MimsApi.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MimsApi.Main.Controllers.V1
{
    /// <summary>
    /// Unified Products controller for all product CRUD operations
    /// Uses optimized stored procedures for performance
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
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

        #region Product CRUD Endpoints

        /// <summary>
        /// Get all products
        /// </summary>
        /// <returns>List of products</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                _logger.LogInformation("Fetching all products");

                var products = await _productService.GetAllProductsAsync();
                var productDtos = products.Select(p => MapProductToDto(p));

                return Ok(new ApiResponse<IEnumerable<ProductDto>>
                {
                    Success = true,
                    Message = "Products retrieved successfully",
                    Data = productDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new ApiErrorResponse
                {
                    Success = false,
                    Message = $"Error retrieving products: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get all products with pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>Paginated list of products</returns>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ProductDto>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProductsPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Fetching paginated products - Page: {PageNumber}, PageSize: {PageSize}", pageNumber, pageSize);

                var (products, total) = await _productService.GetAllProductsPagedAsync(pageNumber, pageSize);
                var productDtos = products.Select(p => MapProductToDto(p));

                var response = new PaginatedResponse<ProductDto>
                {
                    Items = productDtos,
                    TotalCount = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(new ApiResponse<PaginatedResponse<ProductDto>>
                {
                    Success = true,
                    Message = "Products retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new ApiErrorResponse
                {
                    Success = false,
                    Message = $"Error retrieving products: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching product by ID: {ProductId}", id);

                if (id <= 0)
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Message = "Product ID must be greater than 0"
                    });

                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Message = $"Product with ID {id} not found"
                    });

                var productDto = MapProductToDto(product);

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Message = "Product retrieved successfully",
                    Data = productDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product");
                return StatusCode(500, new ApiErrorResponse
                {
                    Success = false,
                    Message = $"Error retrieving product: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        /// <param name="request">Product creation request</param>
        /// <returns>Created product</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new product: {ProductName}", request?.ProductName);

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (string.IsNullOrWhiteSpace(request?.ProductName))
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Message = "Product name is required"
                    });

                var product = new MimsApi.Core.models.Products
                {
                    ProductName = request.ProductName.Trim()
                };

                var createdProduct = await _productService.CreateProductAsync(product);
                var productDto = MapProductToDto(createdProduct);

                return CreatedAtAction(nameof(GetProductById), new { id = createdProduct.Id },
                    new ApiResponse<ProductDto>
                    {
                        Success = true,
                        Message = "Product created successfully",
                        Data = productDto
                    });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiErrorResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new ApiErrorResponse
                {
                    Success = false,
                    Message = $"Error creating product: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="request">Product update request</param>
        /// <returns>Updated product</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                _logger.LogInformation("Updating product: {ProductId}", id);

                if (id <= 0)
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Message = "Product ID must be greater than 0"
                    });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (string.IsNullOrWhiteSpace(request?.ProductName))
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Message = "Product name is required"
                    });

                var product = new MimsApi.Core.models.Products
                {
                    ProductName = request.ProductName.Trim()
                };

                var updatedProduct = await _productService.UpdateProductAsync(id, product);
                var productDto = MapProductToDto(updatedProduct);

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Message = "Product updated successfully",
                    Data = productDto
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new ApiErrorResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return StatusCode(500, new ApiErrorResponse
                {
                    Success = false,
                    Message = $"Error updating product: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Delete a product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                _logger.LogInformation("Deleting product: {ProductId}", id);

                if (id <= 0)
                    return BadRequest(new ApiErrorResponse
                    {
                        Success = false,
                        Message = "Product ID must be greater than 0"
                    });

                var deleted = await _productService.DeleteProductAsync(id);
                if (!deleted)
                    return NotFound(new ApiErrorResponse
                    {
                        Success = false,
                        Message = $"Product with ID {id} not found"
                    });

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = $"Product with ID {id} deleted successfully",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return StatusCode(500, new ApiErrorResponse
                {
                    Success = false,
                    Message = $"Error deleting product: {ex.Message}"
                });
            }
        }

        #endregion

        #region Private Helper Methods

        private static ProductDto MapProductToDto(Products product)
        {
            return new ProductDto
            {
                Id = product.Id,
                ProductName = product.ProductName
            };
        }

        #endregion
    }
}
