-- Initial catalogue for a new installation. Existing merchant pricing takes precedence.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.TokenPackages WITH (UPDLOCK, HOLDLOCK))
    INSERT INTO dbo.TokenPackages (Tokens, PriceBdt, IsActive, CreatedAtUtc)
    VALUES (10, 500.00, 1, SYSUTCDATETIME()), (25, 1100.00, 1, SYSUTCDATETIME()), (60, 2400.00, 1, SYSUTCDATETIME());
COMMIT TRANSACTION;
