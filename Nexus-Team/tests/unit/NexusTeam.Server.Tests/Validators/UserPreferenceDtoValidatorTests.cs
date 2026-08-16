namespace NexusTeam.Server.Tests.Validators
{
    using System.Collections.Generic;
    using FluentValidation.TestHelper;
    using NexusTeam.Server.Validators;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class UserPreferenceDtoValidatorTests
    {
        private readonly UserPreferenceDtoValidator validator = new UserPreferenceDtoValidator();

        [Theory]
        [InlineData("light")]
        [InlineData("dark")]
        public void Validate_WithSupportedTheme_HasNoThemeError(string theme)
        {
            var preferences = CreateValidPreferences();
            preferences.Theme = theme;

            var result = this.validator.TestValidate(preferences);

            result.ShouldNotHaveValidationErrorFor(x => x.Theme);
        }

        [Theory]
        [InlineData("Light")]
        [InlineData("system")]
        [InlineData("")]
        public void Validate_WithUnsupportedTheme_HasPropertyError(string theme)
        {
            var preferences = CreateValidPreferences();
            preferences.Theme = theme;

            var result = this.validator.TestValidate(preferences);

            result.ShouldHaveValidationErrorFor(x => x.Theme);
        }

        [Theory]
        [InlineData("e")]
        [InlineData("abcdef")]
        public void Validate_WithInvalidLanguageLength_HasPropertyError(string language)
        {
            var preferences = CreateValidPreferences();
            preferences.Language = language;

            var result = this.validator.TestValidate(preferences);

            result.ShouldHaveValidationErrorFor(x => x.Language);
        }

        [Fact]
        public void Validate_WithMoreThanOneHundredPinnedChats_HasPropertyError()
        {
            var preferences = CreateValidPreferences();
            preferences.PinnedChats = new List<string>();
            for (var index = 0; index < 101; index++)
            {
                preferences.PinnedChats.Add($"chat-{index}");
            }

            var result = this.validator.TestValidate(preferences);

            result.ShouldHaveValidationErrorFor(x => x.PinnedChats);
        }

        [Fact]
        public void Validate_WithMoreThanOneThousandMutedChats_HasPropertyError()
        {
            var preferences = CreateValidPreferences();
            preferences.MutedChats = new List<string>();
            for (var index = 0; index < 1001; index++)
            {
                preferences.MutedChats.Add($"chat-{index}");
            }

            var result = this.validator.TestValidate(preferences);

            result.ShouldHaveValidationErrorFor(x => x.MutedChats);
        }

        private static UserPreferenceDto CreateValidPreferences()
        {
            return new UserPreferenceDto
            {
                Theme = "light",
                Language = "en",
            };
        }
    }
}
