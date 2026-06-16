using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CFCHub.Infrastructure.Migrations
{
    public partial class AuditInterceptorSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedFields = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            // Enable Row Level Security and add policies
            migrationBuilder.Sql(@"
                ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;
                
                -- Allow inserts (append-only)
                CREATE POLICY audit_logs_insert_policy ON audit_logs
                    FOR INSERT
                    WITH CHECK (true);
                    
                -- Allow selects
                CREATE POLICY audit_logs_select_policy ON audit_logs
                    FOR SELECT
                    USING (true);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");
        }
    }
}
