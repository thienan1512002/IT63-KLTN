SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'[worker].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'worker') IS NULL EXEC(N'CREATE SCHEMA [worker];');
    CREATE TABLE [worker].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [worker].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906132615_SqlServerWorkerBaseline'
)
BEGIN
    IF SCHEMA_ID(N'worker') IS NULL EXEC(N'CREATE SCHEMA [worker];');
END;

IF NOT EXISTS (
    SELECT * FROM [worker].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906132615_SqlServerWorkerBaseline'
)
BEGIN
    CREATE TABLE [worker].[EmailMasterDatas] (
        [Id] bigint NOT NULL IDENTITY,
        [NotificationType] nvarchar(50) NULL,
        [WorkflowAction] nvarchar(50) NULL,
        [ToAddress] nvarchar(500) NULL,
        [CcAddresses] nvarchar(2000) NULL,
        [Subject] nvarchar(1000) NULL,
        [TemplateDataJson] nvarchar(max) NULL,
        [Status] nvarchar(20) NOT NULL,
        [AttemptNumber] int NOT NULL,
        [RetryAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [SentAt] datetime2 NULL,
        CONSTRAINT [PK_EmailMasterDatas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [worker].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906132615_SqlServerWorkerBaseline'
)
BEGIN
    CREATE TABLE [worker].[EmailLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [EmailMasterDataId] bigint NOT NULL,
        [IsSuccess] bit NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmailLogs_EmailMasterDatas_EmailMasterDataId] FOREIGN KEY ([EmailMasterDataId]) REFERENCES [worker].[EmailMasterDatas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [worker].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906132615_SqlServerWorkerBaseline'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_CreatedAt] ON [worker].[EmailLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [worker].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906132615_SqlServerWorkerBaseline'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_EmailMasterDataId] ON [worker].[EmailLogs] ([EmailMasterDataId]);
END;

IF NOT EXISTS (
    SELECT * FROM [worker].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906132615_SqlServerWorkerBaseline'
)
BEGIN
    CREATE INDEX [IX_EmailMasterDatas_CreatedAt] ON [worker].[EmailMasterDatas] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [worker].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906132615_SqlServerWorkerBaseline'
)
BEGIN
    CREATE INDEX [IX_EmailMasterDatas_Status] ON [worker].[EmailMasterDatas] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [worker].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906132615_SqlServerWorkerBaseline'
)
BEGIN
    INSERT INTO [worker].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906132615_SqlServerWorkerBaseline', N'10.0.5');
END;

COMMIT;
GO

