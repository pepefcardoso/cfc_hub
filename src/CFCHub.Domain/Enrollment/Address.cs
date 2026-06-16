using System.Collections.Generic;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment;

public class Address : ValueObject
{
    public string Street { get; }
    public string Number { get; }
    public string? Complement { get; }
    public string District { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }

    public static Address Empty => new(string.Empty, string.Empty, null, string.Empty, string.Empty, string.Empty, string.Empty);

    public Address(string street, string number, string? complement, string district, string city, string state, string zipCode)
    {
        Street = street;
        Number = number;
        Complement = complement;
        District = district;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Address() { }
#pragma warning restore CS8618

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Number;
        yield return Complement;
        yield return District;
        yield return City;
        yield return State;
        yield return ZipCode;
    }
}
