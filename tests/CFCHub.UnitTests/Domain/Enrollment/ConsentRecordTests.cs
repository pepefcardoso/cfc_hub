using System.Reflection;
using CFCHub.Domain.Enrollment;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Enrollment;

public class ConsentRecordTests
{
    [Fact]
    public void ConsentRecord_IsImmutable_NoUpdateMethod()
    {
        // Assert
        var type = typeof(ConsentRecord);
        
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        
        // Ensure there are no Update, Change, or public setter-like methods
        methods.Should().NotContain(m => m.Name.Contains("Update"));
        methods.Should().NotContain(m => m.Name.Contains("Change"));
        methods.Should().NotContain(m => m.Name.StartsWith("Set"));
    }
}
