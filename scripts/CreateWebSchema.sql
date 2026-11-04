SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [AccountStatusLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [Username] nvarchar(200) NULL,
        [NewStatus] int NOT NULL,
        [Reason] int NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [Comment] nvarchar(1000) NULL,
        CONSTRAINT [PK_AccountStatusLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [ApprovalCases] (
        [CaseId] nvarchar(50) NOT NULL,
        [CaseName] nvarchar(200) NULL,
        [Description] nvarchar(300) NULL,
        [BelongSite] nvarchar(100) NULL,
        [FormCode] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ApprovalCases] PRIMARY KEY ([CaseId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [ApprovalRoles] (
        [Id] nvarchar(50) NOT NULL,
        [Title] nvarchar(500) NOT NULL,
        [LocalizationKey] nvarchar(200) NULL,
        [BelongSite] int NOT NULL,
        [FormCode] nvarchar(200) NULL,
        [RoleType] int NOT NULL,
        [DepartmentId] int NULL,
        [SectionId] int NULL,
        [FormApprovalId] int NULL,
        [IdentityRoleName] nvarchar(256) NULL,
        [IsActive] bit NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [UpdatedBy] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ApprovalRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(max) NULL,
        [CategoryName] nvarchar(max) NULL,
        [CategoryOrder] int NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [BelongSites] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [Code] nvarchar(255) NULL,
        [Description] nvarchar(2000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_BelongSites] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [FormApprovers] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(500) NOT NULL,
        [DisplayTitle] nvarchar(500) NOT NULL,
        [UserNames] nvarchar(1000) NOT NULL,
        [FormCode] nvarchar(200) NOT NULL,
        [IsRemoved] bit NOT NULL,
        CONSTRAINT [PK_FormApprovers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [InactiveSchedules] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(200) NULL,
        [From] datetime2 NOT NULL,
        [To] datetime2 NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DeactivateJobId] nvarchar(100) NULL,
        [ReactivateJobId] nvarchar(100) NULL,
        CONSTRAINT [PK_InactiveSchedules] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [RequestTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [RequireRequestedDate] bit NOT NULL,
        [RequireAmount] bit NOT NULL,
        [RequireReason] bit NOT NULL,
        [AllowAttachment] bit NOT NULL,
        [ApprovalCaseId] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_RequestTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequestTypes_ApprovalCases_ApprovalCaseId] FOREIGN KEY ([ApprovalCaseId]) REFERENCES [ApprovalCases] ([CaseId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [ApprovalSteps] (
        [Id] nvarchar(50) NOT NULL,
        [ApprovalRoleId] nvarchar(50) NULL,
        [ApprovalCaseId] nvarchar(50) NULL,
        [StepOrder] int NOT NULL,
        [ActionTypes] int NOT NULL,
        [ApprovalType] int NOT NULL,
        [ReturnStepId] nvarchar(50) NULL,
        [NextStepId] nvarchar(50) NULL,
        [SendSubmissionNoty] bit NOT NULL,
        [ProcessOnlineOffline] bit NOT NULL,
        [GroupTitle] nvarchar(300) NULL,
        [CheckBypass] bit NOT NULL,
        [BypassToStepId] nvarchar(50) NULL,
        [IsParentStep] bit NULL,
        [IsRemoved] bit NOT NULL,
        CONSTRAINT [PK_ApprovalSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovalSteps_ApprovalCases_ApprovalCaseId] FOREIGN KEY ([ApprovalCaseId]) REFERENCES [ApprovalCases] ([CaseId]),
        CONSTRAINT [FK_ApprovalSteps_ApprovalRoles_ApprovalRoleId] FOREIGN KEY ([ApprovalRoleId]) REFERENCES [ApprovalRoles] ([Id]),
        CONSTRAINT [FK_ApprovalSteps_ApprovalSteps_BypassToStepId] FOREIGN KEY ([BypassToStepId]) REFERENCES [ApprovalSteps] ([Id]),
        CONSTRAINT [FK_ApprovalSteps_ApprovalSteps_NextStepId] FOREIGN KEY ([NextStepId]) REFERENCES [ApprovalSteps] ([Id]),
        CONSTRAINT [FK_ApprovalSteps_ApprovalSteps_ReturnStepId] FOREIGN KEY ([ReturnStepId]) REFERENCES [ApprovalSteps] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [ShortName] nvarchar(300) NULL,
        [Description] nvarchar(2000) NULL,
        [SiteId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [MainApprover] nvarchar(255) NULL,
        [BackupApprover] nvarchar(255) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Departments_BelongSites_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [BelongSites] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [Sections] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [EngName] nvarchar(500) NULL,
        [Note] nvarchar(2000) NULL,
        [ShortName] nvarchar(200) NULL,
        [MainApprover] nvarchar(255) NULL,
        [BackupApprover] nvarchar(255) NULL,
        [PIC] nvarchar(1000) NULL,
        [DeputyApprovers] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [DepartmentId] int NOT NULL,
        [SiteId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Sections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sections_BelongSites_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [BelongSites] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sections_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(500) NOT NULL,
        [Avatar] nvarchar(500) NULL,
        [SectionId] int NULL,
        [SiteId] int NULL,
        [Position] nvarchar(200) NULL,
        [DateOfBirth] datetime2 NULL,
        [EmpId] nvarchar(300) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsOnline] bit NOT NULL,
        [IsOperator] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [HireDate] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_BelongSites_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [BelongSites] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AspNetUsers_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [ApprovalRequests] (
        [Id] int NOT NULL IDENTITY,
        [ApplicationNo] nvarchar(200) NOT NULL,
        [DescriptionNo] nvarchar(200) NULL,
        [SubmitDate] datetime2 NOT NULL,
        [RequestName] nvarchar(400) NOT NULL,
        [UrgencyLevel] bit NOT NULL,
        [FaxCode] nvarchar(200) NOT NULL,
        [SiteName] nvarchar(100) NULL,
        [SiteId] int NULL,
        [FullName] nvarchar(100) NOT NULL,
        [SectionName] nvarchar(200) NOT NULL,
        [SectionId] int NOT NULL,
        [DeptName] nvarchar(400) NOT NULL,
        [DeptId] int NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Position] nvarchar(200) NULL,
        [PhoneNumber] nvarchar(100) NULL,
        [Process] int NOT NULL,
        [TotalStep] int NOT NULL,
        [MainApproval] nvarchar(200) NOT NULL,
        [Complete] bit NOT NULL,
        [CurrentApprovalType] int NOT NULL,
        [FormCode] nvarchar(100) NOT NULL,
        [ApprovalCaseId] nvarchar(50) NULL,
        [IsRemoved] bit NOT NULL,
        [RequestTypeId] int NOT NULL,
        [CreatedByUserId] nvarchar(450) NOT NULL,
        [DepartmentId] int NOT NULL,
        [Title] nvarchar(250) NOT NULL,
        [Content] nvarchar(4000) NOT NULL,
        [RequestedDate] datetime2 NULL,
        [Priority] int NOT NULL,
        [Amount] decimal(18,2) NULL,
        [Reason] nvarchar(2000) NULL,
        [Status] int NOT NULL,
        [CurrentStepOrder] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [SubmittedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        CONSTRAINT [PK_ApprovalRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovalRequests_ApprovalCases_ApprovalCaseId] FOREIGN KEY ([ApprovalCaseId]) REFERENCES [ApprovalCases] ([CaseId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalRequests_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalRequests_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalRequests_RequestTypes_RequestTypeId] FOREIGN KEY ([RequestTypeId]) REFERENCES [RequestTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [ApprovalDetails] (
        [Id] int NOT NULL IDENTITY,
        [ApprovalStepId] nvarchar(50) NOT NULL,
        [RequestId] int NOT NULL,
        [Comment] nvarchar(max) NULL,
        [ApprovedDate] datetime2 NULL,
        [IsApproved] bit NOT NULL,
        [ApprovedBy] nvarchar(200) NULL,
        [StepOrder] int NOT NULL,
        [IsRemove] bit NOT NULL,
        [IsCurrentStep] bit NOT NULL,
        [CurrentStatus] int NOT NULL,
        [ProcessOnlineOffline] bit NOT NULL,
        [IsRequester] bit NOT NULL,
        [IsLeader] bit NOT NULL,
        [StepTitle] nvarchar(500) NULL,
        [GroupTitle] nvarchar(300) NULL,
        [ActionTypes] int NOT NULL,
        [ApprovalType] int NOT NULL,
        [ReturnToStepId] int NULL,
        [NextStepId] int NULL,
        [SendSubmissionNoty] bit NOT NULL,
        [CheckBypass] bit NOT NULL,
        [BypassFromStepId] int NULL,
        [BypassToStepId] int NULL,
        CONSTRAINT [PK_ApprovalDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovalDetails_ApprovalDetails_BypassFromStepId] FOREIGN KEY ([BypassFromStepId]) REFERENCES [ApprovalDetails] ([Id]),
        CONSTRAINT [FK_ApprovalDetails_ApprovalDetails_BypassToStepId] FOREIGN KEY ([BypassToStepId]) REFERENCES [ApprovalDetails] ([Id]),
        CONSTRAINT [FK_ApprovalDetails_ApprovalDetails_NextStepId] FOREIGN KEY ([NextStepId]) REFERENCES [ApprovalDetails] ([Id]),
        CONSTRAINT [FK_ApprovalDetails_ApprovalDetails_ReturnToStepId] FOREIGN KEY ([ReturnToStepId]) REFERENCES [ApprovalDetails] ([Id]),
        CONSTRAINT [FK_ApprovalDetails_ApprovalRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ApprovalRequests] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ApprovalDetails_ApprovalSteps_ApprovalStepId] FOREIGN KEY ([ApprovalStepId]) REFERENCES [ApprovalSteps] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [NotiToasts] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(250) NULL,
        [Content] nvarchar(2000) NULL,
        [SendFor] nvarchar(255) NULL,
        [Url] nvarchar(500) NULL,
        [IsRead] bit NOT NULL,
        [RequestId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ContentKey] nvarchar(500) NULL,
        [ContentArgsJson] nvarchar(2000) NULL,
        [NeedTranslate] bit NOT NULL,
        CONSTRAINT [PK_NotiToasts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotiToasts_ApprovalRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ApprovalRequests] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [RequestAttachments] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [StoredFileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(150) NULL,
        [FileSize] bigint NOT NULL,
        [FilePath] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RequestAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequestAttachments_ApprovalRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ApprovalRequests] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [ApprovalHistories] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [ApprovalDetailId] int NULL,
        [RequestCode] nvarchar(50) NOT NULL,
        [ActionByUserId] nvarchar(450) NOT NULL,
        [StepOrder] int NULL,
        [Action] int NOT NULL,
        [Comment] nvarchar(2000) NULL,
        [PreviousStatus] int NOT NULL,
        [NewStatus] int NOT NULL,
        [ActionAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ApprovalHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovalHistories_ApprovalDetails_ApprovalDetailId] FOREIGN KEY ([ApprovalDetailId]) REFERENCES [ApprovalDetails] ([Id]),
        CONSTRAINT [FK_ApprovalHistories_ApprovalRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ApprovalRequests] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE TABLE [ApproverInfos] (
        [Id] int NOT NULL IDENTITY,
        [ApprovalDetailId] int NOT NULL,
        [UserName] nvarchar(200) NULL,
        [FullName] nvarchar(500) NULL,
        [Email] nvarchar(500) NULL,
        [PhoneNumber] nvarchar(11) NULL,
        [SectionName] nvarchar(200) NULL,
        [SectionId] int NOT NULL,
        [Position] nvarchar(100) NULL,
        CONSTRAINT [PK_ApproverInfos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApproverInfos_ApprovalDetails_ApprovalDetailId] FOREIGN KEY ([ApprovalDetailId]) REFERENCES [ApprovalDetails] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CaseId', N'BelongSite', N'CaseName', N'CreatedAt', N'CreatedBy', N'Description', N'FormCode', N'IsActive', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[ApprovalCases]'))
        SET IDENTITY_INSERT [ApprovalCases] ON;
    EXEC(N'INSERT INTO [ApprovalCases] ([CaseId], [BelongSite], [CaseName], [CreatedAt], [CreatedBy], [Description], [FormCode], [IsActive], [UpdatedAt], [UpdatedBy])
    VALUES (N''WF-MULTI'', N''ALL'', N''Phê duyệt hai bước'', ''2026-01-01T00:00:00.0000000Z'', N''seed'', N''Phê duyệt hai bước'', N''WF-MULTI'', CAST(1 AS bit), NULL, N''seed''),
    (N''WF-ONE'', N''ALL'', N''Phê duyệt một bước'', ''2026-01-01T00:00:00.0000000Z'', N''seed'', N''Phê duyệt một bước'', N''WF-ONE'', CAST(1 AS bit), NULL, N''seed'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CaseId', N'BelongSite', N'CaseName', N'CreatedAt', N'CreatedBy', N'Description', N'FormCode', N'IsActive', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[ApprovalCases]'))
        SET IDENTITY_INSERT [ApprovalCases] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BelongSite', N'CreatedAt', N'CreatedBy', N'DepartmentId', N'FormApprovalId', N'FormCode', N'IdentityRoleName', N'IsActive', N'LocalizationKey', N'RoleType', N'SectionId', N'Title', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[ApprovalRoles]'))
        SET IDENTITY_INSERT [ApprovalRoles] ON;
    EXEC(N'INSERT INTO [ApprovalRoles] ([Id], [BelongSite], [CreatedAt], [CreatedBy], [DepartmentId], [FormApprovalId], [FormCode], [IdentityRoleName], [IsActive], [LocalizationKey], [RoleType], [SectionId], [Title], [UpdatedAt], [UpdatedBy])
    VALUES (N''ROLE-DEPT-MANAGER-MULTI'', 0, ''2026-01-01T00:00:00.0000000Z'', N''seed'', NULL, NULL, N''WF-MULTI'', NULL, CAST(1 AS bit), NULL, 4, NULL, N''Trưởng phòng'', NULL, N''seed''),
    (N''ROLE-DEPT-MANAGER-ONE'', 0, ''2026-01-01T00:00:00.0000000Z'', N''seed'', NULL, NULL, N''WF-ONE'', NULL, CAST(1 AS bit), NULL, 4, NULL, N''Trưởng phòng'', NULL, N''seed''),
    (N''ROLE-DIRECTOR'', 0, ''2026-01-01T00:00:00.0000000Z'', N''seed'', NULL, 1, N''WF-MULTI'', NULL, CAST(1 AS bit), NULL, 1, NULL, N''Giám đốc'', NULL, N''seed'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BelongSite', N'CreatedAt', N'CreatedBy', N'DepartmentId', N'FormApprovalId', N'FormCode', N'IdentityRoleName', N'IsActive', N'LocalizationKey', N'RoleType', N'SectionId', N'Title', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[ApprovalRoles]'))
        SET IDENTITY_INSERT [ApprovalRoles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryName', N'CategoryOrder', N'ConcurrencyStamp', N'DisplayName', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] ON;
    EXEC(N'INSERT INTO [AspNetRoles] ([Id], [CategoryName], [CategoryOrder], [ConcurrencyStamp], [DisplayName], [Name], [NormalizedName])
    VALUES (N''a0000001-0000-4000-8000-000000000001'', N''He thong'', 0, N''seed-superuser-stamp-001'', N''Super User'', N''SuperUser'', N''SUPERUSER''),
    (N''a0000001-0000-4000-8000-000000000011'', N''Quản lý nhân sự'', 1, N''seed-hr-view-stamp-001'', N''Xem thông tin nhân sự'', N''HR.Personnel.View'', N''HR.PERSONNEL.VIEW''),
    (N''a0000001-0000-4000-8000-000000000012'', N''Quản lý nhân sự'', 1, N''seed-hr-manage-stamp-001'', N''Quản lý thông tin nhân sự'', N''HR.Personnel.Manage'', N''HR.PERSONNEL.MANAGE''),
    (N''a0000001-0000-4000-8000-000000000021'', N''Phê duyệt'', 1, N''seed-employee'', N''Nhân viên'', N''Employee'', N''EMPLOYEE''),
    (N''a0000001-0000-4000-8000-000000000022'', N''Phê duyệt'', 1, N''seed-approver'', N''Người phê duyệt'', N''Approver'', N''APPROVER'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryName', N'CategoryOrder', N'ConcurrencyStamp', N'DisplayName', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'Description', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[BelongSites]'))
        SET IDENTITY_INSERT [BelongSites] ON;
    EXEC(N'INSERT INTO [BelongSites] ([Id], [Code], [CreatedAt], [Description], [IsActive], [Name], [UpdatedAt])
    VALUES (1, N''YPF'', ''2026-01-01T00:00:00.0000000Z'', N''YP Factory'', CAST(1 AS bit), N''YP Factory'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'Description', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[BelongSites]'))
        SET IDENTITY_INSERT [BelongSites] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DisplayTitle', N'FormCode', N'IsRemoved', N'Title', N'UserNames') AND [object_id] = OBJECT_ID(N'[FormApprovers]'))
        SET IDENTITY_INSERT [FormApprovers] ON;
    EXEC(N'INSERT INTO [FormApprovers] ([Id], [DisplayTitle], [FormCode], [IsRemoved], [Title], [UserNames])
    VALUES (1, N''Giám đốc'', N''WF-MULTI'', CAST(0 AS bit), N''Giám đốc'', N''director1'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DisplayTitle', N'FormCode', N'IsRemoved', N'Title', N'UserNames') AND [object_id] = OBJECT_ID(N'[FormApprovers]'))
        SET IDENTITY_INSERT [FormApprovers] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionTypes', N'ApprovalCaseId', N'ApprovalRoleId', N'ApprovalType', N'BypassToStepId', N'CheckBypass', N'GroupTitle', N'IsParentStep', N'IsRemoved', N'NextStepId', N'ProcessOnlineOffline', N'ReturnStepId', N'SendSubmissionNoty', N'StepOrder') AND [object_id] = OBJECT_ID(N'[ApprovalSteps]'))
        SET IDENTITY_INSERT [ApprovalSteps] ON;
    EXEC(N'INSERT INTO [ApprovalSteps] ([Id], [ActionTypes], [ApprovalCaseId], [ApprovalRoleId], [ApprovalType], [BypassToStepId], [CheckBypass], [GroupTitle], [IsParentStep], [IsRemoved], [NextStepId], [ProcessOnlineOffline], [ReturnStepId], [SendSubmissionNoty], [StepOrder])
    VALUES (N''STEP-MULTI-2'', 23, N''WF-MULTI'', N''ROLE-DIRECTOR'', 1, NULL, CAST(0 AS bit), N'''', NULL, CAST(0 AS bit), NULL, CAST(0 AS bit), NULL, CAST(0 AS bit), 2),
    (N''STEP-ONE-1'', 23, N''WF-ONE'', N''ROLE-DEPT-MANAGER-ONE'', 1, NULL, CAST(0 AS bit), N'''', NULL, CAST(0 AS bit), NULL, CAST(0 AS bit), NULL, CAST(0 AS bit), 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionTypes', N'ApprovalCaseId', N'ApprovalRoleId', N'ApprovalType', N'BypassToStepId', N'CheckBypass', N'GroupTitle', N'IsParentStep', N'IsRemoved', N'NextStepId', N'ProcessOnlineOffline', N'ReturnStepId', N'SendSubmissionNoty', N'StepOrder') AND [object_id] = OBJECT_ID(N'[ApprovalSteps]'))
        SET IDENTITY_INSERT [ApprovalSteps] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BackupApprover', N'CreatedAt', N'CreatedBy', N'Description', N'IsActive', N'MainApprover', N'Name', N'ShortName', N'SiteId', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Departments]'))
        SET IDENTITY_INSERT [Departments] ON;
    EXEC(N'INSERT INTO [Departments] ([Id], [BackupApprover], [CreatedAt], [CreatedBy], [Description], [IsActive], [MainApprover], [Name], [ShortName], [SiteId], [UpdatedAt], [UpdatedBy])
    VALUES (1, N'''', ''2026-01-01T00:00:00.0000000Z'', N''seed'', N''Công nghệ thông tin'', CAST(1 AS bit), N''manager1'', N''Công nghệ thông tin'', N''IT'', 1, NULL, NULL),
    (2, N'''', ''2026-01-01T00:00:00.0000000Z'', N''seed'', N''Tài chính'', CAST(1 AS bit), N''director1'', N''Tài chính'', N''FIN'', 1, NULL, NULL),
    (3, N'''', ''2026-01-01T00:00:00.0000000Z'', N''seed'', N''BOD'', CAST(1 AS bit), N''director1'', N''BOD'', N''BOD'', 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BackupApprover', N'CreatedAt', N'CreatedBy', N'Description', N'IsActive', N'MainApprover', N'Name', N'ShortName', N'SiteId', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Departments]'))
        SET IDENTITY_INSERT [Departments] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowAttachment', N'ApprovalCaseId', N'Code', N'CreatedAt', N'Description', N'IsActive', N'Name', N'RequireAmount', N'RequireReason', N'RequireRequestedDate', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[RequestTypes]'))
        SET IDENTITY_INSERT [RequestTypes] ON;
    EXEC(N'INSERT INTO [RequestTypes] ([Id], [AllowAttachment], [ApprovalCaseId], [Code], [CreatedAt], [Description], [IsActive], [Name], [RequireAmount], [RequireReason], [RequireRequestedDate], [UpdatedAt])
    VALUES (1, CAST(1 AS bit), N''WF-ONE'', N''LEAVE'', ''2026-01-01T00:00:00.0000000Z'', N''Đơn xin nghỉ phép'', CAST(1 AS bit), N''Đơn xin nghỉ phép'', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), NULL),
    (2, CAST(1 AS bit), N''WF-MULTI'', N''PURCHASE'', ''2026-01-01T00:00:00.0000000Z'', N''Đề nghị mua hàng'', CAST(1 AS bit), N''Đề nghị mua hàng'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), NULL),
    (3, CAST(1 AS bit), N''WF-MULTI'', N''PAYMENT'', ''2026-01-01T00:00:00.0000000Z'', N''Đề nghị thanh toán'', CAST(1 AS bit), N''Đề nghị thanh toán'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), NULL),
    (4, CAST(1 AS bit), N''WF-ONE'', N''EQUIPMENT'', ''2026-01-01T00:00:00.0000000Z'', N''Yêu cầu cấp thiết bị'', CAST(1 AS bit), N''Yêu cầu cấp thiết bị'', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), NULL),
    (5, CAST(1 AS bit), N''WF-ONE'', N''OVERTIME'', ''2026-01-01T00:00:00.0000000Z'', N''Yêu cầu làm thêm giờ'', CAST(1 AS bit), N''Yêu cầu làm thêm giờ'', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), NULL),
    (6, CAST(1 AS bit), N''WF-MULTI'', N''INTERNAL'', ''2026-01-01T00:00:00.0000000Z'', N''Đề xuất nội bộ'', CAST(1 AS bit), N''Đề xuất nội bộ'', CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowAttachment', N'ApprovalCaseId', N'Code', N'CreatedAt', N'Description', N'IsActive', N'Name', N'RequireAmount', N'RequireReason', N'RequireRequestedDate', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[RequestTypes]'))
        SET IDENTITY_INSERT [RequestTypes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionTypes', N'ApprovalCaseId', N'ApprovalRoleId', N'ApprovalType', N'BypassToStepId', N'CheckBypass', N'GroupTitle', N'IsParentStep', N'IsRemoved', N'NextStepId', N'ProcessOnlineOffline', N'ReturnStepId', N'SendSubmissionNoty', N'StepOrder') AND [object_id] = OBJECT_ID(N'[ApprovalSteps]'))
        SET IDENTITY_INSERT [ApprovalSteps] ON;
    EXEC(N'INSERT INTO [ApprovalSteps] ([Id], [ActionTypes], [ApprovalCaseId], [ApprovalRoleId], [ApprovalType], [BypassToStepId], [CheckBypass], [GroupTitle], [IsParentStep], [IsRemoved], [NextStepId], [ProcessOnlineOffline], [ReturnStepId], [SendSubmissionNoty], [StepOrder])
    VALUES (N''STEP-MULTI-1'', 23, N''WF-MULTI'', N''ROLE-DEPT-MANAGER-MULTI'', 1, NULL, CAST(0 AS bit), N'''', NULL, CAST(0 AS bit), N''STEP-MULTI-2'', CAST(0 AS bit), NULL, CAST(0 AS bit), 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionTypes', N'ApprovalCaseId', N'ApprovalRoleId', N'ApprovalType', N'BypassToStepId', N'CheckBypass', N'GroupTitle', N'IsParentStep', N'IsRemoved', N'NextStepId', N'ProcessOnlineOffline', N'ReturnStepId', N'SendSubmissionNoty', N'StepOrder') AND [object_id] = OBJECT_ID(N'[ApprovalSteps]'))
        SET IDENTITY_INSERT [ApprovalSteps] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BackupApprover', N'CreatedAt', N'CreatedBy', N'DepartmentId', N'DeputyApprovers', N'EngName', N'IsActive', N'MainApprover', N'Name', N'Note', N'PIC', N'ShortName', N'SiteId', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Sections]'))
        SET IDENTITY_INSERT [Sections] ON;
    EXEC(N'INSERT INTO [Sections] ([Id], [BackupApprover], [CreatedAt], [CreatedBy], [DepartmentId], [DeputyApprovers], [EngName], [IsActive], [MainApprover], [Name], [Note], [PIC], [ShortName], [SiteId], [UpdatedAt], [UpdatedBy])
    VALUES (1, N'''', ''2026-01-01T00:00:00.0000000Z'', N''seed'', 1, N'''', N''IT General'', CAST(1 AS bit), N''manager1'', N''IT General'', N'''', N'''', N''IT General'', 1, NULL, NULL),
    (2, N'''', ''2026-01-01T00:00:00.0000000Z'', N''seed'', 2, N'''', N''Finance General'', CAST(1 AS bit), N''director1'', N''Finance General'', N'''', N'''', N''Finance General'', 1, NULL, NULL),
    (3, N'''', ''2026-01-01T00:00:00.0000000Z'', N''seed'', 3, N'''', N''BOD Office'', CAST(1 AS bit), N''director1'', N''BOD Office'', N'''', N'''', N''BOD Office'', 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BackupApprover', N'CreatedAt', N'CreatedBy', N'DepartmentId', N'DeputyApprovers', N'EngName', N'IsActive', N'MainApprover', N'Name', N'Note', N'PIC', N'ShortName', N'SiteId', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Sections]'))
        SET IDENTITY_INSERT [Sections] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'Avatar', N'ConcurrencyStamp', N'CreatedAt', N'DateOfBirth', N'Email', N'EmailConfirmed', N'EmpId', N'FullName', N'HireDate', N'IsActive', N'IsOnline', N'IsOperator', N'LockoutEnabled', N'LockoutEnd', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'Position', N'SectionId', N'SecurityStamp', N'SiteId', N'TwoFactorEnabled', N'UpdatedAt', N'UserName') AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
        SET IDENTITY_INSERT [AspNetUsers] ON;
    EXEC(N'INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [Avatar], [ConcurrencyStamp], [CreatedAt], [DateOfBirth], [Email], [EmailConfirmed], [EmpId], [FullName], [HireDate], [IsActive], [IsOnline], [IsOperator], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [Position], [SectionId], [SecurityStamp], [SiteId], [TwoFactorEnabled], [UpdatedAt], [UserName])
    VALUES (N''b0000001-0000-4000-8000-000000000001'', 0, N'''', N''seed-nmvadmin-concurrency-stamp'', ''2026-01-01T00:00:00.0000000Z'', NULL, N''nmvadmin@demo.local'', CAST(1 AS bit), N''ADMIN001'', N''System Administrator'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), NULL, N''NMVADMIN@DEMO.LOCAL'', N''NMVADMIN'', N''AQAAAAIAAYagAAAAEMJ5vVKY+0P4eZWbaTWPo4Gpa9IUh/V49rbn+pkRWNt8+mmjnn3U6Mhr/brblygijg=='', NULL, CAST(0 AS bit), N'''', 1, N''seed-nmvadmin-security-stamp'', 1, CAST(0 AS bit), NULL, N''nmvadmin''),
    (N''b0000001-0000-4000-8000-000000000021'', 0, N'''', N''seed-concurrency-employee1'', ''2026-01-01T00:00:00.0000000Z'', NULL, N''employee1@demo.local'', CAST(1 AS bit), N''EMP001'', N''Nguyễn Văn Nhân Viên'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), NULL, N''EMPLOYEE1@DEMO.LOCAL'', N''EMPLOYEE1'', N''AQAAAAIAAYagAAAAEMJ5vVKY+0P4eZWbaTWPo4Gpa9IUh/V49rbn+pkRWNt8+mmjnn3U6Mhr/brblygijg=='', NULL, CAST(0 AS bit), N''Demo'', 1, N''seed-security-employee1'', 1, CAST(0 AS bit), NULL, N''employee1''),
    (N''b0000001-0000-4000-8000-000000000022'', 0, N'''', N''seed-concurrency-manager1'', ''2026-01-01T00:00:00.0000000Z'', NULL, N''manager1@demo.local'', CAST(1 AS bit), N''MGR001'', N''Trần Thị Trưởng Phòng'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), NULL, N''MANAGER1@DEMO.LOCAL'', N''MANAGER1'', N''AQAAAAIAAYagAAAAEMJ5vVKY+0P4eZWbaTWPo4Gpa9IUh/V49rbn+pkRWNt8+mmjnn3U6Mhr/brblygijg=='', NULL, CAST(0 AS bit), N''Demo'', 1, N''seed-security-manager1'', 1, CAST(0 AS bit), NULL, N''manager1''),
    (N''b0000001-0000-4000-8000-000000000023'', 0, N'''', N''seed-concurrency-director1'', ''2026-01-01T00:00:00.0000000Z'', NULL, N''director1@demo.local'', CAST(1 AS bit), N''DIR001'', N''Lê Văn Giám Đốc'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), NULL, N''DIRECTOR1@DEMO.LOCAL'', N''DIRECTOR1'', N''AQAAAAIAAYagAAAAEMJ5vVKY+0P4eZWbaTWPo4Gpa9IUh/V49rbn+pkRWNt8+mmjnn3U6Mhr/brblygijg=='', NULL, CAST(0 AS bit), N''Demo'', 3, N''seed-security-director1'', 1, CAST(0 AS bit), NULL, N''director1'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'Avatar', N'ConcurrencyStamp', N'CreatedAt', N'DateOfBirth', N'Email', N'EmailConfirmed', N'EmpId', N'FullName', N'HireDate', N'IsActive', N'IsOnline', N'IsOperator', N'LockoutEnabled', N'LockoutEnd', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'Position', N'SectionId', N'SecurityStamp', N'SiteId', N'TwoFactorEnabled', N'UpdatedAt', N'UserName') AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
        SET IDENTITY_INSERT [AspNetUsers] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ClaimType', N'ClaimValue', N'UserId') AND [object_id] = OBJECT_ID(N'[AspNetUserClaims]'))
        SET IDENTITY_INSERT [AspNetUserClaims] ON;
    EXEC(N'INSERT INTO [AspNetUserClaims] ([Id], [ClaimType], [ClaimValue], [UserId])
    VALUES (-4, N''IsFirstTimeLogin'', N''false'', N''b0000001-0000-4000-8000-000000000023''),
    (-3, N''IsFirstTimeLogin'', N''false'', N''b0000001-0000-4000-8000-000000000022''),
    (-2, N''IsFirstTimeLogin'', N''false'', N''b0000001-0000-4000-8000-000000000021''),
    (-1, N''IsFirstTimeLogin'', N''false'', N''b0000001-0000-4000-8000-000000000001'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ClaimType', N'ClaimValue', N'UserId') AND [object_id] = OBJECT_ID(N'[AspNetUserClaims]'))
        SET IDENTITY_INSERT [AspNetUserClaims] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'UserId') AND [object_id] = OBJECT_ID(N'[AspNetUserRoles]'))
        SET IDENTITY_INSERT [AspNetUserRoles] ON;
    EXEC(N'INSERT INTO [AspNetUserRoles] ([RoleId], [UserId])
    VALUES (N''a0000001-0000-4000-8000-000000000001'', N''b0000001-0000-4000-8000-000000000001''),
    (N''a0000001-0000-4000-8000-000000000021'', N''b0000001-0000-4000-8000-000000000021''),
    (N''a0000001-0000-4000-8000-000000000021'', N''b0000001-0000-4000-8000-000000000022''),
    (N''a0000001-0000-4000-8000-000000000022'', N''b0000001-0000-4000-8000-000000000022''),
    (N''a0000001-0000-4000-8000-000000000022'', N''b0000001-0000-4000-8000-000000000023'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'UserId') AND [object_id] = OBJECT_ID(N'[AspNetUserRoles]'))
        SET IDENTITY_INSERT [AspNetUserRoles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalDetails_ApprovalStepId] ON [ApprovalDetails] ([ApprovalStepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalDetails_BypassFromStepId] ON [ApprovalDetails] ([BypassFromStepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalDetails_BypassToStepId] ON [ApprovalDetails] ([BypassToStepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalDetails_NextStepId] ON [ApprovalDetails] ([NextStepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalDetails_RequestId] ON [ApprovalDetails] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalDetails_ReturnToStepId] ON [ApprovalDetails] ([ReturnToStepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalHistories_ApprovalDetailId] ON [ApprovalHistories] ([ApprovalDetailId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalHistories_RequestId_ActionAt] ON [ApprovalHistories] ([RequestId], [ActionAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ApprovalRequests_ApplicationNo] ON [ApprovalRequests] ([ApplicationNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalRequests_ApprovalCaseId] ON [ApprovalRequests] ([ApprovalCaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalRequests_CreatedByUserId_Status] ON [ApprovalRequests] ([CreatedByUserId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalRequests_DepartmentId_Status] ON [ApprovalRequests] ([DepartmentId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalRequests_RequestTypeId] ON [ApprovalRequests] ([RequestTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalSteps_ApprovalCaseId] ON [ApprovalSteps] ([ApprovalCaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalSteps_ApprovalRoleId] ON [ApprovalSteps] ([ApprovalRoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalSteps_BypassToStepId] ON [ApprovalSteps] ([BypassToStepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalSteps_NextStepId] ON [ApprovalSteps] ([NextStepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApprovalSteps_ReturnStepId] ON [ApprovalSteps] ([ReturnStepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_ApproverInfos_ApprovalDetailId] ON [ApproverInfos] ([ApprovalDetailId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AspNetUsers_EmpId] ON [AspNetUsers] ([EmpId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_SectionId] ON [AspNetUsers] ([SectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_SiteId] ON [AspNetUsers] ([SiteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BelongSites_Name] ON [BelongSites] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_SiteId_Name] ON [Departments] ([SiteId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_NotiToasts_CreatedAt] ON [NotiToasts] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_NotiToasts_RequestId] ON [NotiToasts] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_NotiToasts_SendFor_IsRead] ON [NotiToasts] ([SendFor], [IsRead]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_RequestAttachments_RequestId] ON [RequestAttachments] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_RequestTypes_ApprovalCaseId] ON [RequestTypes] ([ApprovalCaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RequestTypes_Code] ON [RequestTypes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sections_DepartmentId_Name] ON [Sections] ([DepartmentId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Sections_SiteId] ON [Sections] ([SiteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003041050_SqlServerBaseline'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003041050_SqlServerBaseline', N'10.0.5');
END;

COMMIT;
GO

