-- ============================================================================
-- DATABASE-FIRST APPROACH: STORED PROCEDURES FOR ALL PRODUCT OPERATIONS
-- ============================================================================
-- This script creates all stored procedures needed for the unified Products API
-- NO ORM is used - all operations go through stored procedures
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
-- SUMMARY: STORED PROCEDURES CREATED
-- ============================================================================
-- sp_GetAllProducts              - Get all products ordered by name
-- sp_GetProductsPaged            - Get paginated products with total count
-- sp_GetProductById              - Get single product by ID
-- sp_CreateProduct               - Create new product with duplicate check
-- sp_UpdateProduct               - Update existing product
-- sp_DeleteProduct               - Delete product and related packaging
-- sp_ProductExists               - Check if product exists
-- ============================================================================
-- EXISTING PROCEDURES (NOT MODIFIED - Already in Database)
-- ============================================================================
-- sp_GetProductWithPackagingHierarchy    - Hierarchy with items
-- sp_GetPackagingContainingItem          - Find item locations
-- sp_GetAllItemsInPackaging              - Get nested items
-- sp_ValidatePackagingHierarchy          - Check circular refs
-- sp_GetPackagingStatistics              - Aggregate stats
-- ============================================================================

PRINT 'All product stored procedures created successfully!';
