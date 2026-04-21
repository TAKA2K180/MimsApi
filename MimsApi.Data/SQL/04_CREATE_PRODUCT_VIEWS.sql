-- ============================================================================
-- VIEWS FOR PRODUCT QUERIES
-- ============================================================================
-- These views support the stored procedures and provide optimized queries
-- ============================================================================

-- ============================================================================
-- DROP EXISTING VIEWS
-- ============================================================================
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_PackagingItems')
    DROP VIEW vw_PackagingItems;
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_ProductPackagingSummary')
    DROP VIEW vw_ProductPackagingSummary;
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_ProductWithStats')
    DROP VIEW vw_ProductWithStats;
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_ProductList')
    DROP VIEW vw_ProductList;
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_PackagingHierarchy')
    DROP VIEW vw_PackagingHierarchy;
GO

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
-- 2. PACKAGING HIERARCHY VIEW - Recursive packaging hierarchy
-- ============================================================================
CREATE VIEW [dbo].[vw_PackagingHierarchy]
AS
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
GO

-- ============================================================================
-- 3. PACKAGING ITEMS VIEW - Items in packaging with hierarchy info
-- ============================================================================
CREATE VIEW [dbo].[vw_PackagingItems]
AS
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
GO

-- ============================================================================
-- 4. PRODUCT PACKAGING SUMMARY VIEW - Product summary for reporting
-- ============================================================================
CREATE VIEW [dbo].[vw_ProductPackagingSummary]
AS
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
GO

-- ============================================================================
-- 5. PRODUCT WITH STATS VIEW - Product with packaging statistics
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
-- vw_ProductList             - Simple product listing with packaging counts
-- vw_PackagingHierarchy      - Recursive packaging hierarchy with path
-- vw_PackagingItems          - Items in packaging with nesting level
-- vw_ProductPackagingSummary - Product summary for reporting
-- vw_ProductWithStats        - Products with full statistics
-- ============================================================================

PRINT 'All product views created successfully!';
