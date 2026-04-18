USE EmployeePortal;
GO

IF COL_LENGTH('portal.AuditLogs', 'SourceLayer') IS NULL
BEGIN
  ALTER TABLE portal.AuditLogs ADD SourceLayer NVARCHAR(100) NULL;
END;
GO

IF COL_LENGTH('portal.AuditLogs', 'CorrelationId') IS NULL
BEGIN
  ALTER TABLE portal.AuditLogs ADD CorrelationId NVARCHAR(100) NULL;
END;
GO

IF COL_LENGTH('portal.AuditLogs', 'RequestPath') IS NULL
BEGIN
  ALTER TABLE portal.AuditLogs ADD RequestPath NVARCHAR(300) NULL;
END;
GO

IF COL_LENGTH('portal.AuditLogs', 'StatusCode') IS NULL
BEGIN
  ALTER TABLE portal.AuditLogs ADD StatusCode INT NULL;
END;
GO

IF COL_LENGTH('portal.ErrorLogs', 'UserName') IS NULL
BEGIN
  ALTER TABLE portal.ErrorLogs ADD UserName NVARCHAR(100) NULL;
END;
GO

IF COL_LENGTH('portal.ErrorLogs', 'RequestPath') IS NULL
BEGIN
  ALTER TABLE portal.ErrorLogs ADD RequestPath NVARCHAR(300) NULL;
END;
GO

IF COL_LENGTH('portal.ErrorLogs', 'IpAddress') IS NULL
BEGIN
  ALTER TABLE portal.ErrorLogs ADD IpAddress NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH('portal.ErrorLogs', 'UserAgent') IS NULL
BEGIN
  ALTER TABLE portal.ErrorLogs ADD UserAgent NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH('portal.ErrorLogs', 'StatusCode') IS NULL
BEGIN
  ALTER TABLE portal.ErrorLogs ADD StatusCode INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_CorrelationId' AND object_id = OBJECT_ID('portal.AuditLogs'))
BEGIN
  CREATE INDEX IX_AuditLogs_CorrelationId ON portal.AuditLogs(CorrelationId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ErrorLogs_CorrelationId' AND object_id = OBJECT_ID('portal.ErrorLogs'))
BEGIN
  CREATE INDEX IX_ErrorLogs_CorrelationId ON portal.ErrorLogs(CorrelationId);
END;
GO
