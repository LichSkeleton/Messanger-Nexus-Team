namespace NexusTeam.Server.Validators
{
    using FluentValidation;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Validator for user preference DTOs.
    /// </summary>
    public class UserPreferenceDtoValidator : AbstractValidator<UserPreferenceDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserPreferenceDtoValidator"/> class.
        /// </summary>
        public UserPreferenceDtoValidator()
        {
            this.RuleFor(x => x.Theme)
                .NotEmpty().WithMessage("Theme is required")
                .Must(theme => theme == "light" || theme == "dark").WithMessage("Theme must be either 'light' or 'dark'");

            this.RuleFor(x => x.Language)
                .NotEmpty().WithMessage("Language is required")
                .Length(2, 5).WithMessage("Language code must be between 2 and 5 characters");

            this.RuleFor(x => x.MutedChats)
                .Must(chats => chats == null || chats.Count <= 1000).WithMessage("Muted chats list cannot exceed 1000 items");

            this.RuleFor(x => x.PinnedChats)
                .Must(chats => chats == null || chats.Count <= 100).WithMessage("Pinned chats list cannot exceed 100 items");
        }
    }
}
