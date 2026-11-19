BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119201847_ProtectVerificationEvidence'
)
BEGIN
    ALTER TABLE [VerificationDocuments] ADD [Retention] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119201847_ProtectVerificationEvidence'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261119201847_ProtectVerificationEvidence', N'10.0.14');
END;

COMMIT;
GO
