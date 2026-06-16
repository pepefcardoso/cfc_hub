using System;
using CFCHub.Domain.Enrollment;
using MediatR;

namespace CFCHub.Application.Enrollment.Commands.CreateStudent;

public record AddressRequest(string Street, string Number, string? Complement, string District, string City, string State, string ZipCode);

public record CreateStudentCommand(
    string Name,
    string Cpf,
    string? Rg,
    string Email,
    string Phone,
    DateOnly BirthDate,
    AddressRequest HomeAddress,
    string PolicyVersion,
    string PolicyContentHash,
    ConsentChannel ConsentChannel) : IRequest<CreateStudentResult>;
