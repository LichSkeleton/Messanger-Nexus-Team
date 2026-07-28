namespace NexusTeam.Shared.Helpers
{
    using System;
    using System.Collections.Generic;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.ValueObjects;

    /// <summary>
    /// Helper class for creating paginated responses.
    /// </summary>
    public static class PaginationHelper
    {
        /// <summary>
        /// Creates a paginated response from a list of items and pagination metadata.
        /// </summary>
        /// <typeparam name="T">The type of items in the response.</typeparam>
        /// <param name="items">The items for the current page.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
        /// <param name="pageRequest">The page request parameters.</param>
        /// <returns>A paginated response with items and metadata.</returns>
        public static PaginatedResponse<T> CreateResponse<T>(
            List<T> items,
            int totalCount,
            PageRequest pageRequest)
        {
            if (pageRequest == null)
            {
                throw new ArgumentNullException(nameof(pageRequest));
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageRequest.PageSize);

            return new PaginatedResponse<T>
            {
                Items = items ?? new List<T>(),
                Page = pageRequest.Page,
                PageSize = pageRequest.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
            };
        }
    }
}
