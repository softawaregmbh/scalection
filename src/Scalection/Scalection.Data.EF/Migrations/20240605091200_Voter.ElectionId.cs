using Microsoft.EntityFrameworkCore.Migrations;
using Scalection.ServiceDefaults;

#nullable disable

namespace Scalection.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class VoterElectionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ElectionId",
                table: "Voters",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: DemoData.ElectionId);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElectionId",
                table: "Voters");
        }
    }
}
