namespace NexusTeam.Server.Validators
{
    using FluentValidation;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Validator for send message requests.
    /// </summary>
    public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageRequestValidator"/> class.
        /// </summary>
        public SendMessageRequestValidator()
        {
            this.RuleFor(x => x.ChatId)
                .NotEmpty().WithMessage("Chat ID is required");

            // Content is required (can be whitespace placeholder for attachment-only messages)
            this.RuleFor(x => x.Content)
                .NotNull().WithMessage("Message content is required")
                .MaximumLength(10000).WithMessage("Message content must not exceed 10000 characters");

            this.RuleFor(x => x.ReplyToId)
                .MaximumLength(26).WithMessage("Reply to ID must not exceed 26 characters")
                .When(x => !string.IsNullOrEmpty(x.ReplyToId));
        }
    }
}
