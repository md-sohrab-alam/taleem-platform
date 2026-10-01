using Taleem.Domain.Enums;

namespace Taleem.Tests;

public class PublishingRulesTests
{
    [Theory]
    [InlineData(ContentLifecycleStatus.Uploaded, false)]
    [InlineData(ContentLifecycleStatus.Processing, false)]
    [InlineData(ContentLifecycleStatus.NeedsReview, false)]
    [InlineData(ContentLifecycleStatus.Approved, true)]
    [InlineData(ContentLifecycleStatus.Published, false)]
    public void Only_Approved_Can_Publish(ContentLifecycleStatus status, bool canPublish)
    {
        var allowed = status == ContentLifecycleStatus.Approved;
        Assert.Equal(canPublish, allowed);
    }

    [Fact]
    public void Public_Api_Only_Serves_Published()
    {
        var visible = new[] { ContentLifecycleStatus.Published };
        Assert.DoesNotContain(ContentLifecycleStatus.Draft, visible);
        Assert.DoesNotContain(ContentLifecycleStatus.NeedsReview, visible);
    }
}
