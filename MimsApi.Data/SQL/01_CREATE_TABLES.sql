IF OBJECT_ID('dbo.PackagingDetails', 'U') IS NOT NULL DROP TABLE dbo.PackagingDetails;
IF OBJECT_ID('dbo.PackagingHeaders', 'U') IS NOT NULL DROP TABLE dbo.PackagingHeaders;
IF OBJECT_ID('dbo.Items', 'U') IS NOT NULL DROP TABLE dbo.Items;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
GO

CREATE TABLE dbo.Products
(
    Id INT IDENTITY(1,1) NOT NULL,
    ProductName NVARCHAR(255) NOT NULL,
    CONSTRAINT PK_Products PRIMARY KEY (Id)
);
GO

CREATE TABLE dbo.Items
(
    Id INT IDENTITY(1,1) NOT NULL,
    ItemName NVARCHAR(255) NOT NULL,
    CONSTRAINT PK_Items PRIMARY KEY (Id)
);
GO

CREATE TABLE dbo.Users
(
    Id INT IDENTITY(1,1) NOT NULL,
    Username NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    [Password] NVARCHAR(500) NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE()),
    LastLogin DATETIME2 NULL,
    CONSTRAINT PK_Users PRIMARY KEY (Id)
);
GO

CREATE TABLE dbo.PackagingHeaders
(
    Id INT IDENTITY(1,1) NOT NULL,
    PackagingName NVARCHAR(255) NOT NULL,
    PackagingType NVARCHAR(100) NOT NULL,
    ProductId INT NOT NULL,
    ParentPackagingId INT NULL,
    CONSTRAINT PK_PackagingHeaders PRIMARY KEY (Id),
    CONSTRAINT FK_PackagingHeaders_Products_ProductId
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_PackagingHeaders_PackagingHeaders_ParentPackagingId
        FOREIGN KEY (ParentPackagingId) REFERENCES dbo.PackagingHeaders(Id)
        ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.PackagingDetails
(
    Id INT IDENTITY(1,1) NOT NULL,
    PackagingHeaderId INT NOT NULL,
    ItemsId INT NOT NULL,
    Quantity INT NOT NULL,
    CONSTRAINT PK_PackagingDetails PRIMARY KEY (Id),
    CONSTRAINT FK_PackagingDetails_PackagingHeaders_PackagingHeaderId
        FOREIGN KEY (PackagingHeaderId) REFERENCES dbo.PackagingHeaders(Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_PackagingDetails_Items_ItemsId
        FOREIGN KEY (ItemsId) REFERENCES dbo.Items(Id)
        ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX IX_Users_Username_Unique
    ON dbo.Users(Username);
GO

CREATE UNIQUE INDEX IX_Users_Email_Unique
    ON dbo.Users(Email);
GO

CREATE INDEX IX_PackagingHeaders_ProductId
    ON dbo.PackagingHeaders(ProductId);
GO

CREATE INDEX IX_PackagingHeaders_ParentPackagingId
    ON dbo.PackagingHeaders(ParentPackagingId);
GO

CREATE INDEX IX_PackagingDetails_PackagingHeaderId
    ON dbo.PackagingDetails(PackagingHeaderId);
GO

CREATE INDEX IX_PackagingDetails_ItemsId
    ON dbo.PackagingDetails(ItemsId);
GO