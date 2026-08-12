namespace NexusTeam.Server.Tests.Validators
{
    using FluentValidation.TestHelper;
    using NexusTeam.Server.Validators;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class RegisterRequestValidatorTests
    {
        private readonly RegisterRequestValidator validator = new RegisterRequestValidator();

        [Fact]
        public void Validate_WithValidRegistration_HasNoErrors()
        {
            var result = this.validator.TestValidate(CreateValidRequest());

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("user name")]
        [InlineData("user!")]
        public void Validate_WithInvalidUsername_HasPropertyError(string username)
        {
            var request = CreateValidRequest();
            request.Username = username;

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Username);
        }

        [Fact]
        public void Validate_WithInvalidEmail_HasPropertyError()
        {
            var request = CreateValidRequest();
            request.Email = "not-an-email";

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format");
        }

        [Theory]
        [InlineData("Short1")]
        [InlineData("lowercase1")]
        [InlineData("UPPERCASE1")]
        [InlineData("NoDigitsHere")]
        public void Validate_WithWeakPassword_HasPropertyError(string password)
        {
            var request = CreateValidRequest();
            request.Password = password;

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_WithMissingDisplayName_HasPropertyError()
        {
            var request = CreateValidRequest();
            request.DisplayName = string.Empty;

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.DisplayName)
                .WithErrorMessage("Display name is required");
        }

        private static RegisterRequest CreateValidRequest()
        {
            return new RegisterRequest
            {
                Username = "alice_01",
                Email = "alice@example.com",
                Password = "Secure123",
                DisplayName = "Alice",
            };
        }
    }
}
