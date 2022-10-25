using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OIDC_ArtifactServer.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArtifactBranch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactBranch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtifactBranch_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArtifactEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntryName = table.Column<string>(type: "text", nullable: false),
                    TimeAdded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArtifactBranchId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtifactEntry_ArtifactBranch_ArtifactBranchId",
                        column: x => x.ArtifactBranchId,
                        principalTable: "ArtifactBranch",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ArtifactFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ArtifactEntryId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtifactFile_ArtifactEntry_ArtifactEntryId",
                        column: x => x.ArtifactEntryId,
                        principalTable: "ArtifactEntry",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactBranch_ProjectId",
                table: "ArtifactBranch",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactEntry_ArtifactBranchId",
                table: "ArtifactEntry",
                column: "ArtifactBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactFile_ArtifactEntryId",
                table: "ArtifactFile",
                column: "ArtifactEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessTokens");

            migrationBuilder.DropTable(
                name: "ArtifactFile");

            migrationBuilder.DropTable(
                name: "ArtifactEntry");

            migrationBuilder.DropTable(
                name: "ArtifactBranch");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
