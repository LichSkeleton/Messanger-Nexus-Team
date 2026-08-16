namespace NexusTeam.Server.Validators
{
    using FluentValidation;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Validator for adding participants to a group chat.
    /// </summary>
    public class AddChatParticipantsRequestValidator : AbstractValidator<AddChatParticipantsRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddChatParticipantsRequestValidator"/> class.
        /// </summary>
        public AddChatParticipantsRequestValidator()
        {
            this.RuleFor(x => x.UserIds)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("User list is required.")
                .Must(ids => ids.Count >= 1).WithMessage("Select at least one user to add.")
                .Must(ids => ids.Count <= 50).WithMessage("Cannot add more than 50 users at once.");

            this.RuleForEach(x => x.UserIds)
                .NotEmpty().WithMessage("User ID is required.");
        }
    }
}
