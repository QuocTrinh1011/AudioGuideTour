using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioGuideAdmin.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Pois]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Pois](
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(max) NOT NULL,
                        [Latitude] float NOT NULL,
                        [Longitude] float NOT NULL,
                        [Radius] int NOT NULL,
                        [ImageUrl] nvarchar(max) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_Pois] PRIMARY KEY ([Id])
                    );
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[UserTrackings]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [UserTrackings](
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(max) NOT NULL,
                        [Latitude] float NOT NULL,
                        [Longitude] float NOT NULL,
                        [Accuracy] float NOT NULL,
                        [RecordedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_UserTrackings] PRIMARY KEY ([Id])
                    );
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[VisitHistories]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [VisitHistories](
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(max) NOT NULL,
                        [PoiId] int NOT NULL,
                        [StartTime] datetime2 NOT NULL,
                        [EndTime] datetime2 NOT NULL,
                        [Duration] int NOT NULL,
                        CONSTRAINT [PK_VisitHistories] PRIMARY KEY ([Id])
                    );
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[PoiTranslations]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [PoiTranslations](
                        [Id] int NOT NULL IDENTITY,
                        [PoiId] int NOT NULL,
                        [Language] nvarchar(max) NOT NULL,
                        [Title] nvarchar(max) NOT NULL,
                        [Description] nvarchar(max) NOT NULL,
                        [AudioUrl] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_PoiTranslations] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PoiTranslations_Pois_PoiId] FOREIGN KEY ([PoiId]) REFERENCES [Pois] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_PoiTranslations_PoiId] ON [PoiTranslations] ([PoiId]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'[PoiTranslations]', N'U') IS NOT NULL DROP TABLE [PoiTranslations];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[UserTrackings]', N'U') IS NOT NULL DROP TABLE [UserTrackings];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[VisitHistories]', N'U') IS NOT NULL DROP TABLE [VisitHistories];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[Pois]', N'U') IS NOT NULL DROP TABLE [Pois];");
        }
    }
}
