namespace NexusTeam.Server.Validators
{
    using FluentValidation;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Validator for create chat requests.
    /// </summary>
    public class CreateChatRequestValidator : AbstractValidator<CreateChatRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateChatRequestValidator"/> class.
        /// </summary>
        public CreateChatRequestValidator()
        {
            this.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Chat name is required")
                .MaximumLength(100).WithMessage("Chat name must not exceed 100 characters");

            this.RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Chat description must not exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            this.RuleFor(x => x.ParticipantIds)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Participant list is required")
                .Must(x => x.Count >= 1).WithMessage("At least one other participant is required (minimum 2 including creator)");

            this.RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid chat type");

            this.RuleFor(x => x.AvatarUrl)
                .MaximumLength(500).WithMessage("Avatar URL must not exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
        }
    }
}
