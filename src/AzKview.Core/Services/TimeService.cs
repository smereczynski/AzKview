namespace AzKview.Core.Services;

public interface ITimeService
{
    DateTime UtcNow { get; }
}

public sealed class TimeService : ITimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
