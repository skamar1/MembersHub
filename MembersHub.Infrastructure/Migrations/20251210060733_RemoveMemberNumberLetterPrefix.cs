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
            // Renumber all members sequentially (001, 002, 003...) based on their Id
            // This ensures no duplicates when removing letter prefixes
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT ""Id"", ROW_NUMBER() OVER (ORDER BY ""Id"") as rn
                    FROM ""Members""
                )
                UPDATE ""Members"" m
                SET ""MemberNumber"" = LPAD(n.rn::text, 3, '0')
                FROM numbered n
                WHERE m.""Id"" = n.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
