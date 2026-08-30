using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransactionAggregation.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionBusinessKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Transactions_Source_ExternalTransactionId",
                table: "Transactions",
                columns: new[] { "Source", "ExternalTransactionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Transactions_Source_ExternalTransactionId",
                table: "Transactions");
        }
    }
}
