using System;

namespace CFCHub.Application.Enrollment.Queries.GetStudent;

public class StudentResult
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
}
