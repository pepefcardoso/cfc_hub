using System;
using System.Reflection;
using CFCHub.Domain.Enrollment;

namespace CFCHub.UnitTests.Builders;

public class StudentBuilder
{
    private StudentId _id = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private string _name = "John Doe";
    private string _cpf = "12345678909";
    private string? _rg = "1234567";
    private string _email = "john@example.com";
    private string _phone = "11999999999";
    private DateOnly _birthDate = new(1990, 1, 1);
    private Address _homeAddress = new Address("Street", "123", "Complement", "District", "City", "State", "12345678");
    private StudentStatus _status = StudentStatus.Active;

    public StudentBuilder WithId(StudentId id)
    {
        _id = id;
        return this;
    }

    public StudentBuilder WithCpf(string cpf)
    {
        _cpf = cpf;
        return this;
    }

    public StudentBuilder WithStatus(StudentStatus status)
    {
        _status = status;
        return this;
    }

    public Student Build()
    {
        var ctor = typeof(Student).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] 
        { 
            typeof(StudentId), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateOnly), typeof(Address) 
        }, null);
        
        if (ctor == null)
            throw new InvalidOperationException("Student constructor not found.");

        var student = (Student)ctor.Invoke(new object?[] { _id, _name, _cpf, _rg, _email, _phone, _birthDate, _homeAddress });
        
        typeof(Student).GetProperty(nameof(Student.Status))!.SetValue(student, _status);
        
        return student;
    }
}
