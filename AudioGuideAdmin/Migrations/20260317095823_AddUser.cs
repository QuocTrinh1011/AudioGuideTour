using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioGuideAdmin.Migrations
{
    public partial class AddUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Users]', N'U') IS NULL AND OBJECT_ID(N'[AdminUsers]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Users](
                        [Id] int NOT NULL IDENTITY,
                        [Username] nvarchar(max) NOT NULL,
                        [Password] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                    );
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'[Users]', N'U') IS NOT NULL DROP TABLE [Users];");
        }
    }
}
