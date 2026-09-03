namespace NexusTeam.Server.Validators
{
    using FluentValidation;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Validator for user login requests.
    /// </summary>
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginRequestValidator"/> class.
        /// </summary>
        public LoginRequestValidator()
        {
            this.RuleFor(x => x.UsernameOrEmail)
                .NotEmpty().WithMessage("Username or email is required")
                .MaximumLength(255).WithMessage("Username or email must not exceed 255 characters");

            this.RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters");

            this.RuleFor(x => x.DeviceId)
                .NotEmpty().WithMessage("Device ID is required")
                .Must(value => System.Guid.TryParse(value, out _)).WithMessage("Device ID must be a valid UUID");

            this.RuleFor(x => x.DeviceName)
                .NotEmpty().WithMessage("Device name is required")
                .MaximumLength(120).WithMessage("Device name must not exceed 120 characters");
        }
    }
}
