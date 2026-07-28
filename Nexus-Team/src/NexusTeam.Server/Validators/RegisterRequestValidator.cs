namespace NexusTeam.Server.Validators
{
    using FluentValidation;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Validator for user registration requests.
    /// </summary>
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterRequestValidator"/> class.
        /// </summary>
        public RegisterRequestValidator()
        {
            this.RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .Length(3, 50).WithMessage("Username must be between 3 and 50 characters")
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, underscores, and hyphens");

            this.RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

            this.RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit");

            this.RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .Length(1, 100).WithMessage("Display name must be between 1 and 100 characters");
        }
    }
}
