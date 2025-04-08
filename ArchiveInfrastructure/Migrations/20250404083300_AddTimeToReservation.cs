using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeToReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservationEndDate",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ReservationStartDate",
                table: "Reservations");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationEndDateTime",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationStartDateTime",
                table: "Reservations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservationEndDateTime",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ReservationStartDateTime",
                table: "Reservations");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReservationEndDate",
                table: "Reservations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReservationStartDate",
                table: "Reservations",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
