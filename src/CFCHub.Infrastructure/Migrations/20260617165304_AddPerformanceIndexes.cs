using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CFCHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_students_CreatedAt",
                schema: "__template",
                table: "students",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_scheduling_slots_StartedAt_Category_Status",
                schema: "__template",
                table: "scheduling_slots",
                columns: new[] { "StartedAt", "Category", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_students_CreatedAt",
                schema: "__template",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_scheduling_slots_StartedAt_Category_Status",
                schema: "__template",
                table: "scheduling_slots");
        }
    }
}
