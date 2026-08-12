namespace NexusTeam.Server.Tests.Validators
{
    using System.Collections.Generic;
    using FluentValidation.TestHelper;
    using NexusTeam.Server.Validators;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using Xunit;

    public class CreateChatRequestValidatorTests
    {
        private readonly CreateChatRequestValidator validator = new CreateChatRequestValidator();

        [Fact]
        public void Validate_WithValidChat_HasNoErrors()
        {
            var result = this.validator.TestValidate(CreateValidRequest());

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyParticipants_HasPropertyError()
        {
            var request = CreateValidRequest();
            request.ParticipantIds.Clear();

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
                .WithErrorMessage("At least one other participant is required (minimum 2 including creator)");
        }

        [Fact]
        public void Validate_WithNullParticipants_HasRequiredError()
        {
            var request = CreateValidRequest();
            request.ParticipantIds = null!;

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
                .WithErrorMessage("Participant list is required");
        }

        [Fact]
        public void Validate_WithUnknownChatType_HasPropertyError()
        {
            var request = CreateValidRequest();
            request.Type = (ChatType)999;

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Type)
                .WithErrorMessage("Invalid chat type");
        }

        [Fact]
        public void Validate_WithDescriptionAboveMaximumLength_HasPropertyError()
        {
            var request = CreateValidRequest();
            request.Description = new string('a', 501);

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        private static CreateChatRequest CreateValidRequest()
        {
            return new CreateChatRequest
            {
                Name = "Engineering",
                Type = ChatType.Group,
                ParticipantIds = new List<string> { "user-1" },
                Description = "Engineering team",
            };
        }
    }
}
