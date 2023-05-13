using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenopool.Server.Migrations
{
    /// <inheritdoc />
    public partial class _101 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoolShare",
                columns: table => new
                {
                    WalletAddress = table.Column<string>(type: "TEXT", nullable: false),
                    WorkerId = table.Column<string>(type: "TEXT", nullable: false),
                    Height = table.Column<long>(type: "INTEGER", nullable: false),
                    SharePoints = table.Column<long>(type: "INTEGER", nullable: false),
                    PoolAccountWalletAddress = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolShare", x => x.WalletAddress);
                    table.ForeignKey(
                        name: "FK_PoolShare_PoolAccounts_PoolAccountWalletAddress",
                        column: x => x.PoolAccountWalletAddress,
                        principalTable: "PoolAccounts",
                        principalColumn: "WalletAddress");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoolShare_PoolAccountWalletAddress",
                table: "PoolShare",
                column: "PoolAccountWalletAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoolShare");
        }
    }
}
