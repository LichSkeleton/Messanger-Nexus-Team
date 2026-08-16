namespace NexusTeam.Server.Tests.Validators
{
    using FluentValidation.TestHelper;
    using NexusTeam.Server.Validators;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class ForwardMessageRequestValidatorTests
    {
        private readonly ForwardMessageRequestValidator validator = new ForwardMessageRequestValidator();

        [Fact]
        public void Validate_WithMessageId_HasNoErrors()
        {
            var result = this.validator.TestValidate(new ForwardMessageRequest { MessageId = "message-1" });

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithMissingMessageId_HasPropertyError()
        {
            var result = this.validator.TestValidate(new ForwardMessageRequest());

            result.ShouldHaveValidationErrorFor(x => x.MessageId)
                .WithErrorMessage("Message ID is required");
        }

        [Fact]
        public void Validate_WithMessageIdAboveMaximumLength_HasPropertyError()
        {
            var result = this.validator.TestValidate(new ForwardMessageRequest { MessageId = new string('a', 51) });

            result.ShouldHaveValidationErrorFor(x => x.MessageId);
        }
    }
}
