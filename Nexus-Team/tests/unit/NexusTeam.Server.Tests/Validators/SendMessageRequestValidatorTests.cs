namespace NexusTeam.Server.Tests.Validators
{
    using FluentValidation.TestHelper;
    using NexusTeam.Server.Validators;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class SendMessageRequestValidatorTests
    {
        private readonly SendMessageRequestValidator validator = new SendMessageRequestValidator();

        [Fact]
        public void Validate_WithNormalMessage_HasNoErrors()
        {
            var request = new SendMessageRequest { ChatId = "chat-1", Content = "Hello" };

            var result = this.validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithWhitespaceContent_AllowsAttachmentOnlyPlaceholder()
        {
            var request = new SendMessageRequest { ChatId = "chat-1", Content = " " };

            var result = this.validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Content);
        }

        [Fact]
        public void Validate_WithNullContent_HasPropertyError()
        {
            var request = new SendMessageRequest { ChatId = "chat-1", Content = null! };

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Message content is required");
        }

        [Fact]
        public void Validate_WithContentAboveMaximumLength_HasPropertyError()
        {
            var request = new SendMessageRequest
            {
                ChatId = "chat-1",
                Content = new string('a', 10001),
            };

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Content);
        }

        [Fact]
        public void Validate_WithReplyIdAboveMaximumLength_HasPropertyError()
        {
            var request = new SendMessageRequest
            {
                ChatId = "chat-1",
                Content = "Hello",
                ReplyToId = new string('a', 27),
            };

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ReplyToId);
        }

        [Fact]
        public void Validate_WithMissingChatId_HasPropertyError()
        {
            var request = new SendMessageRequest { Content = "Hello" };

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ChatId)
                .WithErrorMessage("Chat ID is required");
        }
    }
}
