using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LottoTM.Server.Api.Migrations
{
    /// <inheritdoc />
    public partial class Initail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Draws",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrawDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LottoType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Draws", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Draws_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrawNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrawId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawNumbers", x => x.Id);
                    table.CheckConstraint("CHK_DrawNumbers_Number", "[Number] >= 1 AND [Number] <= 49");
                    table.CheckConstraint("CHK_DrawNumbers_Position", "[Position] >= 1 AND [Position] <= 6");
                    table.ForeignKey(
                        name: "FK_DrawNumbers_Draws_DrawId",
                        column: x => x.DrawId,
                        principalTable: "Draws",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketNumbers", x => x.Id);
                    table.CheckConstraint("CHK_TicketNumbers_Number", "[Number] >= 1 AND [Number] <= 49");
                    table.CheckConstraint("CHK_TicketNumbers_Position", "[Position] >= 1 AND [Position] <= 6");
                    table.ForeignKey(
                        name: "FK_TicketNumbers_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrawNumbers_DrawId",
                table: "DrawNumbers",
                column: "DrawId");

            migrationBuilder.CreateIndex(
                name: "IX_DrawNumbers_DrawId_Position",
                table: "DrawNumbers",
                columns: new[] { "DrawId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawNumbers_Number",
                table: "DrawNumbers",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_Draws_CreatedByUserId",
                table: "Draws",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Draws_DrawDate_LottoType",
                table: "Draws",
                columns: new[] { "DrawDate", "LottoType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketNumbers_Number",
                table: "TicketNumbers",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_TicketNumbers_TicketId",
                table: "TicketNumbers",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketNumbers_TicketId_Position",
                table: "TicketNumbers",
                columns: new[] { "TicketId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_UserId",
                table: "Tickets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrawNumbers");

            migrationBuilder.DropTable(
                name: "TicketNumbers");

            migrationBuilder.DropTable(
                name: "Draws");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
