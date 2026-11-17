using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimsApi.Migrations
{
    /// <inheritdoc />
    public partial class AdClaimsUserFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Claims_Users_CustomerId",
                table: "Claims",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_Users_CustomerId",
                table: "Claims");
        }
    }
}
