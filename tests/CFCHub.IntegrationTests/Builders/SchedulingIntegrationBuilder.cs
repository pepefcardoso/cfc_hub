using System;
using System.Threading.Tasks;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Enrollment;
using CFCHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CFCHub.IntegrationTests.Builders;

public class SchedulingIntegrationBuilder
{
    private readonly AppDbContext _dbContext;

    public SchedulingIntegrationBuilder(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedInstructorAsync(Guid instructorId, string name = "Test Instructor")
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"INSERT INTO instructors (id, linked_user_id, name, categories, max_daily_slots, status)
              VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
            instructorId, Guid.NewGuid(), name, "B", 8, "Active");
    }

    public async Task SeedVehicleAsync(Guid vehicleId, string licensePlate = "ABC1234", string category = "B")
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"INSERT INTO vehicles (id, license_plate, category, status)
              VALUES ({0}, {1}, {2}, {3})",
            vehicleId, licensePlate, category, "Active");
    }

    public async Task SeedTrackAsync(Guid trackId, string name = "Test Track", string type = "Car")
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"INSERT INTO tracks (id, name, type)
              VALUES ({0}, {1}, {2})",
            trackId, name, type);
    }

    public async Task SeedStudentAsync(Guid studentId, string name = "Test Student", string cpf = "12345678901")
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"INSERT INTO students (id, name, cpf, email, phone, birth_date, home_address_street, home_address_city, home_address_state, home_address_zip_code, status, created_at)
              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11})",
            studentId, name, cpf, "test@example.com", "11999999999", new DateOnly(2000, 1, 1), "Rua A", "Sao Paulo", "SP", "01000-000", "Active", DateTimeOffset.UtcNow);
    }

    public async Task SeedSchedulingSlotAsync(
        Guid slotId,
        Guid instructorId,
        Guid vehicleId,
        Guid trackId,
        Guid studentId,
        DateTimeOffset startedAt,
        string status = "Confirmed")
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"INSERT INTO scheduling_slots (id, instructor_id, vehicle_id, track_id, student_id, started_at, ended_at, status)
              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})",
            slotId, instructorId, vehicleId, trackId, studentId, startedAt, startedAt.AddMinutes(50), status);
    }
}
