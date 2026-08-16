namespace NexusTeam.Server.Validators
{
    using System.Linq;
    using FluentValidation;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Validator for create/update chat folder requests.
    /// </summary>
    public class CreateChatFolderRequestValidator : AbstractValidator<CreateChatFolderRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateChatFolderRequestValidator"/> class.
        /// </summary>
        public CreateChatFolderRequestValidator()
        {
            this.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Folder name is required")
                .MaximumLength(100).WithMessage("Folder name must not exceed 100 characters");

            this.RuleFor(x => x.ChatIds)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Chat list is required")
                .Must(ids => ids.Count(id => !string.IsNullOrWhiteSpace(id)) >= 1)
                .WithMessage("Folder must contain at least one chat.");
        }
    }
}
