namespace NexusTeam.Server.Tests.Validators
{
    using System.Collections.Generic;
    using FluentValidation.TestHelper;
    using NexusTeam.Server.Validators;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class CreateChatFolderRequestValidatorTests
    {
        private readonly CreateChatFolderRequestValidator validator = new CreateChatFolderRequestValidator();

        [Fact]
        public void Validate_WithNameAndChat_HasNoErrors()
        {
            var result = this.validator.TestValidate(new CreateChatFolderRequest
            {
                Name = "Work",
                ChatIds = new List<string> { "chat-1" },
            });

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyChats_HasPropertyError()
        {
            var result = this.validator.TestValidate(new CreateChatFolderRequest
            {
                Name = "Work",
                ChatIds = new List<string>(),
            });

            result.ShouldHaveValidationErrorFor(x => x.ChatIds)
                .WithErrorMessage("Folder must contain at least one chat.");
        }

        [Fact]
        public void Validate_WithBlankName_HasPropertyError()
        {
            var result = this.validator.TestValidate(new CreateChatFolderRequest
            {
                Name = " ",
                ChatIds = new List<string> { "chat-1" },
            });

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }
    }
}
