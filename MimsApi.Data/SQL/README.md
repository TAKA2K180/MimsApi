# SQL Folder Guide

This folder contains the database-first SQL scripts for the product and packaging hierarchy model used by the API.

## Purpose

The scripts in this folder create and populate a SQL Server schema for:

- products
- hierarchical packaging
- items contained in packaging
- reporting views
- stored procedures used by the API
- supporting indexes

The design supports nested packaging structures such as a master box containing child boxes and packets, with items stored at any packaging level.

## Schema Overview

### 1. Products
Represents the main product entity.

Typical columns:
- `Id`
- `ProductName`

A product can have many packaging headers.

### 2. PackagingHeaders
Represents packaging containers for a product.

Typical columns:
- `Id`
- `PackagingName`
- `PackagingType`
- `ProductId`
- `ParentPackagingId`

This table models the hierarchy.

Rules:
- `ProductId` links the package to a product
- `ParentPackagingId` is `NULL` for root packaging
- `ParentPackagingId` points to another `PackagingHeaders.Id` for nested packaging

### 3. Items
Represents individual items that can be placed in packaging.

Typical columns:
- `Id`
- `ItemName`

### 4. PackagingDetails
Represents the items directly contained in a package.

Typical columns:
- `Id`
- `PackagingHeaderId`
- `ItemsId`
- `Quantity`

This separates package structure from package contents.

## Relationship Model

### Product -> PackagingHeaders
One-to-many.

Reason:
- a product can have multiple root or nested packages

### PackagingHeaders -> PackagingHeaders
Self-referencing one-to-many.

Reason:
- supports arbitrary nesting depth
- models parent-child packaging trees cleanly

### PackagingHeaders -> PackagingDetails
One-to-many.

Reason:
- a package can contain multiple item rows

### Items -> PackagingDetails
One-to-many.

Reason:
- the same item type can appear in multiple packages

## Design Choices

### Why use `PackagingHeaders` and `PackagingDetails` separately?
Because hierarchy and contents are different concerns.

- `PackagingHeaders` defines structure
- `PackagingDetails` defines direct item membership

This allows:
- packages with no direct items
- packages with only child packages
- packages with both items and child packages

### Why use a self-reference for packaging?
A self-referencing foreign key is the simplest relational way to represent nested packaging.

Benefits:
- easy recursive queries
- easy hierarchy reconstruction in SQL or application code
- aligns with the nested API response shape

### Why store `PackagingType` as text?
The current schema uses values like `Box` and `Packet` directly.

Benefits:
- simple schema
- readable SQL output
- no extra lookup table required

Tradeoff:
- if package types become managed reference data, a separate table would be better

### Why allow root packages with no items?
The sample data uses a root package that contains only child packages.

Reason:
- this is common in real packaging structures
- it matches the required nested JSON output

## Views in This Folder

### `vw_ProductList`
Simple product listing with packaging counts.

Use when:
- listing products with lightweight summary data

### `vw_PackagingHierarchy`
Recursive hierarchy view with nesting level and hierarchy path.

Use when:
- traversing package trees
- reporting nesting depth
- ordering hierarchy output

### `vw_PackagingItems`
Flattened view of package-item relationships with hierarchy info.

Use when:
- listing items by package
- reporting package contents

### `vw_ProductPackagingSummary`
Aggregated product summary view.

Use when:
- reporting total packaging count
- reporting root packaging count
- reporting unique items and total quantity

### `vw_ProductWithStats`
Product statistics view.

Use when:
- retrieving product-level metrics for dashboards or summaries

## Stored Procedures in This Folder

### Product CRUD
- `sp_GetAllProducts`
- `sp_GetProductsPaged`
- `sp_GetProductById`
- `sp_CreateProduct`
- `sp_UpdateProduct`
- `sp_DeleteProduct`
- `sp_ProductExists`

### Packaging Hierarchy / Reporting
- `sp_GetProductWithPackagingHierarchy`
- `sp_GetPackagingContainingItem`
- `sp_GetAllItemsInPackaging`
- `sp_ValidatePackagingHierarchy`
- `sp_GetPackagingStatistics`

