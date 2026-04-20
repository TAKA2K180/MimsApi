using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MimsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingViewsAndStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create view for package hierarchy with recursion
            migrationBuilder.Sql(@"
                CREATE VIEW vw_PackagingHierarchy AS
                WITH PackagingCTE AS (
                    -- Base case: Root packaging (no parent)
                    SELECT 
                        Id,
                        PackagingName,
                        PackagingType,
                        ProductId,
                        ParentPackagingId,
                        0 AS NestingLevel,
                        CAST(Id AS VARCHAR(MAX)) AS HierarchyPath
                    FROM PackagingHeaders
                    WHERE ParentPackagingId IS NULL

                    UNION ALL

                    -- Recursive case: Child packaging
                    SELECT 
                        ph.Id,
                        ph.PackagingName,
                        ph.PackagingType,
                        ph.ProductId,
                        ph.ParentPackagingId,
                        cte.NestingLevel + 1,
                        cte.HierarchyPath + '/' + CAST(ph.Id AS VARCHAR(MAX))
                    FROM PackagingHeaders ph
                    INNER JOIN PackagingCTE cte ON ph.ParentPackagingId = cte.Id
                    WHERE cte.NestingLevel < 100 -- Prevent infinite recursion
                )
                SELECT * FROM PackagingCTE;
            ");

            // Create view for products with packaging count
            migrationBuilder.Sql(@"
                CREATE VIEW vw_ProductPackagingSummary AS
                SELECT 
                    p.Id,
                    p.ProductName,
                    COUNT(DISTINCT ph.Id) AS TotalPackagings,
                    COUNT(DISTINCT CASE WHEN ph.ParentPackagingId IS NULL THEN ph.Id END) AS RootPackagings,
                    COUNT(DISTINCT pd.ItemsId) AS UniqueItems,
                    SUM(pd.Quantity) AS TotalItemQuantity,
                    MAX(ph2.NestingLevel) AS MaxNestingLevel
                FROM Products p
                LEFT JOIN PackagingHeaders ph ON p.Id = ph.ProductId
                LEFT JOIN PackagingDetails pd ON ph.Id = pd.PackagingHeaderId
                LEFT JOIN vw_PackagingHierarchy ph2 ON ph.Id = ph2.Id
                GROUP BY p.Id, p.ProductName;
            ");

            // Create view for items in packaging (including nested)
            migrationBuilder.Sql(@"
                CREATE VIEW vw_PackagingItems AS
                SELECT 
                    ph.Id AS PackagingId,
                    ph.PackagingName,
                    ph.PackagingType,
                    ph.ProductId,
                    i.Id AS ItemId,
                    i.ItemName,
                    pd.Quantity,
                    pch.NestingLevel
                FROM PackagingHeaders ph
                LEFT JOIN PackagingDetails pd ON ph.Id = pd.PackagingHeaderId
                LEFT JOIN Items i ON pd.ItemsId = i.Id
                LEFT JOIN vw_PackagingHierarchy pch ON ph.Id = pch.Id
                WHERE pd.Id IS NOT NULL;
            ");

            // Create stored procedure to get product with all packaging levels
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetProductWithPackagingHierarchy
                    @ProductId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Get product
                    SELECT 
                        p.Id,
                        p.ProductName
                    FROM Products p
                    WHERE p.Id = @ProductId;

                    -- Get packaging hierarchy
                    SELECT 
                        ph.Id,
                        ph.PackagingName,
                        ph.PackagingType,
                        ph.ParentPackagingId,
                        pch.NestingLevel,
                        pch.HierarchyPath
                    FROM vw_PackagingHierarchy pch
                    INNER JOIN PackagingHeaders ph ON pch.Id = ph.Id
                    WHERE pch.ProductId = @ProductId
                    ORDER BY pch.NestingLevel, ph.PackagingName;

                    -- Get packaging details and items
                    SELECT 
                        pd.Id,
                        pd.PackagingHeaderId,
                        pd.ItemsId,
                        i.ItemName,
                        pd.Quantity
                    FROM PackagingDetails pd
                    INNER JOIN Items i ON pd.ItemsId = i.Id
                    WHERE pd.PackagingHeaderId IN (
                        SELECT ph.Id FROM PackagingHeaders ph WHERE ph.ProductId = @ProductId
                    )
                    ORDER BY pd.PackagingHeaderId, i.ItemName;
                END;
            ");

            // Create stored procedure to find packaging containing specific item
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetPackagingContainingItem
                    @ItemId INT,
                    @ProductId INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT DISTINCT
                        ph.Id,
                        ph.PackagingName,
                        ph.PackagingType,
                        ph.ProductId,
                        p.ProductName,
                        ph.ParentPackagingId,
                        pch.NestingLevel,
                        pd.Quantity
                    FROM PackagingDetails pd
                    INNER JOIN PackagingHeaders ph ON pd.PackagingHeaderId = ph.Id
                    INNER JOIN Products p ON ph.ProductId = p.Id
                    LEFT JOIN vw_PackagingHierarchy pch ON ph.Id = pch.Id
                    WHERE pd.ItemsId = @ItemId
                        AND (@ProductId IS NULL OR ph.ProductId = @ProductId)
                    ORDER BY p.ProductName, pch.NestingLevel, ph.PackagingName;
                END;
            ");

            // Create stored procedure to get all items in packaging (recursive)
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetAllItemsInPackaging
                    @PackagingId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    WITH PackagingTree AS (
                        -- Get the packaging and all its children
                        SELECT 
                            ph.Id,
                            ph.ParentPackagingId
                        FROM PackagingHeaders ph
                        WHERE ph.Id = @PackagingId

                        UNION ALL

                        SELECT 
                            ph.Id,
                            ph.ParentPackagingId
                        FROM PackagingHeaders ph
                        INNER JOIN PackagingTree pt ON ph.ParentPackagingId = pt.Id
                    )
                    SELECT 
                        pd.Id,
                        pd.PackagingHeaderId,
                        i.Id AS ItemId,
                        i.ItemName,
                        pd.Quantity
                    FROM PackagingDetails pd
                    INNER JOIN Items i ON pd.ItemsId = i.Id
                    WHERE pd.PackagingHeaderId IN (SELECT Id FROM PackagingTree)
                    ORDER BY pd.PackagingHeaderId, i.ItemName;
                END;
            ");

            // Create stored procedure to validate packaging hierarchy (check for circular references)
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_ValidatePackagingHierarchy
                    @ParentPackagingId INT,
                    @ChildPackagingId INT,
                    @IsValid BIT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SET @IsValid = 1;

                    -- Check if parent and child are the same
                    IF @ParentPackagingId = @ChildPackagingId
                    BEGIN
                        SET @IsValid = 0;
                        RETURN;
                    END;

                    -- Check if adding child to parent would create circular reference
                    -- Get all ancestors of the parent
                    WITH AncestorCTE AS (
                        SELECT ParentPackagingId
                        FROM PackagingHeaders
                        WHERE Id = @ParentPackagingId

                        UNION ALL

                        SELECT ph.ParentPackagingId
                        FROM PackagingHeaders ph
                        INNER JOIN AncestorCTE ac ON ph.Id = ac.ParentPackagingId
                        WHERE ac.ParentPackagingId IS NOT NULL
                    )
                    SELECT @IsValid = CASE 
                        WHEN EXISTS (
                            SELECT 1 FROM AncestorCTE WHERE ParentPackagingId = @ChildPackagingId
                        ) THEN 0
                        ELSE 1
                    END;
                END;
            ");

            // Create stored procedure to get packaging statistics
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetPackagingStatistics
                    @ProductId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        @ProductId AS ProductId,
                        COUNT(DISTINCT ph.Id) AS TotalPackagings,
                        COUNT(DISTINCT CASE WHEN ph.ParentPackagingId IS NULL THEN ph.Id END) AS RootPackagings,
                        ISNULL(MAX(pch.NestingLevel), 0) AS MaxNestingLevel,
                        COUNT(DISTINCT pd.ItemsId) AS UniqueItems,
                        ISNULL(SUM(pd.Quantity), 0) AS TotalItemQuantity
                    FROM PackagingHeaders ph
                    LEFT JOIN PackagingDetails pd ON ph.Id = pd.PackagingHeaderId
                    LEFT JOIN vw_PackagingHierarchy pch ON ph.Id = pch.Id
                    WHERE ph.ProductId = @ProductId;
                END;
            ");

            // Create index for better performance on hierarchical queries
            migrationBuilder.Sql(@"
                CREATE NONCLUSTERED INDEX IX_PackagingHeaders_ProductId_ParentId 
                ON PackagingHeaders(ProductId, ParentPackagingId);
            ");

            // Create index for PackagingDetails lookups
            migrationBuilder.Sql(@"
                CREATE NONCLUSTERED INDEX IX_PackagingDetails_ItemsId_Quantity 
                ON PackagingDetails(ItemsId, Quantity);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop stored procedures
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetProductWithPackagingHierarchy;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetPackagingContainingItem;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetAllItemsInPackaging;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ValidatePackagingHierarchy;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetPackagingStatistics;");

            // Drop views (must drop dependent views first)
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_PackagingItems;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ProductPackagingSummary;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_PackagingHierarchy;");

            // Drop indexes
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_PackagingHeaders_ProductId_ParentId ON PackagingHeaders;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_PackagingDetails_ItemsId_Quantity ON PackagingDetails;");
        }
    }
}
