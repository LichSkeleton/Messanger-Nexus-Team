namespace NexusTeam.Shared.Tests.Exceptions
{
    using System;
    using System.Collections.Generic;
    using NexusTeam.Shared.Exceptions;
    using Xunit;

    public class DomainExceptionTests
    {
        [Theory]
        [InlineData("authentication", "Authentication failed")]
        [InlineData("duplicate-chat", "A chat with this name already exists.")]
        [InlineData("duplicate-user", "User with the same username or email already exists")]
        [InlineData("unauthorized", "Unauthorized access")]
        public void SpecializedException_WithDefaultConstructor_UsesExpectedMessage(
            string exceptionType,
            string expectedMessage)
        {
            var exception = CreateException(exceptionType);

            Assert.IsAssignableFrom<DomainException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void DuplicateChatException_WithChatName_IncludesNameInMessage()
        {
            var exception = new DuplicateChatException("Engineering");

            Assert.Equal(
                "A chat with the name 'Engineering' already exists for this user.",
                exception.Message);
        }

        [Fact]
        public void DomainException_WithInnerException_PreservesCause()
        {
            var cause = new InvalidOperationException("database unavailable");

            var exception = new DomainException("operation failed", cause);

            Assert.Equal("operation failed", exception.Message);
            Assert.Same(cause, exception.InnerException);
        }

        [Fact]
        public void ValidationException_WithDefaultConstructor_HasDefaultMessageAndNoErrors()
        {
            var exception = new ValidationException();

            Assert.Equal("One or more validation errors occurred.", exception.Message);
            Assert.Empty(exception.Errors);
        }

        [Fact]
        public void ValidationException_WithErrors_PreservesFieldErrors()
        {
            var errors = new Dictionary<string, string[]>
            {
                ["Email"] = new[] { "Email is required.", "Email is invalid." },
            };

            var exception = new ValidationException(errors);

            Assert.Same(errors, exception.Errors);
            Assert.Equal(2, exception.Errors["Email"].Length);
        }

        private static Exception CreateException(string exceptionType)
        {
            return exceptionType switch
            {
                "authentication" => new AuthenticationException(),
                "duplicate-chat" => new DuplicateChatException(),
                "duplicate-user" => new DuplicateUserException(),
                "unauthorized" => new UnauthorizedException(),
                _ => throw new Xunit.Sdk.XunitException($"Unknown exception type: {exceptionType}"),
            };
        }
    }
}
