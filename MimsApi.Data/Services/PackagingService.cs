using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MimsApi.Core.models;
using MimsApi.Data.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MimsApi.Data.Services
{
    /// <summary>
    /// Service for accessing packaging hierarchy using stored procedures and views
    /// </summary>
    public class PackagingService
    {
        private readonly MimsDbContext _dbContext;
        private readonly ILogger<PackagingService> _logger;

        public PackagingService(MimsDbContext dbContext, ILogger<PackagingService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a product with all its packaging levels using stored procedure
        /// </summary>
        public async Task<ProductHierarchyData> GetProductWithPackagingHierarchyAsync(int productId)
        {
            try
            {
                _logger.LogInformation("Fetching product with packaging hierarchy: {ProductId}", productId);

                var result = new ProductHierarchyData();

                // Execute stored procedure using raw SQL
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "sp_GetProductWithPackagingHierarchy";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@ProductId", productId));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read product
                            if (await reader.ReadAsync())
                            {
                                result.ProductId = reader.GetInt32(0);
                                result.ProductName = reader.GetString(1);
                            }

                            // Read packaging hierarchy
                            if (reader.NextResult())
                            {
                                var packagingDict = new Dictionary<int, PackagingNode>();
                                var rootPackagings = new List<PackagingNode>();

                                while (await reader.ReadAsync())
                                {
                                    var packagingId = reader.GetInt32(0);
                                    var parentId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                                    var node = new PackagingNode
                                    {
                                        Id = packagingId,
                                        PackagingName = reader.GetString(1),
                                        PackagingType = reader.GetString(2),
                                        ParentPackagingId = parentId,
                                        NestingLevel = reader.GetInt32(4),
                                        Children = new List<PackagingNode>()
                                    };

                                    packagingDict[packagingId] = node;

                                    if (parentId == null)
                                        rootPackagings.Add(node);
                                }

                                // Build hierarchy
                                foreach (var packaging in packagingDict.Values.Where(p => p.ParentPackagingId.HasValue))
                                {
                                    if (packagingDict.TryGetValue(packaging.ParentPackagingId.Value, out var parent))
                                    {
                                        parent.Children.Add(packaging);
                                    }
                                }

                                result.RootPackagings = rootPackagings;
                            }

                            // Read packaging details
                            if (reader.NextResult())
                            {
                                var detailsDict = new Dictionary<int, List<PackagingItemData>>();

                                while (await reader.ReadAsync())
                                {
                                    var packagingHeaderId = reader.GetInt32(1);
                                    var itemData = new PackagingItemData
                                    {
                                        ItemId = reader.GetInt32(2),
                                        ItemName = reader.GetString(3),
                                        Quantity = reader.GetInt32(4)
                                    };

                                    if (!detailsDict.ContainsKey(packagingHeaderId))
                                        detailsDict[packagingHeaderId] = new List<PackagingItemData>();

                                    detailsDict[packagingHeaderId].Add(itemData);
                                }

                                result.PackagingItems = detailsDict;
                            }
                        }
                    }
                }

                _logger.LogInformation("Successfully retrieved product with packaging hierarchy: {ProductId}", productId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with packaging hierarchy: {ProductId}", productId);
                throw new InvalidOperationException(
                    $"Error retrieving product with packaging hierarchy for product ID {productId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves all packaging that contains a specific item
        /// </summary>
        public async Task<IEnumerable<PackagingWithItemData>> GetPackagingContainingItemAsync(int itemId, int? productId = null)
        {
            try
            {
                _logger.LogInformation("Fetching packaging containing item: {ItemId}", itemId);

                var result = new List<PackagingWithItemData>();

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "sp_GetPackagingContainingItem";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@ItemId", itemId));
                        command.Parameters.Add(new SqlParameter("@ProductId", productId ?? (object)DBNull.Value));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var packaging = new PackagingWithItemData
                                {
                                    PackagingId = reader.GetInt32(0),
                                    PackagingName = reader.GetString(1),
                                    PackagingType = reader.GetString(2),
                                    ProductId = reader.GetInt32(3),
                                    ProductName = reader.GetString(4),
                                    ParentPackagingId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                    NestingLevel = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                                    Quantity = reader.GetInt32(7)
                                };

                                result.Add(packaging);
                            }
                        }
                    }
                }

                _logger.LogInformation("Found {PackagingCount} packaging(s) containing item {ItemId}", result.Count, itemId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving packaging containing item: {ItemId}", itemId);
                throw new InvalidOperationException(
                    $"Error retrieving packaging containing item ID {itemId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves all items in a packaging including nested items
        /// </summary>
        public async Task<IEnumerable<PackagingDetailData>> GetAllItemsInPackagingAsync(int packagingId)
        {
            try
            {
                _logger.LogInformation("Fetching all items in packaging: {PackagingId}", packagingId);

                var result = new List<PackagingDetailData>();

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "sp_GetAllItemsInPackaging";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@PackagingId", packagingId));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var detail = new PackagingDetailData
                                {
                                    PackagingDetailId = reader.GetInt32(0),
                                    PackagingHeaderId = reader.GetInt32(1),
                                    ItemId = reader.GetInt32(2),
                                    ItemName = reader.GetString(3),
                                    Quantity = reader.GetInt32(4)
                                };

                                result.Add(detail);
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {ItemCount} total items in packaging: {PackagingId}", result.Count, packagingId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all items in packaging: {PackagingId}", packagingId);
                throw new InvalidOperationException(
                    $"Error retrieving all items in packaging ID {packagingId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates packaging hierarchy to prevent circular references
        /// </summary>
        public async Task<bool> ValidatePackagingHierarchyAsync(int parentPackagingId, int childPackagingId)
        {
            try
            {
                _logger.LogInformation("Validating packaging hierarchy: Parent {ParentId}, Child {ChildId}",
                    parentPackagingId, childPackagingId);

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "sp_ValidatePackagingHierarchy";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@ParentPackagingId", parentPackagingId));
                        command.Parameters.Add(new SqlParameter("@ChildPackagingId", childPackagingId));

                        var isValidParam = new SqlParameter("@IsValid", SqlDbType.Bit)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(isValidParam);

                        await command.ExecuteNonQueryAsync();
                        var isValid = (bool)isValidParam.Value;

                        _logger.LogInformation("Packaging hierarchy validation result: {IsValid}", isValid);
                        return isValid;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating packaging hierarchy");
                throw new InvalidOperationException(
                    $"Error validating packaging hierarchy: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves packaging statistics for a product
        /// </summary>
        public async Task<PackagingStatisticsData> GetPackagingStatisticsAsync(int productId)
        {
            try
            {
                _logger.LogInformation("Retrieving packaging statistics for product: {ProductId}", productId);

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "sp_GetPackagingStatistics";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@ProductId", productId));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var stats = new PackagingStatisticsData
                                {
                                    ProductId = reader.GetInt32(0),
                                    TotalPackagings = reader.GetInt32(1),
                                    RootPackagings = reader.GetInt32(2),
                                    MaxNestingLevel = reader.GetInt32(3),
                                    UniqueItems = reader.GetInt32(4),
                                    TotalItemQuantity = reader.GetInt32(5)
                                };

                                _logger.LogInformation("Retrieved packaging statistics - Total: {Total}, MaxLevel: {MaxLevel}",
                                    stats.TotalPackagings, stats.MaxNestingLevel);

                                return stats;
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving packaging statistics for product: {ProductId}", productId);
                throw new InvalidOperationException(
                    $"Error retrieving packaging statistics for product {productId}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Data transfer objects for packaging service
    /// </summary>
    public class ProductHierarchyData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public List<PackagingNode> RootPackagings { get; set; }
        public Dictionary<int, List<PackagingItemData>> PackagingItems { get; set; }
    }

    public class PackagingNode
    {
        public int Id { get; set; }
        public string PackagingName { get; set; }
        public string PackagingType { get; set; }
        public int? ParentPackagingId { get; set; }
        public int NestingLevel { get; set; }
        public List<PackagingNode> Children { get; set; } = new();
    }

    public class PackagingItemData
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }

    public class PackagingWithItemData
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

    public class PackagingDetailData
    {
        public int PackagingDetailId { get; set; }
        public int PackagingHeaderId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }

    public class PackagingStatisticsData
    {
        public int ProductId { get; set; }
        public int TotalPackagings { get; set; }
        public int RootPackagings { get; set; }
        public int MaxNestingLevel { get; set; }
        public int UniqueItems { get; set; }
        public int TotalItemQuantity { get; set; }
    }
}
