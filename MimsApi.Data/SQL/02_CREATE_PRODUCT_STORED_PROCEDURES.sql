-- ============================================================================
-- STORED PROCEDURES FOR ALL PRODUCT OPERATIONS
-- ============================================================================
-- This script creates all stored procedures needed for the Products API
-- ============================================================================

-- ============================================================================
-- DROP EXISTING PROCEDURES (for redeployment)
-- ============================================================================
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetAllProducts')
    DROP PROCEDURE sp_GetAllProducts;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetProductsPaged')
    DROP PROCEDURE sp_GetProductsPaged;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetProductById')
    DROP PROCEDURE sp_GetProductById;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_CreateProduct')
    DROP PROCEDURE sp_CreateProduct;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_UpdateProduct')
    DROP PROCEDURE sp_UpdateProduct;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_DeleteProduct')
    DROP PROCEDURE sp_DeleteProduct;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_ProductExists')
    DROP PROCEDURE sp_ProductExists;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetProductWithPackagingHierarchy')
    DROP PROCEDURE sp_GetProductWithPackagingHierarchy;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetPackagingContainingItem')
    DROP PROCEDURE sp_GetPackagingContainingItem;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetAllItemsInPackaging')
    DROP PROCEDURE sp_GetAllItemsInPackaging;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_ValidatePackagingHierarchy')
    DROP PROCEDURE sp_ValidatePackagingHierarchy;
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetPackagingStatistics')
    DROP PROCEDURE sp_GetPackagingStatistics;
GO

-- ============================================================================
-- 1. GET ALL PRODUCTS
-- ============================================================================
CREATE PROCEDURE sp_GetAllProducts
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Id,
        ProductName
    FROM Products
    ORDER BY ProductName ASC;
END;
GO

-- ============================================================================
-- 2. GET PRODUCTS PAGINATED
-- ============================================================================
CREATE PROCEDURE sp_GetProductsPaged
    @PageNumber INT = 1,
    @PageSize INT = 10,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate parameters
    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 SET @PageSize = 10;
    IF @PageSize > 1000 SET @PageSize = 1000;
    
    -- Get total count
    SELECT @TotalCount = COUNT(*) FROM Products;
    
    -- Get paged results
    SELECT 
        Id,
        ProductName
    FROM Products
    ORDER BY ProductName ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- ============================================================================
-- 3. GET PRODUCT BY ID
-- ============================================================================
CREATE PROCEDURE sp_GetProductById
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @ProductId <= 0
    BEGIN
        RAISERROR('Product ID must be greater than 0', 16, 1);
        RETURN;
    END;
    
    SELECT 
        Id,
        ProductName
    FROM Products
    WHERE Id = @ProductId;
END;
GO

-- ============================================================================
-- 4. CREATE PRODUCT
-- ============================================================================
CREATE PROCEDURE sp_CreateProduct
    @ProductName NVARCHAR(255),
    @ProductId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate input
    IF @ProductName IS NULL OR LTRIM(RTRIM(@ProductName)) = ''
    BEGIN
        RAISERROR('Product name is required', 16, 1);
        RETURN;
    END;
    
    -- Check for duplicate
    IF EXISTS (SELECT 1 FROM Products WHERE ProductName = LTRIM(RTRIM(@ProductName)))
    BEGIN
        RAISERROR('Product with this name already exists', 16, 1);
        RETURN;
    END;
    
    -- Insert product
    INSERT INTO Products (ProductName)
    VALUES (LTRIM(RTRIM(@ProductName)));
    
    -- Return the new product ID
    SET @ProductId = SCOPE_IDENTITY();
END;
GO

-- ============================================================================
-- 5. UPDATE PRODUCT
-- ============================================================================
CREATE PROCEDURE sp_UpdateProduct
    @ProductId INT,
    @ProductName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate inputs
    IF @ProductId <= 0
    BEGIN
        RAISERROR('Product ID must be greater than 0', 16, 1);
        RETURN;
    END;
    
    IF @ProductName IS NULL OR LTRIM(RTRIM(@ProductName)) = ''
    BEGIN
        RAISERROR('Product name is required', 16, 1);
        RETURN;
    END;
    
    -- Check if product exists
    IF NOT EXISTS (SELECT 1 FROM Products WHERE Id = @ProductId)
    BEGIN
        RAISERROR('Product not found', 16, 1);
        RETURN;
    END;
    
    -- Check for duplicate name (excluding current product)
    IF EXISTS (SELECT 1 FROM Products WHERE ProductName = LTRIM(RTRIM(@ProductName)) AND Id != @ProductId)
    BEGIN
        RAISERROR('Another product with this name already exists', 16, 1);
        RETURN;
    END;
    
    -- Update product
    UPDATE Products
    SET ProductName = LTRIM(RTRIM(@ProductName))
    WHERE Id = @ProductId;
END;
GO

-- ============================================================================
-- 6. DELETE PRODUCT
-- ============================================================================
CREATE PROCEDURE sp_DeleteProduct
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate input
    IF @ProductId <= 0
    BEGIN
        RAISERROR('Product ID must be greater than 0', 16, 1);
        RETURN;
    END;
    
    -- Check if product exists
    IF NOT EXISTS (SELECT 1 FROM Products WHERE Id = @ProductId)
    BEGIN
        RETURN; -- Product not found, return gracefully
    END;
    
    -- Delete associated packaging headers
    DELETE FROM PackagingHeaders
    WHERE ProductId = @ProductId;
    
    -- Delete the product
    DELETE FROM Products
    WHERE Id = @ProductId;
END;
GO

-- ============================================================================
-- 7. CHECK IF PRODUCT EXISTS
-- ============================================================================
CREATE PROCEDURE sp_ProductExists
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT COUNT(*)
    FROM Products
    WHERE Id = @ProductId;
END;
GO

-- ============================================================================
-- 8. GET PRODUCT WITH PACKAGING HIERARCHY
-- ============================================================================
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
GO

-- ============================================================================
-- 9. GET PACKAGING CONTAINING ITEM
-- ============================================================================
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
GO

-- ============================================================================
-- 10. GET ALL ITEMS IN PACKAGING (RECURSIVE)
-- ============================================================================
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
GO

-- ============================================================================
-- 11. VALIDATE PACKAGING HIERARCHY
-- ============================================================================
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
GO

-- ============================================================================
-- 12. GET PACKAGING STATISTICS
-- ============================================================================
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
GO

-- ============================================================================
-- SUMMARY: STORED PROCEDURES CREATED
-- ============================================================================
-- sp_GetAllProducts                    - Get all products ordered by name
-- sp_GetProductsPaged                  - Get paginated products with total count
-- sp_GetProductById                    - Get single product by ID
-- sp_CreateProduct                     - Create new product with duplicate check
-- sp_UpdateProduct                     - Update existing product
-- sp_DeleteProduct                     - Delete product and related packaging
-- sp_ProductExists                     - Check if product exists
-- sp_GetProductWithPackagingHierarchy  - Hierarchy with items
-- sp_GetPackagingContainingItem        - Find item locations
-- sp_GetAllItemsInPackaging            - Get nested items
-- sp_ValidatePackagingHierarchy        - Check circular refs
-- sp_GetPackagingStatistics            - Aggregate stats
-- ============================================================================
