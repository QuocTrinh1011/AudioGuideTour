using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioGuideAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeAudioTourSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Visitors]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Visitors](
                        [Id] nvarchar(450) NOT NULL,
                        [DeviceId] nvarchar(450) NOT NULL,
                        [Language] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [AllowAutoPlay] bit NOT NULL CONSTRAINT [DF_Visitors_AllowAutoPlay] DEFAULT 0,
                        [AllowBackgroundTracking] bit NOT NULL CONSTRAINT [DF_Visitors_AllowBackgroundTracking] DEFAULT 0,
                        [DisplayName] nvarchar(max) NOT NULL CONSTRAINT [DF_Visitors_DisplayName] DEFAULT N'',
                        [LastSeenAt] datetime2 NOT NULL CONSTRAINT [DF_Visitors_LastSeenAt] DEFAULT '0001-01-01T00:00:00.0000000',
                        CONSTRAINT [PK_Visitors] PRIMARY KEY ([Id])
                    );
                END

                IF COL_LENGTH('VisitHistories', 'ActivationDistanceMeters') IS NULL
                    ALTER TABLE [VisitHistories] ADD [ActivationDistanceMeters] float NOT NULL CONSTRAINT [DF_VisitHistories_ActivationDistanceMeters] DEFAULT 0.0E0;
                IF COL_LENGTH('VisitHistories', 'Language') IS NULL
                    ALTER TABLE [VisitHistories] ADD [Language] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitHistories_Language] DEFAULT N'';
                IF COL_LENGTH('VisitHistories', 'PlaybackMode') IS NULL
                    ALTER TABLE [VisitHistories] ADD [PlaybackMode] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitHistories_PlaybackMode] DEFAULT N'';
                IF COL_LENGTH('VisitHistories', 'TriggerType') IS NULL
                    ALTER TABLE [VisitHistories] ADD [TriggerType] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitHistories_TriggerType] DEFAULT N'';
                IF COL_LENGTH('VisitHistories', 'WasAutoPlayed') IS NULL
                    ALTER TABLE [VisitHistories] ADD [WasAutoPlayed] bit NOT NULL CONSTRAINT [DF_VisitHistories_WasAutoPlayed] DEFAULT 0;
                IF COL_LENGTH('VisitHistories', 'WasCompleted') IS NULL
                    ALTER TABLE [VisitHistories] ADD [WasCompleted] bit NOT NULL CONSTRAINT [DF_VisitHistories_WasCompleted] DEFAULT 0;

                IF COL_LENGTH('UserTrackings', 'Bearing') IS NULL
                    ALTER TABLE [UserTrackings] ADD [Bearing] float NULL;
                IF COL_LENGTH('UserTrackings', 'IsForeground') IS NULL
                    ALTER TABLE [UserTrackings] ADD [IsForeground] bit NOT NULL CONSTRAINT [DF_UserTrackings_IsForeground] DEFAULT 0;
                IF COL_LENGTH('UserTrackings', 'Source') IS NULL
                    ALTER TABLE [UserTrackings] ADD [Source] nvarchar(max) NOT NULL CONSTRAINT [DF_UserTrackings_Source] DEFAULT N'';
                IF COL_LENGTH('UserTrackings', 'SpeedMetersPerSecond') IS NULL
                    ALTER TABLE [UserTrackings] ADD [SpeedMetersPerSecond] float NULL;

                IF COL_LENGTH('PoiTranslations', 'IsPublished') IS NULL
                    ALTER TABLE [PoiTranslations] ADD [IsPublished] bit NOT NULL CONSTRAINT [DF_PoiTranslations_IsPublished] DEFAULT 0;
                IF COL_LENGTH('PoiTranslations', 'Summary') IS NULL
                    ALTER TABLE [PoiTranslations] ADD [Summary] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_Summary] DEFAULT N'';
                IF COL_LENGTH('PoiTranslations', 'TtsScript') IS NULL
                    ALTER TABLE [PoiTranslations] ADD [TtsScript] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_TtsScript] DEFAULT N'';
                IF COL_LENGTH('PoiTranslations', 'UpdatedAt') IS NULL
                    ALTER TABLE [PoiTranslations] ADD [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_PoiTranslations_UpdatedAt] DEFAULT '0001-01-01T00:00:00.0000000';
                IF COL_LENGTH('PoiTranslations', 'VoiceName') IS NULL
                    ALTER TABLE [PoiTranslations] ADD [VoiceName] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_VoiceName] DEFAULT N'';

                IF COL_LENGTH('Pois', 'Address') IS NULL
                    ALTER TABLE [Pois] ADD [Address] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_Address] DEFAULT N'';
                IF COL_LENGTH('Pois', 'ApproachRadiusMeters') IS NULL
                    ALTER TABLE [Pois] ADD [ApproachRadiusMeters] int NOT NULL CONSTRAINT [DF_Pois_ApproachRadiusMeters] DEFAULT 0;
                IF COL_LENGTH('Pois', 'AudioMode') IS NULL
                    ALTER TABLE [Pois] ADD [AudioMode] nvarchar(40) NOT NULL CONSTRAINT [DF_Pois_AudioMode] DEFAULT N'';
                IF COL_LENGTH('Pois', 'AudioUrl') IS NULL
                    ALTER TABLE [Pois] ADD [AudioUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_AudioUrl] DEFAULT N'';
                IF COL_LENGTH('Pois', 'Category') IS NULL
                    ALTER TABLE [Pois] ADD [Category] nvarchar(100) NOT NULL CONSTRAINT [DF_Pois_Category] DEFAULT N'';
                IF COL_LENGTH('Pois', 'CooldownSeconds') IS NULL
                    ALTER TABLE [Pois] ADD [CooldownSeconds] int NOT NULL CONSTRAINT [DF_Pois_CooldownSeconds] DEFAULT 0;
                IF COL_LENGTH('Pois', 'DebounceSeconds') IS NULL
                    ALTER TABLE [Pois] ADD [DebounceSeconds] int NOT NULL CONSTRAINT [DF_Pois_DebounceSeconds] DEFAULT 0;
                IF COL_LENGTH('Pois', 'DefaultLanguage') IS NULL
                    ALTER TABLE [Pois] ADD [DefaultLanguage] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_DefaultLanguage] DEFAULT N'';
                IF COL_LENGTH('Pois', 'Description') IS NULL
                    ALTER TABLE [Pois] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_Description] DEFAULT N'';
                IF COL_LENGTH('Pois', 'EstimatedDurationSeconds') IS NULL
                    ALTER TABLE [Pois] ADD [EstimatedDurationSeconds] int NOT NULL CONSTRAINT [DF_Pois_EstimatedDurationSeconds] DEFAULT 0;
                IF COL_LENGTH('Pois', 'MapUrl') IS NULL
                    ALTER TABLE [Pois] ADD [MapUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_MapUrl] DEFAULT N'';
                IF COL_LENGTH('Pois', 'Priority') IS NULL
                    ALTER TABLE [Pois] ADD [Priority] int NOT NULL CONSTRAINT [DF_Pois_Priority] DEFAULT 0;
                IF COL_LENGTH('Pois', 'Summary') IS NULL
                    ALTER TABLE [Pois] ADD [Summary] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_Summary] DEFAULT N'';
                IF COL_LENGTH('Pois', 'TriggerMode') IS NULL
                    ALTER TABLE [Pois] ADD [TriggerMode] nvarchar(40) NOT NULL CONSTRAINT [DF_Pois_TriggerMode] DEFAULT N'';
                IF COL_LENGTH('Pois', 'TtsScript') IS NULL
                    ALTER TABLE [Pois] ADD [TtsScript] nvarchar(max) NOT NULL CONSTRAINT [DF_Pois_TtsScript] DEFAULT N'';
                IF COL_LENGTH('Pois', 'UpdatedAt') IS NULL
                    ALTER TABLE [Pois] ADD [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_Pois_UpdatedAt] DEFAULT '0001-01-01T00:00:00.0000000';

                IF COL_LENGTH('Visitors', 'AllowAutoPlay') IS NULL
                    ALTER TABLE [Visitors] ADD [AllowAutoPlay] bit NOT NULL CONSTRAINT [DF_Visitors_AllowAutoPlay_Mig] DEFAULT 0;
                IF COL_LENGTH('Visitors', 'AllowBackgroundTracking') IS NULL
                    ALTER TABLE [Visitors] ADD [AllowBackgroundTracking] bit NOT NULL CONSTRAINT [DF_Visitors_AllowBackgroundTracking_Mig] DEFAULT 0;
                IF COL_LENGTH('Visitors', 'DisplayName') IS NULL
                    ALTER TABLE [Visitors] ADD [DisplayName] nvarchar(max) NOT NULL CONSTRAINT [DF_Visitors_DisplayName_Mig] DEFAULT N'';
                IF COL_LENGTH('Visitors', 'LastSeenAt') IS NULL
                    ALTER TABLE [Visitors] ADD [LastSeenAt] datetime2 NOT NULL CONSTRAINT [DF_Visitors_LastSeenAt_Mig] DEFAULT '0001-01-01T00:00:00.0000000';

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

                IF OBJECT_ID(N'[QRCodes]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [QRCodes](
                        [Id] int NOT NULL IDENTITY,
                        [PoiId] int NOT NULL,
                        [Code] nvarchar(450) NOT NULL,
                        [Note] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_QRCodes] PRIMARY KEY ([Id])
                    );
                END

                IF OBJECT_ID(N'[Tours]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Tours](
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(200) NOT NULL,
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

                IF OBJECT_ID(N'[TourStops]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [TourStops](
                        [Id] int NOT NULL IDENTITY,
                        [TourId] int NOT NULL,
                        [PoiId] int NOT NULL,
                        [SortOrder] int NOT NULL,
                        [AutoPlay] bit NOT NULL,
                        [Note] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_TourStops] PRIMARY KEY ([Id])
                    );
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_PoiTranslations_PoiId_Language'
                      AND object_id = OBJECT_ID(N'[PoiTranslations]')
                )
                    CREATE UNIQUE INDEX [IX_PoiTranslations_PoiId_Language] ON [PoiTranslations] ([PoiId], [Language]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_QRCodes_Code'
                      AND object_id = OBJECT_ID(N'[QRCodes]')
                )
                    CREATE UNIQUE INDEX [IX_QRCodes_Code] ON [QRCodes] ([Code]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_QRCodes_PoiId'
                      AND object_id = OBJECT_ID(N'[QRCodes]')
                )
                    CREATE INDEX [IX_QRCodes_PoiId] ON [QRCodes] ([PoiId]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_TourStops_PoiId'
                      AND object_id = OBJECT_ID(N'[TourStops]')
                )
                    CREATE INDEX [IX_TourStops_PoiId] ON [TourStops] ([PoiId]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_TourStops_TourId_SortOrder'
                      AND object_id = OBJECT_ID(N'[TourStops]')
                )
                    CREATE UNIQUE INDEX [IX_TourStops_TourId_SortOrder] ON [TourStops] ([TourId], [SortOrder]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PoiTranslations_Pois_PoiId'
                )
                    ALTER TABLE [PoiTranslations] ADD CONSTRAINT [FK_PoiTranslations_Pois_PoiId] FOREIGN KEY ([PoiId]) REFERENCES [Pois]([Id]) ON DELETE CASCADE;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_QRCodes_Pois_PoiId'
                )
                    ALTER TABLE [QRCodes] ADD CONSTRAINT [FK_QRCodes_Pois_PoiId] FOREIGN KEY ([PoiId]) REFERENCES [Pois]([Id]) ON DELETE CASCADE;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TourStops_Pois_PoiId'
                )
                    ALTER TABLE [TourStops] ADD CONSTRAINT [FK_TourStops_Pois_PoiId] FOREIGN KEY ([PoiId]) REFERENCES [Pois]([Id]) ON DELETE CASCADE;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TourStops_Tours_TourId'
                )
                    ALTER TABLE [TourStops] ADD CONSTRAINT [FK_TourStops_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [Tours]([Id]) ON DELETE CASCADE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PoiTranslations_Pois_PoiId",
                table: "PoiTranslations");

            migrationBuilder.DropTable(
                name: "GeofenceTriggers");

            migrationBuilder.DropTable(
                name: "QRCodes");

            migrationBuilder.DropTable(
                name: "TourStops");

            migrationBuilder.DropTable(
                name: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_PoiTranslations_PoiId_Language",
                table: "PoiTranslations");

            migrationBuilder.DropColumn(
                name: "ActivationDistanceMeters",
                table: "VisitHistories");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "VisitHistories");

            migrationBuilder.DropColumn(
                name: "PlaybackMode",
                table: "VisitHistories");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "VisitHistories");

            migrationBuilder.DropColumn(
                name: "WasAutoPlayed",
                table: "VisitHistories");

            migrationBuilder.DropColumn(
                name: "WasCompleted",
                table: "VisitHistories");

            migrationBuilder.DropColumn(
                name: "Bearing",
                table: "UserTrackings");

            migrationBuilder.DropColumn(
                name: "IsForeground",
                table: "UserTrackings");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "UserTrackings");

            migrationBuilder.DropColumn(
                name: "SpeedMetersPerSecond",
                table: "UserTrackings");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "PoiTranslations");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "PoiTranslations");

            migrationBuilder.DropColumn(
                name: "TtsScript",
                table: "PoiTranslations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PoiTranslations");

            migrationBuilder.DropColumn(
                name: "VoiceName",
                table: "PoiTranslations");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "ApproachRadiusMeters",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "AudioMode",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "CooldownSeconds",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "DebounceSeconds",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "DefaultLanguage",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationSeconds",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "MapUrl",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "TriggerMode",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "TtsScript",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "AllowAutoPlay",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "AllowBackgroundTracking",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Visitors");

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "PoiTranslations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Pois",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
