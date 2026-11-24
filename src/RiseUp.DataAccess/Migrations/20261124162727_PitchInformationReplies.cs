using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiseUp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PitchInformationReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PitchInfoItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReplyText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AnsweredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplyMediaId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitchInfoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitchInfoItems_ConnectionRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ConnectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PitchInfoItems_MediaAssets_ReplyMediaId",
                        column: x => x.ReplyMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PitchInfoItems_ReplyMediaId",
                table: "PitchInfoItems",
                column: "ReplyMediaId",
                unique: true,
                filter: "[ReplyMediaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PitchInfoItems_RequestId",
                table: "PitchInfoItems",
                column: "RequestId");

            migrationBuilder.Sql(PitchInformationSchemaV1.ImportRequests);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PitchInfoItems");
        }
    }
}
