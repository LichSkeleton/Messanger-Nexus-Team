namespace NexusTeam.Shared.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using NexusTeam.Shared.Helpers;
    using NexusTeam.Shared.ValueObjects;
    using Xunit;

    public class PaginationHelperTests
    {
        [Fact]
        public void CreateResponse_WithPartialLastPage_RoundsTotalPagesUp()
        {
            var request = new PageRequest(page: 2, pageSize: 20);
            var items = new List<string> { "message-21" };

            var result = PaginationHelper.CreateResponse(items, totalCount: 21, request);

            Assert.Same(items, result.Items);
            Assert.Equal(2, result.Page);
            Assert.Equal(20, result.PageSize);
            Assert.Equal(21, result.TotalCount);
            Assert.Equal(2, result.TotalPages);
            Assert.True(result.HasPreviousPage);
            Assert.False(result.HasNextPage);
        }

        [Fact]
        public void CreateResponse_WithNullItems_ReturnsEmptyItemsCollection()
        {
            var request = new PageRequest();

            var result = PaginationHelper.CreateResponse<string>(null!, totalCount: 0, request);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalPages);
            Assert.False(result.HasPreviousPage);
            Assert.False(result.HasNextPage);
        }

        [Fact]
        public void CreateResponse_OnMiddlePage_ReportsBothDirections()
        {
            var request = new PageRequest(page: 2, pageSize: 10);

            var result = PaginationHelper.CreateResponse(
                new List<int> { 11, 12 },
                totalCount: 30,
                request);

            Assert.Equal(3, result.TotalPages);
            Assert.True(result.HasPreviousPage);
            Assert.True(result.HasNextPage);
        }

        [Fact]
        public void CreateResponse_WithNullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PaginationHelper.CreateResponse(new List<int>(), totalCount: 0, null!));
        }
    }
}
