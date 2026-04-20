-- ============================================================================
-- DATABASE-FIRST APPROACH: INDEX OPTIMIZATION FOR PERFORMANCE
-- ============================================================================
-- These indexes optimize all stored procedure queries
-- ============================================================================

-- ============================================================================
-- DROP EXISTING INDEXES IF THEY EXIST
-- ============================================================================
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Name')
    DROP INDEX IX_Products_Name ON Products;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PackagingHeaders_ProductId')
    DROP INDEX IX_PackagingHeaders_ProductId ON PackagingHeaders;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PackagingHeaders_ParentId')
    DROP INDEX IX_PackagingHeaders_ParentId ON PackagingHeaders;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PackagingHeaders_ProductId_ParentId')
    DROP INDEX IX_PackagingHeaders_ProductId_ParentId ON PackagingHeaders;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PackagingDetails_PackagingHeaderId')
    DROP INDEX IX_PackagingDetails_PackagingHeaderId ON PackagingDetails;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PackagingDetails_ItemsId')
    DROP INDEX IX_PackagingDetails_ItemsId ON PackagingDetails;

-- ============================================================================
-- PRODUCTS TABLE INDEXES
-- ============================================================================

-- Index for product name lookups (used in CREATE/UPDATE duplicate checks)
CREATE NONCLUSTERED INDEX IX_Products_Name 
ON Products(ProductName)
WHERE ProductName IS NOT NULL;

PRINT 'Created index: IX_Products_Name';

-- ============================================================================
-- PACKAGING HEADERS TABLE INDEXES
-- ============================================================================

-- Index for product lookups (sp_GetProductWithPackagingHierarchy, sp_DeleteProduct)
CREATE NONCLUSTERED INDEX IX_PackagingHeaders_ProductId 
ON PackagingHeaders(ProductId)
INCLUDE (Id, PackagingName, PackagingType, ParentPackagingId);

PRINT 'Created index: IX_PackagingHeaders_ProductId';

-- Index for parent lookups (hierarchy traversal)
CREATE NONCLUSTERED INDEX IX_PackagingHeaders_ParentId 
ON PackagingHeaders(ParentPackagingId)
INCLUDE (Id, ProductId, PackagingName, PackagingType);

PRINT 'Created index: IX_PackagingHeaders_ParentId';

-- Composite index for hierarchy queries
CREATE NONCLUSTERED INDEX IX_PackagingHeaders_ProductId_ParentId 
ON PackagingHeaders(ProductId, ParentPackagingId)
INCLUDE (Id, PackagingName, PackagingType);

PRINT 'Created index: IX_PackagingHeaders_ProductId_ParentId';

-- ============================================================================
-- PACKAGING DETAILS TABLE INDEXES
-- ============================================================================

-- Index for packaging lookups (item retrieval)
CREATE NONCLUSTERED INDEX IX_PackagingDetails_PackagingHeaderId 
ON PackagingDetails(PackagingHeaderId)
INCLUDE (Id, ItemsId, Quantity);

PRINT 'Created index: IX_PackagingDetails_PackagingHeaderId';

-- Index for item lookups (find packaging containing item)
CREATE NONCLUSTERED INDEX IX_PackagingDetails_ItemsId 
ON PackagingDetails(ItemsId)
INCLUDE (Id, PackagingHeaderId, Quantity);

PRINT 'Created index: IX_PackagingDetails_ItemsId';

-- ============================================================================
-- VERIFY INDEXES
-- ============================================================================
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('Products', 'PackagingHeaders', 'PackagingDetails')
ORDER BY t.name, i.name;

PRINT 'All indexes created successfully!';
