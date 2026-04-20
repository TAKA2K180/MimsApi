-- ============================================================================
-- DATABASE-FIRST APPROACH: VIEWS FOR PRODUCT QUERIES
-- ============================================================================
-- These views support the stored procedures and provide optimized queries
-- ============================================================================

-- ============================================================================
-- DROP EXISTING VIEWS
-- ============================================================================
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_ProductList')
    DROP VIEW vw_ProductList;
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_ProductWithStats')
    DROP VIEW vw_ProductWithStats;

-- ============================================================================
-- 1. PRODUCT LIST VIEW - Simple product listing
-- ============================================================================
CREATE VIEW vw_ProductList
AS
SELECT 
    p.Id,
    p.ProductName,
    COUNT(DISTINCT ph.Id) AS TotalPackagings,
    COUNT(DISTINCT ph.Id) - ISNULL(
        (SELECT COUNT(DISTINCT Id) FROM PackagingHeaders WHERE ParentPackagingId IS NOT NULL AND ProductId = p.Id),
        0
    ) AS RootPackagings
FROM Products p
LEFT JOIN PackagingHeaders ph ON p.Id = ph.ProductId
GROUP BY p.Id, p.ProductName;
GO

-- ============================================================================
-- 2. PRODUCT WITH STATS VIEW - Product with packaging statistics
-- ============================================================================
CREATE VIEW vw_ProductWithStats
AS
SELECT 
    p.Id,
    p.ProductName,
    ISNULL(COUNT(DISTINCT ph.Id), 0) AS TotalPackagings,
    ISNULL(
        (SELECT COUNT(DISTINCT Id) 
         FROM PackagingHeaders 
         WHERE ParentPackagingId IS NULL AND ProductId = p.Id),
        0
    ) AS RootPackagings,
    ISNULL(
        (SELECT MAX(pch.NestingLevel)
         FROM vw_PackagingHierarchy pch
         WHERE pch.ProductId = p.Id),
        0
    ) AS MaxNestingLevel,
    ISNULL(COUNT(DISTINCT pd.ItemsId), 0) AS UniqueItems,
    ISNULL(SUM(pd.Quantity), 0) AS TotalItemQuantity
FROM Products p
LEFT JOIN PackagingHeaders ph ON p.Id = ph.ProductId
LEFT JOIN PackagingDetails pd ON ph.Id = pd.PackagingHeaderId
GROUP BY p.Id, p.ProductName;
GO

-- ============================================================================
-- SUMMARY: VIEWS CREATED
-- ============================================================================
-- vw_ProductList         - Simple product listing with packaging counts
-- vw_ProductWithStats    - Products with full statistics
-- ============================================================================

PRINT 'All product views created successfully!';
