-- ============================================================================
-- INSERT QUERY: Self Adjusting Table with Hierarchical Packaging
-- ============================================================================
-- This script inserts sample data for the "Self adjusting table" product
-- with a complete hierarchical packaging structure including nested items
-- ============================================================================

-- Step 1: Insert the Product
-- ============================================================================
INSERT INTO Products (ProductName)
VALUES ('Self adjusting table');

-- Get the Product ID (will be used in subsequent INSERTs)
-- Note: In production, you would capture this with SCOPE_IDENTITY() or OUTPUT clause
DECLARE @ProductId INT = SCOPE_IDENTITY();

-- Step 2: Insert Items
-- ============================================================================
INSERT INTO Items (ItemName)
VALUES 
    ('Table top'),      -- ItemId will be @TableTopItemId
    ('Table legs'),     -- ItemId will be @TableLegsItemId
    ('Screwdriver'),    -- ItemId will be @ScrewdriverItemId
    ('Screws');         -- ItemId will be @ScrewsItemId

-- Capture Item IDs
DECLARE @TableTopItemId INT = (SELECT Id FROM Items WHERE ItemName = 'Table top');
DECLARE @TableLegsItemId INT = (SELECT Id FROM Items WHERE ItemName = 'Table legs');
DECLARE @ScrewdriverItemId INT = (SELECT Id FROM Items WHERE ItemName = 'Screwdriver');
DECLARE @ScrewsItemId INT = (SELECT Id FROM Items WHERE ItemName = 'Screws');

-- Step 3: Insert Root Packaging (Master Box)
-- ============================================================================
-- Root packaging: 1 Box containing everything
INSERT INTO PackagingHeaders (PackagingName, PackagingType, ProductId, ParentPackagingId)
VALUES ('Master Box', 'Box', @ProductId, NULL);

DECLARE @MasterBoxId INT = SCOPE_IDENTITY();

-- Step 4: Insert Level 1 Child Packaging (3 boxes inside Master Box)
-- ============================================================================
-- Box 1: Contains Table Top
INSERT INTO PackagingHeaders (PackagingName, PackagingType, ProductId, ParentPackagingId)
VALUES ('Table Top', 'Box', @ProductId, @MasterBoxId);

DECLARE @TableTopBoxId INT = SCOPE_IDENTITY();

-- Box 2: Contains Table Legs
INSERT INTO PackagingHeaders (PackagingName, PackagingType, ProductId, ParentPackagingId)
VALUES ('Table Legs', 'Box', @ProductId, @MasterBoxId);

DECLARE @TableLegsBoxId INT = SCOPE_IDENTITY();

-- Packet 3: Contains Screwdriver + nested packet with Screws
INSERT INTO PackagingHeaders (PackagingName, PackagingType, ProductId, ParentPackagingId)
VALUES ('Tools Packet', 'Packet', @ProductId, @MasterBoxId);

DECLARE @ToolsPacketId INT = SCOPE_IDENTITY();

-- Step 5: Insert Level 2 Child Packaging (Screws packet inside Tools packet)
-- ============================================================================
-- Packet inside Tools Packet: Contains Screws
INSERT INTO PackagingHeaders (PackagingName, PackagingType, ProductId, ParentPackagingId)
VALUES ('Screws Packet', 'Packet', @ProductId, @ToolsPacketId);

DECLARE @ScrewsPacketId INT = SCOPE_IDENTITY();

-- Step 6: Insert Packaging Details (Items with quantities)
-- ============================================================================

-- Master Box - no items directly (items are in child packaging)
-- This demonstrates nested structure where container has no direct items

-- Table Top Box contains: Table top (1 unit)
INSERT INTO PackagingDetails (PackagingHeaderId, ItemsId, Quantity)
VALUES (@TableTopBoxId, @TableTopItemId, 1);

-- Table Legs Box contains: Table legs (1 unit)
INSERT INTO PackagingDetails (PackagingHeaderId, ItemsId, Quantity)
VALUES (@TableLegsBoxId, @TableLegsItemId, 1);

