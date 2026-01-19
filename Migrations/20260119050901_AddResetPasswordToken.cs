using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace loginAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddResetPasswordToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpired",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Password", "ResetToken", "ResetTokenExpired" },
                values: new object[] { new DateTime(2026, 1, 19, 5, 9, 1, 202, DateTimeKind.Utc).AddTicks(2895), "AQAAAAIAAYagAAAAEHDc/zpcK3DH4DDgurqIXguDAXj7/pqiTV9gdfToM4DrBY5HvFfk0kYlxTG0JOje5g==", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpired",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Password" },
                values: new object[] { new DateTime(2026, 1, 16, 7, 56, 7, 703, DateTimeKind.Utc).AddTicks(9086), "AQAAAAIAAYagAAAAEFfcj38FiKc2iiuX07mpOuDNlM5ZfJfK001OlG158VmhX+14MyAwx1s4d8nO0fuRkg==" });
        }
    }
}
