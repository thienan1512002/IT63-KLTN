-- Run on SQL Server with an account allowed to create databases.
-- Does not replace or delete an existing database.
IF DB_ID(N'AutomaticApprovalDb') IS NULL
    EXEC(N'CREATE DATABASE [AutomaticApprovalDb]');
GO
