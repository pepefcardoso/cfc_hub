using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CFCHub.Api.Endpoints.Development;

public static class DevelopmentEndpoints
{
    public static void MapDevelopmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dev").AllowAnonymous();

        group.MapPost("/seed-test-data", async (
            HttpContext context,
            ITenantProvisioningService provisioningService,
            IServiceProvider sp,
            IPasswordHasher passwordHasher,
            IIdGenerator idGenerator,
            ISystemClock clock) =>
        {
            var slug = "test";
            var tenantId = Guid.NewGuid();
            var schemaName = $"cfc_{slug}";

            // 1. Provision Tenant
            await provisioningService.ProvisionAsync(slug, tenantId, CancellationToken.None);

            // 2. Create Context
            var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
            var tenantContext = new DevTenantContext(schemaName);
            using var dbContext = new AppDbContext(options, tenantContext, clock);

            // 3. Create Admin StaffUser
            var adminId = idGenerator.NewId<StaffUserId>();
            var adminUser = StaffUser.Create(
                adminId,
                "Admin Test",
                "admin@test.com",
                passwordHasher.Hash("password123"),
                RoleType.Admin,
                clock
            );
            dbContext.StaffUsers.Add(adminUser);

            // 4. Create Instructor StaffUser & Instructor
            var instrUserId = idGenerator.NewId<StaffUserId>();
            var instrUser = StaffUser.Create(
                instrUserId,
                "Instructor Test",
                "instructor@test.com",
                passwordHasher.Hash("password123"),
                RoleType.Instructor,
                clock
            );
            dbContext.StaffUsers.Add(instrUser);

            var instructor = new Instructor(
                idGenerator.NewId<InstructorId>(),
                instrUserId,
                "Instructor Test",
                new[] { CnhCategory.B },
                10
            );
            dbContext.Instructors.Add(instructor);

            // 5. Create Vehicle
            var vehicle = new Vehicle(
                idGenerator.NewId<VehicleId>(),
                "ABC1234",
                CnhCategory.B
            );
            dbContext.Vehicles.Add(vehicle);

            // 6. Create Student
            var studentId = idGenerator.NewId<CFCHub.Domain.Enrollment.StudentId>();
            var student = CFCHub.Domain.Enrollment.Student.Create(
                studentId,
                "Student Test",
                "12345678909",
                "1234567",
                "student@test.com",
                "11999999999",
                new DateOnly(2000, 1, 1),
                new CFCHub.Domain.Enrollment.Address("Rua", "123", null, "Bairro", "Cidade", "SP", "01234567"),
                clock,
                idGenerator
            );
            dbContext.Students.Add(student);

            await dbContext.SaveChangesAsync();

            return Results.Ok(new {
                Message = "Test data seeded successfully.",
                TenantSlug = slug,
                AdminEmail = "admin@test.com",
                InstructorEmail = "instructor@test.com",
                Password = "password123"
            });
        });
    }

    private class DevTenantContext : ITenantContext
    {
        public DevTenantContext(string schemaName)
        {
            SchemaName = schemaName;
        }

        public Guid TenantId => Guid.Empty;
        public string TenantSlug => SchemaName.Replace("cfc_", "");
        public string SchemaName { get; }
        public bool IsResolved => true;
    }
}
