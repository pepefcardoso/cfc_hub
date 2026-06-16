// Target: __template schema — apply to tenant schemas via TenantMigrationOrchestrator
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CFCHub.Infrastructure.Persistence.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "__template");

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            // staff_users
            migrationBuilder.CreateTable(
                name: "staff_users",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_staff_users", x => x.id); });

            // instructors
            migrationBuilder.CreateTable(
                name: "instructors",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_instructors", x => x.id); });

            // vehicles
            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_plate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    renavam = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    category = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_vehicles", x => x.id); });

            // tracks
            migrationBuilder.CreateTable(
                name: "tracks",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_tracks", x => x.id); });

            // instructor_availability_templates
            migrationBuilder.CreateTable(
                name: "instructor_availability_templates",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table => { table.PrimaryKey("pk_instructor_availability_templates", x => x.id); });

            // day_availability_overrides
            migrationBuilder.CreateTable(
                name: "day_availability_overrides",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table => { table.PrimaryKey("pk_day_availability_overrides", x => x.id); });

            // students
            migrationBuilder.CreateTable(
                name: "students",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cpf = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_students", x => x.id); });

            // enrollments
            migrationBuilder.CreateTable(
                name: "enrollments",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_enrollments", x => x.id); });

            // consent_records
            migrationBuilder.CreateTable(
                name: "consent_records",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    policy_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    client_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false)
                },
                constraints: table => { table.PrimaryKey("pk_consent_records", x => x.id); });

            // contracts
            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    s3_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    signed_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    template_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    signature_data = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_contracts", x => x.id); });

            // signature_records
            migrationBuilder.CreateTable(
                name: "signature_records",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    document_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table => { table.PrimaryKey("pk_signature_records", x => x.id); });

            // payments
            migrationBuilder.CreateTable(
                name: "payments",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    method = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_payments", x => x.id); });

            // installments
            migrationBuilder.CreateTable(
                name: "installments",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_installments", x => x.id); });

            // document_records
            migrationBuilder.CreateTable(
                name: "document_records",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    s3_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_document_records", x => x.id); });

            // outbox_messages
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    occurred_on = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    processed_on = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("pk_outbox_messages", x => x.id); });

            // audit_logs
            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "TEXT", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    changes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => { table.PrimaryKey("pk_audit_logs", x => x.id); });

            // data_erasure_requests
            migrationBuilder.CreateTable(
                name: "data_erasure_requests",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_data_erasure_requests", x => x.id); });

                        // scheduling_slots
            migrationBuilder.CreateTable(
                name: "scheduling_slots",
                schema: "__template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    track_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    class_type = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "TIMESTAMPTZ", nullable: true)
                },
                constraints: table => { table.PrimaryKey("pk_scheduling_slots", x => x.id); });

            // Indexes
            migrationBuilder.CreateIndex(name: "idx_outbox_pending", schema: "__template", table: "outbox_messages", column: "status", filter: "status = 'Pending'");
            migrationBuilder.CreateIndex(name: "idx_scheduling_slots_instructor_date", schema: "__template", table: "scheduling_slots", columns: new[] { "instructor_id", "date" });
            migrationBuilder.CreateIndex(name: "idx_students_cpf", schema: "__template", table: "students", column: "cpf");
            migrationBuilder.CreateIndex(name: "idx_audit_logs_entity", schema: "__template", table: "audit_logs", columns: new[] { "entity_name", "entity_id" });

            // Exclusion Constraints
            migrationBuilder.Sql("ALTER TABLE __template.scheduling_slots ADD CONSTRAINT ex_scheduling_slots_instructor EXCLUDE USING gist (instructor_id WITH =, daterange(date, date, '[]') WITH &&, tsrange((date + start_time)::timestamp, (date + end_time)::timestamp, '()') WITH &&) WHERE (status != 'Cancelled');");
            migrationBuilder.Sql("ALTER TABLE __template.scheduling_slots ADD CONSTRAINT ex_scheduling_slots_vehicle EXCLUDE USING gist (vehicle_id WITH =, daterange(date, date, '[]') WITH &&, tsrange((date + start_time)::timestamp, (date + end_time)::timestamp, '()') WITH &&) WHERE (vehicle_id IS NOT NULL AND status != 'Cancelled');");
            migrationBuilder.Sql("ALTER TABLE __template.scheduling_slots ADD CONSTRAINT ex_scheduling_slots_track EXCLUDE USING gist (track_id WITH =, daterange(date, date, '[]') WITH &&, tsrange((date + start_time)::timestamp, (date + end_time)::timestamp, '()') WITH &&) WHERE (track_id IS NOT NULL AND status != 'Cancelled');");

            // RLS on audit_logs
            migrationBuilder.Sql("ALTER TABLE __template.audit_logs ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("CREATE POLICY audit_logs_insert_only ON __template.audit_logs FOR INSERT WITH CHECK (true);");
            migrationBuilder.Sql("CREATE POLICY audit_logs_select ON __template.audit_logs FOR SELECT USING (true);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "scheduling_slots", schema: "__template");
            migrationBuilder.DropTable(name: "data_erasure_requests", schema: "__template");
            migrationBuilder.DropTable(name: "audit_logs", schema: "__template");
            migrationBuilder.DropTable(name: "outbox_messages", schema: "__template");
            migrationBuilder.DropTable(name: "document_records", schema: "__template");
            migrationBuilder.DropTable(name: "installments", schema: "__template");
            migrationBuilder.DropTable(name: "payments", schema: "__template");
            migrationBuilder.DropTable(name: "signature_records", schema: "__template");
            migrationBuilder.DropTable(name: "contracts", schema: "__template");
            migrationBuilder.DropTable(name: "consent_records", schema: "__template");
            migrationBuilder.DropTable(name: "enrollments", schema: "__template");
            migrationBuilder.DropTable(name: "students", schema: "__template");
            migrationBuilder.DropTable(name: "day_availability_overrides", schema: "__template");
            migrationBuilder.DropTable(name: "instructor_availability_templates", schema: "__template");
            migrationBuilder.DropTable(name: "tracks", schema: "__template");
            migrationBuilder.DropTable(name: "vehicles", schema: "__template");
            migrationBuilder.DropTable(name: "instructors", schema: "__template");
            migrationBuilder.DropTable(name: "staff_users", schema: "__template");
        }
    }
}