-- Tools Packet contains: Screwdriver (1 unit)
INSERT INTO PackagingDetails (PackagingHeaderId, ItemsId, Quantity)
VALUES (@ToolsPacketId, @ScrewdriverItemId, 1);

-- Screws Packet contains: Screws (100 units)
INSERT INTO PackagingDetails (PackagingHeaderId, ItemsId, Quantity)
VALUES (@ScrewsPacketId, @ScrewsItemId, 100);

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- View 1: See the hierarchy structure
PRINT '=== HIERARCHY STRUCTURE ==='
SELECT * FROM vw_PackagingHierarchy 
WHERE ProductId = @ProductId
ORDER BY NestingLevel, PackagingName;

-- View 2: See all items in packaging
PRINT '=== ITEMS IN PACKAGING ==='
SELECT 
    p.PackagingName,
    p.PackagingType,
    i.ItemName,
    pd.Quantity,
    ph.NestingLevel
FROM vw_PackagingItems phi
INNER JOIN PackagingHeaders p ON phi.PackagingId = p.Id
INNER JOIN Items i ON phi.ItemId = i.Id
INNER JOIN PackagingDetails pd ON p.Id = pd.PackagingHeaderId AND i.Id = pd.ItemsId
LEFT JOIN vw_PackagingHierarchy ph ON p.Id = ph.Id
WHERE p.ProductId = @ProductId
ORDER BY p.Id, i.ItemName;

-- View 3: Product summary
PRINT '=== PRODUCT SUMMARY ==='
SELECT * FROM vw_ProductPackagingSummary
WHERE ProductName = 'Self adjusting table';

-- View 4: Get complete hierarchy
PRINT '=== COMPLETE STRUCTURE ==='
EXEC sp_GetProductWithPackagingHierarchy @ProductId = @ProductId;

-- View 5: Get all items in master box (including nested)
PRINT '=== ALL ITEMS IN MASTER BOX (INCLUDING NESTED) ==='
EXEC sp_GetAllItemsInPackaging @PackagingId = @MasterBoxId;

-- View 6: Find where screwdriver is located
PRINT '=== FIND SCREWDRIVER LOCATIONS ==='
EXEC sp_GetPackagingContainingItem @ItemId = @ScrewdriverItemId;

-- View 7: Find where screws are located
PRINT '=== FIND SCREWS LOCATIONS ==='
EXEC sp_GetPackagingContainingItem @ItemId = @ScrewsItemId;

-- View 8: Get statistics
PRINT '=== PACKAGING STATISTICS ==='
EXEC sp_GetPackagingStatistics @ProductId = @ProductId;

-- ============================================================================
-- HIERARCHY VISUALIZATION
-- ============================================================================
/*
STRUCTURE CREATED:

Master Box (Root - Nesting Level 0)
    +- Table Top Box (Nesting Level 1)
    ¦   +- Table top (1x)
    ¦
    +- Table Legs Box (Nesting Level 1)
    ¦   +- Table legs (1x)
    ¦
    +- Tools Packet (Nesting Level 1)
        +- Screwdriver (1x)
        ¦
        +- Screws Packet (Nesting Level 2)
            +- Screws (100x)

TOTAL STRUCTURE:
- Total Packaging: 5
- Root Packaging: 1
- Max Nesting Level: 2
- Unique Items: 4
- Total Item Quantity: 103
*/

-- ============================================================================
-- CLEANUP (Optional - uncomment to remove test data)
-- ============================================================================
/*
 To remove this test data, execute:
DELETE FROM PackagingDetails 
WHERE PackagingHeaderId IN (
    SELECT Id FROM PackagingHeaders WHERE ProductId = 1
);

DELETE FROM PackagingHeaders 
WHERE ProductId = 1;

DELETE FROM Products 
WHERE ProductName = 'Self adjusting table';

DELETE FROM Items 
WHERE ItemName IN ('Table top', 'Table legs', 'Screwdriver', 'Screws');
*/
