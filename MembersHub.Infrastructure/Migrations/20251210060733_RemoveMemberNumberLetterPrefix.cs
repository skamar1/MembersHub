using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MembersHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMemberNumberLetterPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the letter prefix (A, K, F, M) from member numbers
            // Changes A001 -> 001, K002 -> 002, etc.
            migrationBuilder.Sql(@"
                UPDATE ""Members""
                SET ""MemberNumber"" = SUBSTRING(""MemberNumber"" FROM 2)
                WHERE ""MemberNumber"" ~ '^[A-Z]';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
