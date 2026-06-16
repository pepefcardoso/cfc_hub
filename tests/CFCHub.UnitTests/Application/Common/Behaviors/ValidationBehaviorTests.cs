using CFCHub.Application.Common.Behaviors;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using FluentValidation;
using MediatR;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Common.Behaviors;

public class ValidationBehaviorTests
{
    public class TestRequest : IRequest<string>
    {
        public string Prop1 { get; set; } = string.Empty;
        public string Prop2 { get; set; } = string.Empty;
    }

    public class TestValidator1 : AbstractValidator<TestRequest>
    {
        public TestValidator1()
        {
            RuleFor(x => x.Prop1).NotEmpty().WithErrorCode("PROP1_EMPTY");
        }
    }

    public class TestValidator2 : AbstractValidator<TestRequest>
    {
        public TestValidator2()
        {
            RuleFor(x => x.Prop2).NotEmpty().WithErrorCode("PROP2_EMPTY");
        }
    }

    [Fact]
    public async Task ValidationBehavior_WithMultipleFailures_CollectsAll()
    {
        // Arrange
        var validators = new IValidator<TestRequest>[]
        {
            new TestValidator1(),
            new TestValidator2()
        };

        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest { Prop1 = "", Prop2 = "" };
        
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act
        var act = async () => await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<CFCHub.Domain.Shared.Exceptions.ValidationException>();
        var errors = exception.Which.Errors;

        errors.Should().HaveCount(2);
        errors.Should().Contain(e => e.PropertyName == "Prop1" && e.ErrorCode == "PROP1_EMPTY");
        errors.Should().Contain(e => e.PropertyName == "Prop2" && e.ErrorCode == "PROP2_EMPTY");
        
        await next.DidNotReceive()();
    }
}
