using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitWithCorrectIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReservationStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReservationEndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PublicationDate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Info = table.Column<string>(type: "ntext", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_Documents_DocumentTypes",
                        column: x => x.TypeID,
                        principalTable: "DocumentTypes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "AuthorDocument",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    AuthorID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorDocument", x => x.id);
                    table.ForeignKey(
                        name: "FK_AuthorDocument_Authors",
                        column: x => x.AuthorID,
                        principalTable: "Authors",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_AuthorDocument_Documents",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "CategoryDocument",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryDocument", x => x.id);
                    table.ForeignKey(
                        name: "FK_CategoryDocument_Categories",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_CategoryDocument_Documents",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "DocumentInstances",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryNumber = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Available = table.Column<bool>(type: "bit", nullable: false),
                    DocumentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentInstances", x => x.id);
                    table.ForeignKey(
                        name: "FK_DocumentInstances_Documents",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ReservationDocument",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationID = table.Column<int>(type: "int", nullable: false),
                    DocumentInstanceID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationDocument", x => x.id);
                    table.ForeignKey(
                        name: "FK_ReservationDocument_DocumentInstances",
                        column: x => x.DocumentInstanceID,
                        principalTable: "DocumentInstances",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ReservationDocument_Reservations",
                        column: x => x.ReservationID,
                        principalTable: "Reservations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorDocument_AuthorID",
                table: "AuthorDocument",
                column: "AuthorID");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorDocument_DocumentID",
                table: "AuthorDocument",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryDocument_CategoryID",
                table: "CategoryDocument",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryDocument_DocumentID",
                table: "CategoryDocument",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentInstances_DocumentID",
                table: "DocumentInstances",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TypeID",
                table: "Documents",
                column: "TypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationDocument_DocumentInstanceID",
                table: "ReservationDocument",
                column: "DocumentInstanceID");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationDocument_ReservationID",
                table: "ReservationDocument",
                column: "ReservationID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorDocument");

            migrationBuilder.DropTable(
                name: "CategoryDocument");

            migrationBuilder.DropTable(
                name: "ReservationDocument");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "DocumentInstances");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "DocumentTypes");
        }
    }
}
