using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndBookingUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "catalog",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "users",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_UserId",
                schema: "catalog",
                table: "bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Login",
                schema: "catalog",
                table: "users",
                column: "Login",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_UserId",
                schema: "catalog",
                table: "bookings",
                column: "UserId",
                principalSchema: "catalog",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_UserId",
                schema: "catalog",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "users",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_bookings_UserId",
                schema: "catalog",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "catalog",
                table: "bookings");
        }
    }
}
