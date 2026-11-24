using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiseUp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UniqueAccountContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AccountContactSchemaV1.Install(sqlite: false))
                migrationBuilder.Sql("EXEC(N'" + sql.Replace("'", "''") + "')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER TR_UserAccounts_Contacts, TR_TeamAccounts_Contacts");
            migrationBuilder.Sql("DROP TABLE AccountContacts");
        }
    }
}
