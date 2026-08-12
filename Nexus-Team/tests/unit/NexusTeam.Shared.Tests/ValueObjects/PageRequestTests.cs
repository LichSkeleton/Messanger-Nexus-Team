namespace NexusTeam.Shared.Tests.ValueObjects
{
    using NexusTeam.Shared.ValueObjects;
    using Xunit;

    public class PageRequestTests
    {
        [Fact]
        public void Constructor_WithoutArguments_UsesDefaults()
        {
            var request = new PageRequest();

            Assert.Equal(1, request.Page);
            Assert.Equal(20, request.PageSize);
            Assert.Equal(0, request.Skip);
            Assert.Equal(20, request.Take);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Page_WhenBelowOne_IsNormalizedToFirstPage(int page)
        {
            var request = new PageRequest(page, pageSize: 20);

            Assert.Equal(1, request.Page);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void PageSize_WhenBelowOne_UsesDefaultSize(int pageSize)
        {
            var request = new PageRequest(page: 1, pageSize);

            Assert.Equal(20, request.PageSize);
        }

        [Fact]
        public void PageSize_WhenAboveMaximum_IsCappedAtOneHundred()
        {
            var request = new PageRequest(page: 1, pageSize: 101);

            Assert.Equal(100, request.PageSize);
        }

        [Fact]
        public void Skip_OnLaterPage_UsesPageAndPageSize()
        {
            var request = new PageRequest(page: 3, pageSize: 25);

            Assert.Equal(50, request.Skip);
            Assert.Equal(25, request.Take);
        }
    }
}
