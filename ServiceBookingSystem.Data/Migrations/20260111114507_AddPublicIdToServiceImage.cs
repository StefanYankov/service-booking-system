using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBookingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicIdToServiceImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "ServiceImages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ServiceImages");
        }
    }
}
