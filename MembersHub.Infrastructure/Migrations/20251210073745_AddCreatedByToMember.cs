using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MembersHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByToMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Members",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Members",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedByUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Members",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedByUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Members",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedByUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Members",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedByUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Members",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedByUserId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Members_CreatedByUserId",
                table: "Members",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Users_CreatedByUserId",
                table: "Members",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Users_CreatedByUserId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_CreatedByUserId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Members");
        }
    }
}
