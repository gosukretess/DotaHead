using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotaHead.Migrations
{
    /// <inheritdoc />
    public partial class ServerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    PeakHoursStart = table.Column<int>(type: "INTEGER", nullable: false),
                    PeakHoursEnd = table.Column<int>(type: "INTEGER", nullable: false),
                    PeakHoursRefreshTime = table.Column<int>(type: "INTEGER", nullable: false),
                    NormalRefreshTime = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Servers");
        }
    }
}
