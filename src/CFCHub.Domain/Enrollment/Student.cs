using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Enrollment.Events;

namespace CFCHub.Domain.Enrollment;

public class Student : AggregateRoot<StudentId>, ISoftDeletable, IAuditable
{
    [Sensitive]
    public string Name { get; private set; }
    [Sensitive]
    public string Cpf { get; private set; }
    [Sensitive]
    public string? Rg { get; private set; }
    [Sensitive]
    public string Email { get; private set; }
    [Sensitive]
    public string Phone { get; private set; }
    [Sensitive]
    public DateOnly BirthDate { get; private set; }
    [Sensitive]
    public Address HomeAddress { get; private set; }
    
    public StudentStatus Status { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    private Student() : base(new StudentId(Guid.Empty))
    {
        Name = null!;
        Cpf = null!;
        Email = null!;
        Phone = null!;
        HomeAddress = null!;
    }

    private Student(
        StudentId id, 
        string name, 
        string cpf, 
        string? rg,
        string email, 
        string phone, 
        DateOnly birthDate, 
        Address homeAddress) 
        : base(id)
    {
        Name = name;
        Cpf = cpf;
        Rg = rg;
        Email = email;
        Phone = phone;
        BirthDate = birthDate;
        HomeAddress = homeAddress;
        Status = StudentStatus.Active;
    }

    public static Student Create(
        StudentId id, 
        string name, 
        string cpf, 
        string? rg,
        string email, 
        string phone, 
        DateOnly birthDate, 
        Address homeAddress, 
        ISystemClock clock, 
        IIdGenerator idGenerator)
    {
        if (!Regex.IsMatch(cpf, @"^\d{11}$"))
        {
            throw new ValidationException("CPF must contain exactly 11 digits.");
        }

        var student = new Student(id, name, cpf, rg, email, phone, birthDate, homeAddress);
        student.AddDomainEvent(new StudentCreatedEvent(student.Id, clock.UtcNow));

        return student;
    }

    public void Anonymize(ISystemClock clock)
    {
        if (Status != StudentStatus.PendingErasure)
        {
            throw new UnprocessableException("Student can only be anonymized if status is PendingErasure.");
        }

        Name = "[REMOVIDO]";
        Email = "[REMOVIDO]";
        Phone = "[REMOVIDO]";
        Rg = null;
        HomeAddress = Address.Empty;
        
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(Cpf));
            Cpf = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        AddDomainEvent(new StudentAnonymizedEvent(Id, clock.UtcNow));
    }

    public void SoftDelete(ISystemClock clock)
    {
        DeletedAt = clock.UtcNow;
    }

    public void RequestErasure()
    {
        Status = StudentStatus.PendingErasure;
    }
}
