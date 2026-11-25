using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiseUp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TeamGroupAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreviousConversationIds",
                table: "TeamAccounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "SessionVersion",
                table: "TeamAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GroupMode",
                table: "TeamAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old schema restores explicit group IDs for deactivated members.
            migrationBuilder.Sql("""
                UPDATE t SET PreviousConversationIds = COALESCE((
                    SELECT STRING_AGG(CONVERT(nvarchar(max), c.Id), ',')
                    FROM Conversations c
                    WHERE c.StartupId = t.StartupId AND (t.GroupMode = 1 OR NOT EXISTS (
                        SELECT 1 FROM STRING_SPLIT(t.PreviousConversationIds, ',') s WHERE TRY_CONVERT(int, s.value) = c.Id
                    ))
                ), '')
                FROM TeamAccounts t WHERE t.Status = 2 AND t.GroupMode IN (1, 2);

                IF EXISTS (SELECT 1 FROM TeamAccounts WHERE LEN(PreviousConversationIds) > 500)
                    THROW 51003, 'Saved group assignments exceed the size supported by the older schema.', 1;
                """);
            migrationBuilder.DropColumn(name: "SessionVersion", table: "TeamAccounts");
            migrationBuilder.DropColumn(
                name: "GroupMode",
                table: "TeamAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "PreviousConversationIds",
                table: "TeamAccounts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
