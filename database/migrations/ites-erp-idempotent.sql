IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [Companies] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(40) NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Address] nvarchar(400) NULL,
        [Phone] nvarchar(40) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NULL,
        [Name] nvarchar(max) NULL,
        [ContactName] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [Items] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NULL,
        [Name] nvarchar(max) NULL,
        [Unit] nvarchar(max) NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [ReorderLevel] decimal(18,3) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Items] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [MenuItems] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NULL,
        [Url] nvarchar(max) NULL,
        [PermissionCode] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NULL,
        [Name] nvarchar(max) NULL,
        [Module] nvarchar(max) NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(450) NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [Suppliers] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(450) NULL,
        [Name] nvarchar(max) NULL,
        [ContactName] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [Branches] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [Code] nvarchar(40) NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Address] nvarchar(400) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Branches_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [PermissionId] int NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [InventoryTransactions] (
        [Id] int NOT NULL IDENTITY,
        [BranchId] int NOT NULL,
        [ItemId] int NOT NULL,
        [QuantityDelta] decimal(18,3) NOT NULL,
        [TransactionType] nvarchar(max) NULL,
        [Reference] nvarchar(max) NULL,
        [OccurredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryTransactions_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryTransactions_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [SalesOrders] (
        [Id] int NOT NULL IDENTITY,
        [Number] nvarchar(450) NULL,
        [BranchId] int NOT NULL,
        [CustomerId] int NOT NULL,
        [OrderedAt] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_SalesOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalesOrders_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [BranchId] int NULL,
        [UserName] nvarchar(100) NOT NULL,
        [Email] nvarchar(160) NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [SalesOrderLines] (
        [Id] int NOT NULL IDENTITY,
        [SalesOrderId] int NOT NULL,
        [ItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_SalesOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalesOrderLines_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderLines_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [SalesOrders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [PurchaseRequests] (
        [Id] int NOT NULL IDENTITY,
        [Number] nvarchar(450) NULL,
        [BranchId] int NOT NULL,
        [RequestedById] int NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_PurchaseRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseRequests_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseRequests_Users_RequestedById] FOREIGN KEY ([RequestedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [StockAdjustments] (
        [Id] int NOT NULL IDENTITY,
        [Reference] nvarchar(450) NULL,
        [BranchId] int NOT NULL,
        [ItemId] int NOT NULL,
        [QuantityDelta] decimal(18,3) NOT NULL,
        [AdjustedAt] datetime2 NOT NULL,
        [Reason] nvarchar(max) NULL,
        [AdjustedById] int NOT NULL,
        CONSTRAINT [PK_StockAdjustments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockAdjustments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockAdjustments_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockAdjustments_Users_AdjustedById] FOREIGN KEY ([AdjustedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [PurchaseOrders] (
        [Id] int NOT NULL IDENTITY,
        [Number] nvarchar(450) NULL,
        [BranchId] int NOT NULL,
        [SupplierId] int NOT NULL,
        [PurchaseRequestId] int NULL,
        [OrderedAt] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseOrders_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrders_PurchaseRequests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId]) REFERENCES [PurchaseRequests] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [PurchaseRequestLines] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseRequestId] int NOT NULL,
        [ItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [EstimatedUnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PurchaseRequestLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseRequestLines_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseRequestLines_PurchaseRequests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId]) REFERENCES [PurchaseRequests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [GoodsReceipts] (
        [Id] int NOT NULL IDENTITY,
        [Number] nvarchar(450) NULL,
        [BranchId] int NOT NULL,
        [PurchaseOrderId] int NOT NULL,
        [ReceivedAt] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_GoodsReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GoodsReceipts_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [PurchaseOrderLines] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseOrderId] int NOT NULL,
        [ItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PurchaseOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseOrderLines_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE TABLE [GoodsReceiptLines] (
        [Id] int NOT NULL IDENTITY,
        [GoodsReceiptId] int NOT NULL,
        [PurchaseOrderLineId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_GoodsReceiptLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId] FOREIGN KEY ([GoodsReceiptId]) REFERENCES [GoodsReceipts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GoodsReceiptLines_PurchaseOrderLines_PurchaseOrderLineId] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [PurchaseOrderLines] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_CompanyId_Code] ON [Branches] ([CompanyId], [Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Companies_Code] ON [Companies] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_Code] ON [Customers] ([Code]) WHERE [Code] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_GoodsReceiptLines_GoodsReceiptId] ON [GoodsReceiptLines] ([GoodsReceiptId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_GoodsReceiptLines_PurchaseOrderLineId] ON [GoodsReceiptLines] ([PurchaseOrderLineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_GoodsReceipts_BranchId] ON [GoodsReceipts] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_GoodsReceipts_Number] ON [GoodsReceipts] ([Number]) WHERE [Number] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_GoodsReceipts_PurchaseOrderId] ON [GoodsReceipts] ([PurchaseOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_BranchId] ON [InventoryTransactions] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_ItemId] ON [InventoryTransactions] ([ItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Items_Code] ON [Items] ([Code]) WHERE [Code] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Permissions_Code] ON [Permissions] ([Code]) WHERE [Code] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_ItemId] ON [PurchaseOrderLines] ([ItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_PurchaseOrderId] ON [PurchaseOrderLines] ([PurchaseOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_BranchId] ON [PurchaseOrders] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PurchaseOrders_Number] ON [PurchaseOrders] ([Number]) WHERE [Number] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_PurchaseRequestId] ON [PurchaseOrders] ([PurchaseRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_PurchaseRequestLines_ItemId] ON [PurchaseRequestLines] ([ItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_PurchaseRequestLines_PurchaseRequestId] ON [PurchaseRequestLines] ([PurchaseRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_PurchaseRequests_BranchId] ON [PurchaseRequests] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PurchaseRequests_Number] ON [PurchaseRequests] ([Number]) WHERE [Number] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_PurchaseRequests_RequestedById] ON [PurchaseRequests] ([RequestedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [RolePermissions] ([RoleId], [PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]) WHERE [Name] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_SalesOrderLines_ItemId] ON [SalesOrderLines] ([ItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_SalesOrderLines_SalesOrderId] ON [SalesOrderLines] ([SalesOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_BranchId] ON [SalesOrders] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_CustomerId] ON [SalesOrders] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SalesOrders_Number] ON [SalesOrders] ([Number]) WHERE [Number] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_StockAdjustments_AdjustedById] ON [StockAdjustments] ([AdjustedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_StockAdjustments_BranchId] ON [StockAdjustments] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_StockAdjustments_ItemId] ON [StockAdjustments] ([ItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StockAdjustments_Reference] ON [StockAdjustments] ([Reference]) WHERE [Reference] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Suppliers_Code] ON [Suppliers] ([Code]) WHERE [Code] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [UserRoles] ([UserId], [RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE INDEX [IX_Users_BranchId] ON [Users] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119171352_InitialErpSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261119171352_InitialErpSchema', N'8.0.34');
END;
GO

COMMIT;
GO

