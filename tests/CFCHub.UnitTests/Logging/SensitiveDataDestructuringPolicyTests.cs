using System;
using System.Linq;
using CFCHub.Api.Logging;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using Serilog.Core;
using Xunit;
using CFCHub.Domain.Shared;

namespace CFCHub.UnitTests.Logging;

public class SensitiveDataDestructuringPolicyTests
{
    public class TestDto
    {
        public string Name { get; set; } = string.Empty;
        
        [Sensitive]
        public string Cpf { get; set; } = string.Empty;
    }

    [Fact]
    public void SensitiveDataDestructuringPolicy_WhenPropertyHasSensitiveAttribute_ReplacesValueWithRedacted()
    {
        // Arrange
        var policy = new SensitiveDataDestructuringPolicy();
        var dto = new TestDto { Name = "John Doe", Cpf = "123.456.789-00" };
        var factory = new PropertyValueFactory(policy);

        // Act
        var canDestructure = policy.TryDestructure(dto, factory, out var result);

        // Assert
        canDestructure.Should().BeTrue();
        var structureValue = result as StructureValue;
        structureValue.Should().NotBeNull();

        var cpfProperty = structureValue!.Properties.FirstOrDefault(p => p.Name == "Cpf");
        cpfProperty.Should().NotBeNull();
        cpfProperty!.Value.ToString().Should().Be("\"[REDACTED]\"");

        var nameProperty = structureValue.Properties.FirstOrDefault(p => p.Name == "Name");
        nameProperty.Should().NotBeNull();
        nameProperty!.Value.ToString().Should().Be("\"John Doe\"");
    }
    
    // Create a dummy factory for testing
    private class PropertyValueFactory : ILogEventPropertyValueFactory
    {
        private readonly IDestructuringPolicy _policy;

        public PropertyValueFactory(IDestructuringPolicy policy)
        {
            _policy = policy;
        }

        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
        {
            if (value == null) return new ScalarValue(null);
            if (value is string s) return new ScalarValue(s);
            return new ScalarValue(value.ToString());
        }
    }
}
