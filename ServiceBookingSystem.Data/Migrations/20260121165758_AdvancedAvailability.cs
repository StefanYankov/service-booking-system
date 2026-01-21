using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBookingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdvancedAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "OperatingHours");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "OperatingHours");

            migrationBuilder.CreateTable(
                name: "OperatingSegments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperatingHourId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatingSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatingSegments_OperatingHours_OperatingHourId",
                        column: x => x.OperatingHourId,
                        principalTable: "OperatingHours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsDayOff = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleOverrides_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OverrideSegments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleOverrideId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverrideSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OverrideSegments_ScheduleOverrides_ScheduleOverrideId",
                        column: x => x.ScheduleOverrideId,
                        principalTable: "ScheduleOverrides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperatingSegments_OperatingHourId",
                table: "OperatingSegments",
                column: "OperatingHourId");

            migrationBuilder.CreateIndex(
                name: "IX_OverrideSegments_ScheduleOverrideId",
                table: "OverrideSegments",
                column: "ScheduleOverrideId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleOverrides_ServiceId",
                table: "ScheduleOverrides",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperatingSegments");

            migrationBuilder.DropTable(
                name: "OverrideSegments");

            migrationBuilder.DropTable(
                name: "ScheduleOverrides");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "OperatingHours",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "OperatingHours",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }
    }
}
