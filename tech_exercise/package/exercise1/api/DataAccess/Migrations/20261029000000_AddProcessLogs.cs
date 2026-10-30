using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StargateAPI.DataAccess;

#nullable disable

namespace StargateAPI.Migrations
{
    [DbContext(typeof(StargateContext))]
    [Migration("20261029000000_AddProcessLogs")]
    public partial class AddProcessLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Operation = table.Column<string>(type: "TEXT", nullable: false),
                    Component = table.Column<string>(type: "TEXT", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    ExceptionType = table.Column<string>(type: "TEXT", nullable: true),
                    ExceptionDetails = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessLog", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProcessLog");
        }
    }
}