These procedures are used by the API service layer instead of direct ORM queries.

## Recommended Execution Order

Run the scripts in this order:

1. `01_CREATE_TABLES.sql`
2. `01_CREATE_PRODUCT_STORED_PROCEDURES.sql` or `02_CREATE_PRODUCT_STORED_PROCEDURES.sql` depending on the final naming used in the folder
3. `03_CREATE_INDEXES.sql`
4. `04_CREATE_PRODUCT_VIEWS.sql`
5. `05_INSERT_PRODUCT_QUERY.sql`

If your stored procedures depend on views, create the views before executing those procedures.

Recommended practical order for this folder:

1. tables
2. views
3. stored procedures
4. indexes
5. sample data

## Sample Data Structure

The sample insert script creates the product `Self adjusting table` with this hierarchy:

- Master Box
  - Table Top
    - Table top
  - Table Legs
    - Table legs
  - Tools Packet
    - Screwdriver
    - Screws Packet
      - Screws

This demonstrates:
- root packaging
- nested packaging
- direct item membership
- mixed package and item containment

## How to Query the Data

### Get all products
```sql
SELECT *
FROM Products;
```

### Get all packaging for a product
```sql
SELECT *
FROM PackagingHeaders
WHERE ProductId = 1;
```

### Get root packaging only
```sql
SELECT *
FROM PackagingHeaders
WHERE ProductId = 1
  AND ParentPackagingId IS NULL;
```

### Get child packages of a package
```sql
SELECT *
FROM PackagingHeaders
WHERE ParentPackagingId = 1;
```

### Get direct items in a package
```sql
SELECT 
    ph.Id AS PackageId,
    ph.PackagingName,
    i.Id AS ItemId,
    i.ItemName,
    pd.Quantity
FROM PackagingDetails pd
INNER JOIN PackagingHeaders ph ON ph.Id = pd.PackagingHeaderId
INNER JOIN Items i ON i.Id = pd.ItemsId
WHERE ph.Id = 2;
```

### Get hierarchy using the view
```sql
SELECT *
FROM vw_PackagingHierarchy
WHERE ProductId = 1
ORDER BY NestingLevel, PackagingName;
```

### Get package items using the view
```sql
SELECT *
FROM vw_PackagingItems
WHERE ProductId = 1
ORDER BY PackagingId, ItemName;
```

### Get product summary using the view
```sql
SELECT *
FROM vw_ProductPackagingSummary
WHERE Id = 1;
```

### Get product stats using the view
```sql
SELECT *
FROM vw_ProductWithStats
WHERE Id = 1;
```

### Get full hierarchy using the stored procedure
```sql
EXEC sp_GetProductWithPackagingHierarchy @ProductId = 1;
```

### Get all nested items in a package
```sql
EXEC sp_GetAllItemsInPackaging @PackagingId = 1;
```

### Find where an item is located
```sql
EXEC sp_GetPackagingContainingItem @ItemId = 1;
```

### Get packaging statistics
```sql
EXEC sp_GetPackagingStatistics @ProductId = 1;
```

## Notes on Constraints and Integrity

The schema is intended to enforce:
- valid product ownership
- valid package nesting
- valid item references
- no orphaned detail rows

The hierarchy validation procedure helps prevent circular references when changing parent-child relationships.

## Notes on API Mapping

The API builds nested JSON from:
- product row
- hierarchy rows
- package item rows

The SQL model is intentionally normalized, while the API response is hierarchical.

That separation keeps the database efficient and the API consumer-friendly.

## Folder Contents Summary

Typical files in this folder:
- table creation script
- stored procedure creation script
- index creation script
- view creation script
- sample insert script

## Summary

This SQL folder implements a database-first model for:
- products
- nested packaging
- package-contained items
- reporting and hierarchy queries

It is designed to support both direct SQL reporting and API-driven hierarchical responses.

Notes:
There are 2 options for adjustments and improvements for the database schema and stored procedures:
A. User code-first migration using EF and then generate SQL scripts from the resulting schema.
B. Manually adjust the SQL scripts in this folder to reflect the final schema and stored procedure design, ensuring they are production-ready and match the API requirements.
