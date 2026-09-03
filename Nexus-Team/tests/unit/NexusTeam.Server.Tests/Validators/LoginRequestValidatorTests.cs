namespace NexusTeam.Server.Tests.Validators
{
    using FluentValidation.TestHelper;
    using NexusTeam.Server.Validators;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class LoginRequestValidatorTests
    {
        private readonly LoginRequestValidator validator = new LoginRequestValidator();

        [Fact]
        public void Validate_WithValidCredentials_HasNoErrors()
        {
            var request = new LoginRequest
            {
                UsernameOrEmail = "alice@example.com",
                Password = "password",
                DeviceId = "7bc83f5d-0ed1-4c75-a225-b0e5d79f4817",
                DeviceName = "Chrome on macOS",
            };

            var result = this.validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithInvalidDeviceId_HasPropertyError()
        {
            var request = new LoginRequest
            {
                UsernameOrEmail = "alice",
                Password = "password",
                DeviceId = "not-a-uuid",
                DeviceName = "Browser",
            };

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.DeviceId);
        }

        [Fact]
        public void Validate_WithMissingUsernameOrEmail_HasPropertyError()
        {
            var request = new LoginRequest { Password = "password" };

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.UsernameOrEmail)
                .WithErrorMessage("Username or email is required");
        }

        [Fact]
        public void Validate_WithPasswordAboveMaximumLength_HasPropertyError()
        {
            var request = new LoginRequest
            {
                UsernameOrEmail = "alice",
                Password = new string('a', 101),
            };

            var result = this.validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must not exceed 100 characters");
        }
    }
}
