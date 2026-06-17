using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CFCHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotReminderSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_Status",
                schema: "__template",
                table: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "Type",
                schema: "__template",
                table: "outbox_messages",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "__template",
                table: "outbox_messages",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Payload",
                schema: "__template",
                table: "outbox_messages",
                newName: "payload");

            migrationBuilder.RenameColumn(
                name: "Error",
                schema: "__template",
                table: "outbox_messages",
                newName: "error");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "__template",
                table: "outbox_messages",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProcessedOnUtc",
                schema: "__template",
                table: "outbox_messages",
                newName: "processed_at");

            migrationBuilder.RenameColumn(
                name: "OccurredOnUtc",
                schema: "__template",
                table: "outbox_messages",
                newName: "scheduled_after");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReminderSentAt",
                schema: "__template",
                table: "scheduling_slots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "attempts",
                schema: "__template",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "__template",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "error_details",
                schema: "__template",
                table: "outbox_messages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_attempts",
                schema: "__template",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "idx_outbox_pending",
                schema: "__template",
                table: "outbox_messages",
                columns: new[] { "status", "scheduled_after" },
                filter: "status = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_outbox_pending",
                schema: "__template",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                schema: "__template",
                table: "scheduling_slots");

            migrationBuilder.DropColumn(
                name: "attempts",
                schema: "__template",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "__template",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "error_details",
                schema: "__template",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "max_attempts",
                schema: "__template",
                table: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "__template",
                table: "outbox_messages",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "__template",
                table: "outbox_messages",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "payload",
                schema: "__template",
                table: "outbox_messages",
                newName: "Payload");

            migrationBuilder.RenameColumn(
                name: "error",
                schema: "__template",
                table: "outbox_messages",
                newName: "Error");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "__template",
                table: "outbox_messages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "scheduled_after",
                schema: "__template",
                table: "outbox_messages",
                newName: "OccurredOnUtc");

            migrationBuilder.RenameColumn(
                name: "processed_at",
                schema: "__template",
                table: "outbox_messages",
                newName: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status",
                schema: "__template",
                table: "outbox_messages",
                column: "Status",
                filter: "status = 'Pending'");
        }
    }
}
