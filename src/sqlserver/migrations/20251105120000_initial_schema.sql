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
IF SCHEMA_ID(N'LottoTM') IS NULL EXEC(N'CREATE SCHEMA [LottoTM];');

CREATE TABLE [LottoTM].[Users] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [IsAdmin] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [LottoTM].[Draws] (
    [Id] int NOT NULL IDENTITY,
    [DrawSystemId] int NOT NULL,
    [DrawDate] date NOT NULL,
    [LottoType] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedByUserId] int NULL,
    [TicketPrice] numeric(18,2) NULL,
    [WinPoolCount1] int NULL,
    [WinPoolAmount1] numeric(18,2) NULL,
    [WinPoolCount2] int NULL,
    [WinPoolAmount2] numeric(18,2) NULL,
    [WinPoolCount3] int NULL,
    [WinPoolAmount3] numeric(18,2) NULL,
    [WinPoolCount4] int NULL,
    [WinPoolAmount4] numeric(18,2) NULL,
    CONSTRAINT [PK_Draws] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Draws_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [LottoTM].[Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [LottoTM].[Tickets] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [GroupName] nvarchar(100) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tickets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [LottoTM].[Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [LottoTM].[DrawNumbers] (
    [Id] int NOT NULL IDENTITY,
    [DrawId] int NOT NULL,
    [Number] int NOT NULL,
    [Position] tinyint NOT NULL,
    CONSTRAINT [PK_DrawNumbers] PRIMARY KEY ([Id]),
    CONSTRAINT [CHK_DrawNumbers_Number] CHECK ([Number] >= 1 AND [Number] <= 49),
    CONSTRAINT [CHK_DrawNumbers_Position] CHECK ([Position] >= 1 AND [Position] <= 6),
    CONSTRAINT [FK_DrawNumbers_Draws_DrawId] FOREIGN KEY ([DrawId]) REFERENCES [LottoTM].[Draws] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [LottoTM].[TicketNumbers] (
    [Id] int NOT NULL IDENTITY,
    [TicketId] int NOT NULL,
    [Number] int NOT NULL,
    [Position] tinyint NOT NULL,
    CONSTRAINT [PK_TicketNumbers] PRIMARY KEY ([Id]),
    CONSTRAINT [CHK_TicketNumbers_Number] CHECK ([Number] >= 1 AND [Number] <= 49),
    CONSTRAINT [CHK_TicketNumbers_Position] CHECK ([Position] >= 1 AND [Position] <= 6),
    CONSTRAINT [FK_TicketNumbers_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [LottoTM].[Tickets] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_DrawNumbers_DrawId] ON [LottoTM].[DrawNumbers] ([DrawId]);

CREATE UNIQUE INDEX [IX_DrawNumbers_DrawId_Position] ON [LottoTM].[DrawNumbers] ([DrawId], [Position]);

CREATE INDEX [IX_DrawNumbers_Number] ON [LottoTM].[DrawNumbers] ([Number]);

CREATE INDEX [IX_Draws_CreatedByUserId] ON [LottoTM].[Draws] ([CreatedByUserId]);

CREATE INDEX [IX_Draws_DrawDate] ON [LottoTM].[Draws] ([DrawDate]);

CREATE INDEX [IX_Draws_DrawSystemId] ON [LottoTM].[Draws] ([DrawSystemId]);

CREATE INDEX [IX_TicketNumbers_Number] ON [LottoTM].[TicketNumbers] ([Number]);

CREATE INDEX [IX_TicketNumbers_TicketId] ON [LottoTM].[TicketNumbers] ([TicketId]);

CREATE UNIQUE INDEX [IX_TicketNumbers_TicketId_Position] ON [LottoTM].[TicketNumbers] ([TicketId], [Position]);

CREATE INDEX [IX_Tickets_UserId] ON [LottoTM].[Tickets] ([UserId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [LottoTM].[Users] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251202211532_Init', N'9.0.10');

COMMIT;
GO