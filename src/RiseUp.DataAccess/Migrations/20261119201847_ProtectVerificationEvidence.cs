using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiseUp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ProtectVerificationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Retention",
                table: "VerificationDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Retention",
                table: "VerificationDocuments");
        }
    }
}
