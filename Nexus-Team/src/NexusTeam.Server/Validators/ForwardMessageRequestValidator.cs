namespace NexusTeam.Server.Validators
{
    using FluentValidation;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Validator for forward message requests.
    /// </summary>
    public class ForwardMessageRequestValidator : AbstractValidator<ForwardMessageRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardMessageRequestValidator"/> class.
        /// </summary>
        public ForwardMessageRequestValidator()
        {
            this.RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required")
                .MaximumLength(50).WithMessage("Message ID must not exceed 50 characters");
        }
    }
}
