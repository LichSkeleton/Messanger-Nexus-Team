namespace NexusTeam.Server.Tests.Validators
{
    using System.Collections.Generic;
    using FluentValidation.TestHelper;
    using NexusTeam.Server.Validators;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class AddChatParticipantsRequestValidatorTests
    {
        private readonly AddChatParticipantsRequestValidator validator = new AddChatParticipantsRequestValidator();

        [Fact]
        public void Validate_WithUserIds_HasNoErrors()
        {
            var result = this.validator.TestValidate(new AddChatParticipantsRequest
            {
                UserIds = new List<string> { "user-1" },
            });

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyList_HasPropertyError()
        {
            var result = this.validator.TestValidate(new AddChatParticipantsRequest
            {
                UserIds = new List<string>(),
            });

            result.ShouldHaveValidationErrorFor(x => x.UserIds);
        }

        [Fact]
        public void Validate_WithBlankUserId_HasPropertyError()
        {
            var result = this.validator.TestValidate(new AddChatParticipantsRequest
            {
                UserIds = new List<string> { " " },
            });

            result.ShouldHaveValidationErrorFor("UserIds[0]");
        }
    }
}
