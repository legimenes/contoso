namespace Tests;

public sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    public override DateTimeOffset GetUtcNow()
    {
        return UtcNow;
    }

    public void Advance(TimeSpan duration)
    {
        UtcNow = UtcNow.Add(duration);
    }

    public void Reset(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }
}