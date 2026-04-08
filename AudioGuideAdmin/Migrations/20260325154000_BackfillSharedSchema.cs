using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioGuideAdmin.Migrations
{
    public partial class BackfillSharedSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Pois]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Pois', 'Category') IS NULL
                        ALTER TABLE [Pois] ADD [Category] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_Category_Backfill] DEFAULT N'food-street';
                    IF COL_LENGTH('Pois', 'Summary') IS NULL
                        ALTER TABLE [Pois] ADD [Summary] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_Summary_Backfill] DEFAULT N'';
                    IF COL_LENGTH('Pois', 'Description') IS NULL
                        ALTER TABLE [Pois] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_Description_Backfill] DEFAULT N'';
                    IF COL_LENGTH('Pois', 'Address') IS NULL
                        ALTER TABLE [Pois] ADD [Address] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_Address_Backfill] DEFAULT N'';
                    IF COL_LENGTH('Pois', 'ApproachRadiusMeters') IS NULL
                        ALTER TABLE [Pois] ADD [ApproachRadiusMeters] int NOT NULL CONSTRAINT [DF_Pois_ApproachRadius_Backfill] DEFAULT 90;
                    IF COL_LENGTH('Pois', 'Priority') IS NULL
                        ALTER TABLE [Pois] ADD [Priority] int NOT NULL CONSTRAINT [DF_Pois_Priority_Backfill] DEFAULT 1;
                    IF COL_LENGTH('Pois', 'DebounceSeconds') IS NULL
                        ALTER TABLE [Pois] ADD [DebounceSeconds] int NOT NULL CONSTRAINT [DF_Pois_Debounce_Backfill] DEFAULT 15;
                    IF COL_LENGTH('Pois', 'CooldownSeconds') IS NULL
                        ALTER TABLE [Pois] ADD [CooldownSeconds] int NOT NULL CONSTRAINT [DF_Pois_Cooldown_Backfill] DEFAULT 120;
                    IF COL_LENGTH('Pois', 'TriggerMode') IS NULL
                        ALTER TABLE [Pois] ADD [TriggerMode] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_TriggerMode_Backfill] DEFAULT N'both';
                    IF COL_LENGTH('Pois', 'MapUrl') IS NULL
                        ALTER TABLE [Pois] ADD [MapUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_MapUrl_Backfill] DEFAULT N'';
                    IF COL_LENGTH('Pois', 'AudioMode') IS NULL
                        ALTER TABLE [Pois] ADD [AudioMode] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_AudioMode_Backfill] DEFAULT N'hybrid';
                    IF COL_LENGTH('Pois', 'AudioUrl') IS NULL
                        ALTER TABLE [Pois] ADD [AudioUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_AudioUrl_Backfill] DEFAULT N'';
                    IF COL_LENGTH('Pois', 'TtsScript') IS NULL
                        ALTER TABLE [Pois] ADD [TtsScript] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_TtsScript_Backfill] DEFAULT N'';
                    IF COL_LENGTH('Pois', 'DefaultLanguage') IS NULL
                        ALTER TABLE [Pois] ADD [DefaultLanguage] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_DefaultLanguage_Backfill] DEFAULT N'vi-VN';
                    IF COL_LENGTH('Pois', 'EstimatedDurationSeconds') IS NULL
                        ALTER TABLE [Pois] ADD [EstimatedDurationSeconds] int NOT NULL CONSTRAINT [DF_Pois_EstimatedDuration_Backfill] DEFAULT 60;
                    IF COL_LENGTH('Pois', 'UpdatedAt') IS NULL
                        ALTER TABLE [Pois] ADD [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_Pois_UpdatedAt_Backfill] DEFAULT '0001-01-01T00:00:00.0000000';
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[PoiTranslations]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('PoiTranslations', 'Summary') IS NULL
                        ALTER TABLE [PoiTranslations] ADD [Summary] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_Summary_Backfill] DEFAULT N'';
                    IF COL_LENGTH('PoiTranslations', 'AudioUrl') IS NULL
                        ALTER TABLE [PoiTranslations] ADD [AudioUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_AudioUrl_Backfill] DEFAULT N'';
                    IF COL_LENGTH('PoiTranslations', 'TtsScript') IS NULL
                        ALTER TABLE [PoiTranslations] ADD [TtsScript] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_TtsScript_Backfill] DEFAULT N'';
                    IF COL_LENGTH('PoiTranslations', 'VoiceName') IS NULL
                        ALTER TABLE [PoiTranslations] ADD [VoiceName] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_VoiceName_Backfill] DEFAULT N'';
                    IF COL_LENGTH('PoiTranslations', 'IsPublished') IS NULL
                        ALTER TABLE [PoiTranslations] ADD [IsPublished] bit NOT NULL CONSTRAINT [DF_PoiTranslations_IsPublished_Backfill] DEFAULT 1;
                    IF COL_LENGTH('PoiTranslations', 'UpdatedAt') IS NULL
                        ALTER TABLE [PoiTranslations] ADD [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_PoiTranslations_UpdatedAt_Backfill] DEFAULT '0001-01-01T00:00:00.0000000';
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[UserTrackings]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('UserTrackings', 'SpeedMetersPerSecond') IS NULL
                        ALTER TABLE [UserTrackings] ADD [SpeedMetersPerSecond] float NULL;
                    IF COL_LENGTH('UserTrackings', 'Bearing') IS NULL
                        ALTER TABLE [UserTrackings] ADD [Bearing] float NULL;
                    IF COL_LENGTH('UserTrackings', 'Source') IS NULL
                        ALTER TABLE [UserTrackings] ADD [Source] nvarchar(max) NOT NULL CONSTRAINT [DF_UserTrackings_Source_Backfill] DEFAULT N'gps';
                    IF COL_LENGTH('UserTrackings', 'IsForeground') IS NULL
                        ALTER TABLE [UserTrackings] ADD [IsForeground] bit NOT NULL CONSTRAINT [DF_UserTrackings_IsForeground_Backfill] DEFAULT 1;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[VisitHistories]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('VisitHistories', 'Language') IS NULL
                        ALTER TABLE [VisitHistories] ADD [Language] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitHistories_Language_Backfill] DEFAULT N'vi-VN';
                    IF COL_LENGTH('VisitHistories', 'TriggerType') IS NULL
                        ALTER TABLE [VisitHistories] ADD [TriggerType] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitHistories_TriggerType_Backfill] DEFAULT N'manual';
                    IF COL_LENGTH('VisitHistories', 'PlaybackMode') IS NULL
                        ALTER TABLE [VisitHistories] ADD [PlaybackMode] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitHistories_PlaybackMode_Backfill] DEFAULT N'tts';
                    IF COL_LENGTH('VisitHistories', 'WasAutoPlayed') IS NULL
                        ALTER TABLE [VisitHistories] ADD [WasAutoPlayed] bit NOT NULL CONSTRAINT [DF_VisitHistories_WasAutoPlayed_Backfill] DEFAULT 1;
                    IF COL_LENGTH('VisitHistories', 'WasCompleted') IS NULL
                        ALTER TABLE [VisitHistories] ADD [WasCompleted] bit NOT NULL CONSTRAINT [DF_VisitHistories_WasCompleted_Backfill] DEFAULT 1;
                    IF COL_LENGTH('VisitHistories', 'ActivationDistanceMeters') IS NULL
                        ALTER TABLE [VisitHistories] ADD [ActivationDistanceMeters] float NOT NULL CONSTRAINT [DF_VisitHistories_ActivationDistance_Backfill] DEFAULT 0;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[GeofenceTriggers]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [GeofenceTriggers](
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(max) NOT NULL,
                        [PoiId] int NOT NULL,
                        [Language] nvarchar(max) NOT NULL,
                        [TriggerType] nvarchar(max) NOT NULL,
                        [Status] nvarchar(max) NOT NULL,
                        [DistanceMeters] float NOT NULL,
                        [RecordedAt] datetime2 NOT NULL,
                        [CooldownUntil] datetime2 NOT NULL,
                        CONSTRAINT [PK_GeofenceTriggers] PRIMARY KEY ([Id])
                    );
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Tours]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Tours](
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(max) NOT NULL,
                        [Description] nvarchar(max) NOT NULL,
                        [Language] nvarchar(max) NOT NULL,
                        [CoverImageUrl] nvarchar(max) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [EstimatedDurationMinutes] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_Tours] PRIMARY KEY ([Id])
                    );
                END
                ELSE
                BEGIN
                    IF COL_LENGTH('Tours', 'Language') IS NULL
                        ALTER TABLE [Tours] ADD [Language] nvarchar(max) NOT NULL CONSTRAINT [DF_Tours_Language_Backfill] DEFAULT N'vi-VN';
                    IF COL_LENGTH('Tours', 'CoverImageUrl') IS NULL
                        ALTER TABLE [Tours] ADD [CoverImageUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Tours_CoverImageUrl_Backfill] DEFAULT N'';
                    IF COL_LENGTH('Tours', 'IsActive') IS NULL
                        ALTER TABLE [Tours] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Tours_IsActive_Backfill] DEFAULT 1;
                    IF COL_LENGTH('Tours', 'EstimatedDurationMinutes') IS NULL
                        ALTER TABLE [Tours] ADD [EstimatedDurationMinutes] int NOT NULL CONSTRAINT [DF_Tours_EstimatedDuration_Backfill] DEFAULT 45;
                    IF COL_LENGTH('Tours', 'CreatedAt') IS NULL
                        ALTER TABLE [Tours] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Tours_CreatedAt_Backfill] DEFAULT '0001-01-01T00:00:00.0000000';
                    IF COL_LENGTH('Tours', 'UpdatedAt') IS NULL
                        ALTER TABLE [Tours] ADD [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_Tours_UpdatedAt_Backfill] DEFAULT '0001-01-01T00:00:00.0000000';
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[TourStops]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [TourStops](
                        [Id] int NOT NULL IDENTITY,
                        [TourId] int NOT NULL,
                        [PoiId] int NOT NULL,
                        [SortOrder] int NOT NULL,
                        [AutoPlay] bit NOT NULL,
                        [Note] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_TourStops] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_TourStops_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [Tours]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_TourStops_Pois_PoiId] FOREIGN KEY ([PoiId]) REFERENCES [Pois]([Id]) ON DELETE CASCADE
                    );
                END
                ELSE
                BEGIN
                    IF COL_LENGTH('TourStops', 'AutoPlay') IS NULL
                        ALTER TABLE [TourStops] ADD [AutoPlay] bit NOT NULL CONSTRAINT [DF_TourStops_AutoPlay_Backfill] DEFAULT 1;
                    IF COL_LENGTH('TourStops', 'Note') IS NULL
                        ALTER TABLE [TourStops] ADD [Note] nvarchar(max) NOT NULL CONSTRAINT [DF_TourStops_Note_Backfill] DEFAULT N'';
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[PoiTranslations]', N'U') IS NOT NULL
                    AND COL_LENGTH('PoiTranslations', 'Language') IS NOT NULL
                    AND EXISTS (
                        SELECT 1
                        FROM sys.columns
                        WHERE object_id = OBJECT_ID(N'[PoiTranslations]')
                          AND name = 'Language'
                          AND max_length = -1
                    )
                BEGIN
                    ALTER TABLE [PoiTranslations] ALTER COLUMN [Language] nvarchar(450) NOT NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[PoiTranslations]', N'U') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE name = 'IX_PoiTranslations_PoiId_Language'
                          AND object_id = OBJECT_ID(N'[PoiTranslations]')
                    )
                BEGIN
                    CREATE UNIQUE INDEX [IX_PoiTranslations_PoiId_Language] ON [PoiTranslations]([PoiId], [Language]);
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[TourStops]', N'U') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE name = 'IX_TourStops_TourId_SortOrder'
                          AND object_id = OBJECT_ID(N'[TourStops]')
                    )
                BEGIN
                    CREATE UNIQUE INDEX [IX_TourStops_TourId_SortOrder] ON [TourStops]([TourId], [SortOrder]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
