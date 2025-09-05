using AzKview.Core.Services;
using Xunit;

namespace AzKview.Core.Tests;

public class TimeServiceTests
{
    [Fact]
    public void UtcNow_IsCloseTo_SystemUtcNow()
    {
        var svc = new TimeService();
        var before = DateTime.UtcNow.AddSeconds(-1);
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(svc.UtcNow, before, after);
    }
}
