namespace NexusTeam.Shared.ValueObjects
{
    /// <summary>
    /// Value object representing a pagination request.
    /// Provides validation and defaults for page-based queries.
    /// </summary>
    public class PageRequest
    {
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 20;

        private int page = 1;
        private int pageSize = DefaultPageSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageRequest"/> class.
        /// </summary>
        public PageRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageRequest"/> class.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        public PageRequest(int page, int pageSize)
        {
            this.Page = page;
            this.PageSize = pageSize;
        }

        /// <summary>
        /// Gets or sets the page number (1-based).
        /// Minimum value is 1.
        /// </summary>
        public int Page
        {
            get => this.page;
            set => this.page = value < 1 ? 1 : value;
        }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// Valid range is 1 to 100, defaults to 20.
        /// </summary>
        public int PageSize
        {
            get => this.pageSize;
            set
            {
                if (value < 1)
                {
                    this.pageSize = DefaultPageSize;
                }
                else if (value > MaxPageSize)
                {
                    this.pageSize = MaxPageSize;
                }
                else
                {
                    this.pageSize = value;
                }
            }
        }

        /// <summary>
        /// Gets the number of items to skip for database queries.
        /// </summary>
        public int Skip => (this.Page - 1) * this.PageSize;

        /// <summary>
        /// Gets the number of items to take for database queries.
        /// </summary>
        public int Take => this.PageSize;
    }
}
