using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StargateAPI.DataAccess;

#nullable disable

namespace StargateAPI.Migrations
{
    [DbContext(typeof(StargateContext))]
    [Migration("20261028000000_AddUniquePersonName")]
    public partial class AddUniquePersonName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Person_Name",
                table: "Person",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Person_Name",
                table: "Person");
        }
    }
}