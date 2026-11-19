-- Notifications Table Migration
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Notifications (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL CONSTRAINT FK_Notifications_Users REFERENCES dbo.Users(Id) ON DELETE CASCADE,
        Title NVARCHAR(150) NOT NULL,
        Message NVARCHAR(500) NOT NULL,
        Type NVARCHAR(20) NOT NULL CONSTRAINT DF_Notifications_Type DEFAULT 'info',
        IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
        CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Notifications_CreatedUtc DEFAULT SYSUTCDATETIME(),
        ActionUrl NVARCHAR(500) NULL
    );

    CREATE NONCLUSTERED INDEX IX_Notifications_UserId_IsRead_CreatedUtc
    ON dbo.Notifications (UserId, IsRead, CreatedUtc DESC);
END;
GO
