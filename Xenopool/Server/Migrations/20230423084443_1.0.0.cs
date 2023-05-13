using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenopool.Server.Migrations
{
    /// <inheritdoc />
    public partial class _100 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoolAccounts",
                columns: table => new
                {
                    WalletAddress = table.Column<string>(type: "TEXT", nullable: false),
                    PayoutAmount = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MinimumPayoutAmount = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsBanned = table.Column<bool>(type: "INTEGER", nullable: false),
                    BanReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolAccounts", x => x.WalletAddress);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoolAccounts");
        }
    }
}
