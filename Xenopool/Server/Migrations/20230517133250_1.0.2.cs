using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenopool.Server.Migrations
{
    /// <inheritdoc />
    public partial class _102 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PayoutAmount",
                table: "PoolAccounts",
                newName: "WalletAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WalletAmount",
                table: "PoolAccounts",
                newName: "PayoutAmount");
        }
    }
}
