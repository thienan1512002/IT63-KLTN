BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    ALTER TABLE [RequestTypes] ADD [TemplateCode] nvarchar(30) NOT NULL DEFAULT N'INTERNAL';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE TABLE [RequestMasterItems] (
        [Id] int NOT NULL IDENTITY,
        [Category] nvarchar(40) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [ParentId] int NULL,
        [Unit] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [Capacity] int NULL,
        CONSTRAINT [PK_RequestMasterItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequestMasterItems_RequestMasterItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE TABLE [RequestBusinessData] (
        [RequestId] int NOT NULL,
        [TemplateCode] nvarchar(30) NOT NULL,
        [EmployeeId] nvarchar(450) NULL,
        [ReplacementEmployeeId] nvarchar(450) NULL,
        [TargetDepartmentId] int NULL,
        [StartAt] datetime2 NULL,
        [EndAt] datetime2 NULL,
        [Duration] decimal(10,2) NULL,
        [LeaveTypeId] int NULL,
        [StartSession] nvarchar(20) NULL,
        [EndSession] nvarchar(20) NULL,
        [PurposeId] int NULL,
        [CountryId] int NULL,
        [CityId] int NULL,
        [PartnerId] int NULL,
        [CurrencyId] int NULL,
        [ExchangeRate] decimal(18,6) NULL,
        [OriginalAmount] decimal(18,2) NULL,
        [Location] nvarchar(500) NULL,
        [OvertimeTypeId] int NULL,
        [PaymentMethodId] int NULL,
        [Payee] nvarchar(200) NULL,
        [BankAccount] nvarchar(100) NULL,
        [BankName] nvarchar(200) NULL,
        [InvoiceNumber] nvarchar(100) NULL,
        [InvoiceDate] datetime2 NULL,
        [ProposalCategoryId] int NULL,
        [ExpectedBenefit] nvarchar(2000) NULL,
        [VehicleTypeId] int NULL,
        [VehicleId] int NULL,
        [DriverEmployeeId] nvarchar(450) NULL,
        [PickupLocation] nvarchar(500) NULL,
        [Destination] nvarchar(500) NULL,
        [PassengerCount] int NULL,
        [ContactPhone] nvarchar(30) NULL,
        CONSTRAINT [PK_RequestBusinessData] PRIMARY KEY ([RequestId]),
        CONSTRAINT [FK_RequestBusinessData_ApprovalRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ApprovalRequests] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RequestBusinessData_AspNetUsers_DriverEmployeeId] FOREIGN KEY ([DriverEmployeeId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_AspNetUsers_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_AspNetUsers_ReplacementEmployeeId] FOREIGN KEY ([ReplacementEmployeeId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_Departments_TargetDepartmentId] FOREIGN KEY ([TargetDepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_CityId] FOREIGN KEY ([CityId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_OvertimeTypeId] FOREIGN KEY ([OvertimeTypeId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_ProposalCategoryId] FOREIGN KEY ([ProposalCategoryId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_PurposeId] FOREIGN KEY ([PurposeId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_VehicleTypeId] FOREIGN KEY ([VehicleTypeId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE TABLE [RequestLineItems] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [LineOrder] int NOT NULL,
        [CatalogItemId] int NULL,
        [Description] nvarchar(500) NULL,
        [Unit] nvarchar(50) NULL,
        [Quantity] decimal(12,2) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [LineTotal] decimal(18,2) NOT NULL,
        [Note] nvarchar(1000) NULL,
        CONSTRAINT [PK_RequestLineItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequestLineItems_RequestBusinessData_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [RequestBusinessData] ([RequestId]) ON DELETE CASCADE,
        CONSTRAINT [FK_RequestLineItems_RequestMasterItems_CatalogItemId] FOREIGN KEY ([CatalogItemId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE TABLE [RequestParticipants] (
        [RequestId] int NOT NULL,
        [EmployeeId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_RequestParticipants] PRIMARY KEY ([RequestId], [EmployeeId]),
        CONSTRAINT [FK_RequestParticipants_AspNetUsers_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestParticipants_RequestBusinessData_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [RequestBusinessData] ([RequestId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Capacity', N'Category', N'Code', N'IsActive', N'Name', N'ParentId', N'Unit') AND [object_id] = OBJECT_ID(N'[RequestMasterItems]'))
        SET IDENTITY_INSERT [RequestMasterItems] ON;
    EXEC(N'INSERT INTO [RequestMasterItems] ([Id], [Capacity], [Category], [Code], [IsActive], [Name], [ParentId], [Unit])
    VALUES (1, NULL, N''LeaveType'', N''ANNUAL'', CAST(1 AS bit), N''Nghỉ phép năm'', NULL, NULL),
    (2, NULL, N''LeaveType'', N''UNPAID'', CAST(1 AS bit), N''Nghỉ không hưởng lương'', NULL, NULL),
    (3, NULL, N''LeaveType'', N''SICK'', CAST(1 AS bit), N''Nghỉ ốm'', NULL, NULL),
    (4, NULL, N''LeaveType'', N''PERSONAL'', CAST(1 AS bit), N''Nghỉ việc riêng'', NULL, NULL),
    (10, NULL, N''TripPurpose'', N''CUSTOMER'', CAST(1 AS bit), N''Gặp gỡ khách hàng / Đối tác'', NULL, NULL),
    (11, NULL, N''TripPurpose'', N''CONFERENCE'', CAST(1 AS bit), N''Tham gia hội thảo / Triển lãm'', NULL, NULL),
    (12, NULL, N''TripPurpose'', N''TRAINING'', CAST(1 AS bit), N''Đào tạo / Tập huấn'', NULL, NULL),
    (13, NULL, N''TripPurpose'', N''SITE_VISIT'', CAST(1 AS bit), N''Khảo sát / Làm việc tại cơ sở'', NULL, NULL),
    (20, NULL, N''Country'', N''VN'', CAST(1 AS bit), N''Việt Nam'', NULL, NULL),
    (21, NULL, N''Country'', N''JP'', CAST(1 AS bit), N''Nhật Bản'', NULL, NULL),
    (22, NULL, N''Country'', N''SG'', CAST(1 AS bit), N''Singapore'', NULL, NULL),
    (40, NULL, N''Currency'', N''VND'', CAST(1 AS bit), N''Việt Nam đồng (VND)'', NULL, NULL),
    (41, NULL, N''Currency'', N''JPY'', CAST(1 AS bit), N''Yên Nhật (JPY)'', NULL, NULL),
    (42, NULL, N''Currency'', N''USD'', CAST(1 AS bit), N''Đô la Mỹ (USD)'', NULL, NULL),
    (43, NULL, N''Currency'', N''SGD'', CAST(1 AS bit), N''Đô la Singapore (SGD)'', NULL, NULL),
    (50, NULL, N''ExpenseCategory'', N''AIRFARE'', CAST(1 AS bit), N''Vé máy bay'', NULL, N''vé''),
    (51, NULL, N''ExpenseCategory'', N''HOTEL'', CAST(1 AS bit), N''Lưu trú / Khách sạn'', NULL, N''đêm''),
    (52, NULL, N''ExpenseCategory'', N''MEALS'', CAST(1 AS bit), N''Ăn uống / Công tác phí'', NULL, N''ngày''),
    (53, NULL, N''ExpenseCategory'', N''TRANSPORT'', CAST(1 AS bit), N''Di chuyển'', NULL, N''lượt''),
    (54, NULL, N''ExpenseCategory'', N''SERVICE'', CAST(1 AS bit), N''Dịch vụ'', NULL, N''lần''),
    (55, NULL, N''ExpenseCategory'', N''OTHER'', CAST(1 AS bit), N''Chi phí khác'', NULL, N''khoản''),
    (60, NULL, N''Product'', N''LAPTOP'', CAST(1 AS bit), N''Máy tính xách tay'', NULL, N''chiếc''),
    (61, NULL, N''Product'', N''STATIONERY'', CAST(1 AS bit), N''Văn phòng phẩm'', NULL, N''bộ''),
    (70, NULL, N''Equipment'', N''LAPTOP'', CAST(1 AS bit), N''Máy tính xách tay'', NULL, N''chiếc''),
    (71, NULL, N''Equipment'', N''MONITOR'', CAST(1 AS bit), N''Màn hình'', NULL, N''chiếc''),
    (72, NULL, N''Equipment'', N''KEYBOARD'', CAST(1 AS bit), N''Bàn phím'', NULL, N''chiếc''),
    (73, NULL, N''Equipment'', N''MOUSE'', CAST(1 AS bit), N''Chuột máy tính'', NULL, N''chiếc''),
    (80, NULL, N''OvertimeType'', N''WEEKDAY'', CAST(1 AS bit), N''Làm thêm ngày thường'', NULL, NULL),
    (81, NULL, N''OvertimeType'', N''WEEKEND'', CAST(1 AS bit), N''Làm thêm ngày nghỉ'', NULL, NULL),
    (82, NULL, N''OvertimeType'', N''HOLIDAY'', CAST(1 AS bit), N''Làm thêm ngày lễ'', NULL, NULL),
    (90, NULL, N''PaymentMethod'', N''TRANSFER'', CAST(1 AS bit), N''Chuyển khoản'', NULL, NULL),
    (91, NULL, N''PaymentMethod'', N''CASH'', CAST(1 AS bit), N''Tiền mặt'', NULL, NULL),
    (100, NULL, N''ProposalCategory'', N''PROCESS'', CAST(1 AS bit), N''Cải tiến quy trình'', NULL, NULL),
    (101, NULL, N''ProposalCategory'', N''WORKPLACE'', CAST(1 AS bit), N''Cải thiện môi trường làm việc'', NULL, NULL),
    (102, NULL, N''ProposalCategory'', N''TRAINING'', CAST(1 AS bit), N''Đào tạo nhân sự'', NULL, NULL),
    (103, NULL, N''ProposalCategory'', N''OTHER'', CAST(1 AS bit), N''Đề xuất khác'', NULL, NULL),
    (110, 3, N''VehicleType'', N''4_SEATS'', CAST(1 AS bit), N''Xe 4 chỗ'', NULL, NULL),
    (111, 6, N''VehicleType'', N''7_SEATS'', CAST(1 AS bit), N''Xe 7 chỗ'', NULL, NULL),
    (112, 15, N''VehicleType'', N''16_SEATS'', CAST(1 AS bit), N''Xe 16 chỗ'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Capacity', N'Category', N'Code', N'IsActive', N'Name', N'ParentId', N'Unit') AND [object_id] = OBJECT_ID(N'[RequestMasterItems]'))
        SET IDENTITY_INSERT [RequestMasterItems] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    EXEC(N'UPDATE [RequestTypes] SET [TemplateCode] = N''LEAVE''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    EXEC(N'UPDATE [RequestTypes] SET [TemplateCode] = N''PURCHASE''
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    EXEC(N'UPDATE [RequestTypes] SET [TemplateCode] = N''PAYMENT''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    EXEC(N'UPDATE [RequestTypes] SET [TemplateCode] = N''EQUIPMENT''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    EXEC(N'UPDATE [RequestTypes] SET [TemplateCode] = N''OVERTIME''
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    EXEC(N'UPDATE [RequestTypes] SET [TemplateCode] = N''INTERNAL''
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Capacity', N'Category', N'Code', N'IsActive', N'Name', N'ParentId', N'Unit') AND [object_id] = OBJECT_ID(N'[RequestMasterItems]'))
        SET IDENTITY_INSERT [RequestMasterItems] ON;
    EXEC(N'INSERT INTO [RequestMasterItems] ([Id], [Capacity], [Category], [Code], [IsActive], [Name], [ParentId], [Unit])
    VALUES (30, NULL, N''City'', N''VN-HN'', CAST(1 AS bit), N''Hà Nội'', 20, NULL),
    (31, NULL, N''City'', N''VN-HCM'', CAST(1 AS bit), N''Thành phố Hồ Chí Minh'', 20, NULL),
    (32, NULL, N''City'', N''VN-DN'', CAST(1 AS bit), N''Đà Nẵng'', 20, NULL),
    (33, NULL, N''City'', N''JP-TOKYO'', CAST(1 AS bit), N''Tokyo'', 21, NULL),
    (34, NULL, N''City'', N''JP-OSAKA'', CAST(1 AS bit), N''Osaka'', 21, NULL),
    (35, NULL, N''City'', N''SG-SINGAPORE'', CAST(1 AS bit), N''Singapore'', 22, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Capacity', N'Category', N'Code', N'IsActive', N'Name', N'ParentId', N'Unit') AND [object_id] = OBJECT_ID(N'[RequestMasterItems]'))
        SET IDENTITY_INSERT [RequestMasterItems] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    UPDATE RequestTypes SET TemplateCode = CASE
        WHEN UPPER(Code) IN ('LEAVE','PURCHASE','PAYMENT','EQUIPMENT','OVERTIME','INTERNAL') THEN UPPER(Code)
        WHEN UPPER(Code) IN ('BUSINESS_TRIP','TRAVEL','BUS') THEN 'BUSINESS_TRIP'
        WHEN UPPER(Code) IN ('VEHICLE','VEH') THEN 'VEHICLE'
        ELSE TemplateCode END;
    INSERT INTO RequestTypes (Code, TemplateCode, Name, Description, IsActive, RequireRequestedDate, RequireAmount, RequireReason, AllowAttachment, ApprovalCaseId, CreatedAt)
    SELECT 'BUSINESS_TRIP', 'BUSINESS_TRIP', N'Đăng ký công tác', N'Đăng ký lịch trình và dự toán chi phí công tác', 1, 1, 0, 1, 1, CaseId, SYSUTCDATETIME()
    FROM ApprovalCases WHERE CaseId = 'WF-MULTI' AND IsActive = 1
    AND NOT EXISTS (SELECT 1 FROM RequestTypes WHERE TemplateCode = 'BUSINESS_TRIP');
    INSERT INTO RequestTypes (Code, TemplateCode, Name, Description, IsActive, RequireRequestedDate, RequireAmount, RequireReason, AllowAttachment, ApprovalCaseId, CreatedAt)
    SELECT 'VEHICLE', 'VEHICLE', N'Đặt xe công ty', N'Đăng ký lịch trình và phương tiện công tác', 1, 1, 0, 1, 1, CaseId, SYSUTCDATETIME()
    FROM ApprovalCases WHERE CaseId = 'WF-ONE' AND IsActive = 1
    AND NOT EXISTS (SELECT 1 FROM RequestTypes WHERE TemplateCode = 'VEHICLE');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_CityId] ON [RequestBusinessData] ([CityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_CountryId] ON [RequestBusinessData] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_CurrencyId] ON [RequestBusinessData] ([CurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_DriverEmployeeId] ON [RequestBusinessData] ([DriverEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_EmployeeId] ON [RequestBusinessData] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_LeaveTypeId] ON [RequestBusinessData] ([LeaveTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_OvertimeTypeId] ON [RequestBusinessData] ([OvertimeTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_PartnerId] ON [RequestBusinessData] ([PartnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_PaymentMethodId] ON [RequestBusinessData] ([PaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_ProposalCategoryId] ON [RequestBusinessData] ([ProposalCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_PurposeId] ON [RequestBusinessData] ([PurposeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_ReplacementEmployeeId] ON [RequestBusinessData] ([ReplacementEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_TargetDepartmentId] ON [RequestBusinessData] ([TargetDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_TemplateCode_StartAt] ON [RequestBusinessData] ([TemplateCode], [StartAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_VehicleId] ON [RequestBusinessData] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_VehicleTypeId] ON [RequestBusinessData] ([VehicleTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestLineItems_CatalogItemId] ON [RequestLineItems] ([CatalogItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RequestLineItems_RequestId_LineOrder] ON [RequestLineItems] ([RequestId], [LineOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RequestMasterItems_Category_Code] ON [RequestMasterItems] ([Category], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestMasterItems_ParentId] ON [RequestMasterItems] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    CREATE INDEX [IX_RequestParticipants_EmployeeId] ON [RequestParticipants] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015135454_RequestBusinessForms'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015135454_RequestBusinessForms', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015141339_RequestBusinessBanksAndSites'
)
BEGIN
    ALTER TABLE [RequestBusinessData] ADD [BankId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015141339_RequestBusinessBanksAndSites'
)
BEGIN
    ALTER TABLE [RequestBusinessData] ADD [WorkSiteId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015141339_RequestBusinessBanksAndSites'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_BankId] ON [RequestBusinessData] ([BankId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015141339_RequestBusinessBanksAndSites'
)
BEGIN
    CREATE INDEX [IX_RequestBusinessData_WorkSiteId] ON [RequestBusinessData] ([WorkSiteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015141339_RequestBusinessBanksAndSites'
)
BEGIN
    ALTER TABLE [RequestBusinessData] ADD CONSTRAINT [FK_RequestBusinessData_BelongSites_WorkSiteId] FOREIGN KEY ([WorkSiteId]) REFERENCES [BelongSites] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015141339_RequestBusinessBanksAndSites'
)
BEGIN
    ALTER TABLE [RequestBusinessData] ADD CONSTRAINT [FK_RequestBusinessData_RequestMasterItems_BankId] FOREIGN KEY ([BankId]) REFERENCES [RequestMasterItems] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015141339_RequestBusinessBanksAndSites'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015141339_RequestBusinessBanksAndSites', N'10.0.5');
END;

COMMIT;
GO

