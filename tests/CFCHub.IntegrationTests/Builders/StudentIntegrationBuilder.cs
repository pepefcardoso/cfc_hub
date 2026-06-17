using System;
using CFCHub.Application.Enrollment.Commands.CreateStudent;
using CFCHub.Domain.Enrollment;

namespace CFCHub.IntegrationTests.Builders;

public class StudentIntegrationBuilder
{
    private string _name = "João da Silva";
    private string _cpf = "12345678909";
    private string _email = "joao@example.com";
    private string _phone = "11999999999";
    private DateOnly _birthDate = new DateOnly(1990, 1, 1);
    private string _policyVersion = "1.0";
    private string _policyContentHash = "hash123";
    private ConsentChannel _consentChannel = ConsentChannel.Web;

    public StudentIntegrationBuilder WithCpf(string cpf)
    {
        _cpf = cpf;
        return this;
    }

    public StudentIntegrationBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public StudentIntegrationBuilder WithoutConsent()
    {
        _policyVersion = string.Empty;
        _policyContentHash = string.Empty;
        return this;
    }

    public CreateStudentCommand BuildCommand()
    {
        return new CreateStudentCommand(
            _name,
            _cpf,
            "1234567",
            _email,
            _phone,
            _birthDate,
            new AddressRequest("Rua", "123", null, "Bairro", "Cidade", "SP", "01234567"),
            _policyVersion,
            _policyContentHash,
            _consentChannel
        );
    }
}
