namespace NexusTeam.Server.Controllers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using Serilog;

    /// <summary>
    /// Controller for user preference endpoints.
    /// </summary>
    [ApiController]
    [Route("api/preferences")]
    public class PreferencesController : ControllerBase
    {
        private readonly IUserPreferenceRepository preferenceRepository;
        private readonly IClock clock;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreferencesController"/> class.
        /// </summary>
        /// <param name="preferenceRepository">User preference repository.</param>
        /// <param name="clock">System clock.</param>
        /// <param name="logger">Logger.</param>
        public PreferencesController(IUserPreferenceRepository preferenceRepository, IClock clock, ILogger logger)
        {
            this.preferenceRepository = preferenceRepository;
            this.clock = clock;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the preferences for the authenticated user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user preferences DTO.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(UserPreferenceDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserPreferenceDto>> GetPreferences(CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var preference = await this.preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

            if (preference == null)
            {
                preference = new UserPreference
                {
                    UserId = userId,
                    NotificationsEnabled = true,
                    SoundEnabled = true,
                    Theme = "light",
                    Language = "en",
                    CreatedAt = this.clock.UtcNow,
                    UpdatedAt = this.clock.UtcNow,
                };

                await this.preferenceRepository.CreateAsync(preference, cancellationToken);
            }

            return this.Ok(this.MapToDto(preference));
        }

        /// <summary>
        /// Updates the preferences for the authenticated user.
        /// </summary>
        /// <param name="dto">The updated preferences.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated preferences DTO.</returns>
        [HttpPut]
        [ProducesResponseType(typeof(UserPreferenceDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserPreferenceDto>> UpdatePreferences([FromBody] UserPreferenceDto dto, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var preference = await this.preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

            if (preference == null)
            {
                preference = new UserPreference
                {
                    UserId = userId,
                    CreatedAt = this.clock.UtcNow,
                };
            }

            preference.NotificationsEnabled = dto.NotificationsEnabled;
            preference.SoundEnabled = dto.SoundEnabled;
            preference.Theme = dto.Theme;
            preference.Language = dto.Language;
            preference.MutedChats = dto.MutedChats;
            preference.UpdatedAt = this.clock.UtcNow;

            if (preference.Id == null)
            {
                await this.preferenceRepository.CreateAsync(preference, cancellationToken);
            }
            else
            {
                await this.preferenceRepository.UpdateAsync(preference, cancellationToken);
            }

            this.logger.Information("Preferences updated for user {UserId}", userId);

            return this.Ok(this.MapToDto(preference));
        }

        private UserPreferenceDto MapToDto(UserPreference preference)
        {
            return new UserPreferenceDto
            {
                UserId = preference.UserId,
                NotificationsEnabled = preference.NotificationsEnabled,
                SoundEnabled = preference.SoundEnabled,
                Theme = preference.Theme,
                Language = preference.Language,
                MutedChats = preference.MutedChats,
            };
        }
    }
}
