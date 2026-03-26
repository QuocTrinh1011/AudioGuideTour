using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioGuideAdmin.Migrations
{
    public partial class SplitAdminUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Users]', N'U') IS NOT NULL AND OBJECT_ID(N'[AdminUsers]', N'U') IS NULL
                BEGIN
                    EXEC sp_rename N'[Users]', N'AdminUsers';
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[AdminUsers]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [AdminUsers](
                        [Id] int NOT NULL IDENTITY,
                        [Username] nvarchar(450) NOT NULL,
                        [Password] nvarchar(max) NOT NULL,
                        [DisplayName] nvarchar(max) NOT NULL CONSTRAINT [DF_AdminUsers_DisplayName] DEFAULT N'',
                        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_AdminUsers_CreatedAt] DEFAULT '0001-01-01T00:00:00.0000000',
                        CONSTRAINT [PK_AdminUsers] PRIMARY KEY ([Id])
                    );
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[AdminUsers]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('AdminUsers', 'DisplayName') IS NULL
                        ALTER TABLE [AdminUsers] ADD [DisplayName] nvarchar(max) NOT NULL CONSTRAINT [DF_AdminUsers_DisplayName_Mig] DEFAULT N'';

                    IF COL_LENGTH('AdminUsers', 'CreatedAt') IS NULL
                        ALTER TABLE [AdminUsers] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_AdminUsers_CreatedAt_Mig] DEFAULT '0001-01-01T00:00:00.0000000';
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[AdminUsers]', N'U') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE name = 'IX_AdminUsers_Username'
                          AND object_id = OBJECT_ID(N'[AdminUsers]')
                    )
                BEGIN
                    ALTER TABLE [AdminUsers] ALTER COLUMN [Username] nvarchar(450) NOT NULL;
                    CREATE UNIQUE INDEX [IX_AdminUsers_Username] ON [AdminUsers] ([Username]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AdminUsers_Username' AND object_id = OBJECT_ID(N'[AdminUsers]')) DROP INDEX [IX_AdminUsers_Username] ON [AdminUsers];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[AdminUsers]', N'U') IS NOT NULL DROP TABLE [AdminUsers];");
        }
    }
}
