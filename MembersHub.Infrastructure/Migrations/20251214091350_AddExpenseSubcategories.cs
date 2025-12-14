using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MembersHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseSubcategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCategoryId",
                table: "ExpenseCategories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_ParentCategoryId",
                table: "ExpenseCategories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_ExpenseCategories_ParentCategoryId",
                table: "ExpenseCategories",
                column: "ParentCategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategories_ExpenseCategories_ParentCategoryId",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_ParentCategoryId",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "ExpenseCategories");
        }
    }
}
