using ChatBot_BE.Services;
using FluentAssertions;

namespace ChatBot_BE.Tests.Services;

public class GeminiKeyPoolTests
{
    [Fact]
    public void TryAcquire_RotatesThroughAvailableKeys()
    {
        var pool = CreatePool(["key-1", "key-2", "key-3"]);

        pool.TryAcquire()!.Id.Should().Be(1);
        pool.TryAcquire()!.Id.Should().Be(2);
        pool.TryAcquire()!.Id.Should().Be(3);
        pool.TryAcquire()!.Id.Should().Be(1);
    }

    [Fact]
    public void TryAcquire_SkipsCoolingKeyUntilCooldownExpires()
    {
        var clock = new ManualTimeProvider();
        var pool = CreatePool(["key-1", "key-2"], clock);

        var first = pool.TryAcquire()!;
        pool.PutOnCooldown(first.Id, TimeSpan.FromSeconds(60));

        pool.TryAcquire()!.Id.Should().Be(2);
        pool.TryAcquire()!.Id.Should().Be(2);

        clock.Advance(TimeSpan.FromSeconds(60));

        pool.TryAcquire()!.Id.Should().Be(1);
    }

    [Fact]
    public void TryAcquire_NeverReturnsDisabledKey()
    {
        var pool = CreatePool(["key-1", "key-2"]);
        var first = pool.TryAcquire()!;

        pool.Disable(first.Id);

        pool.TryAcquire()!.Id.Should().Be(2);
        pool.TryAcquire()!.Id.Should().Be(2);
    }

    [Fact]
    public void PutOnCooldown_EnforcesConfiguredMinimum()
    {
        var clock = new ManualTimeProvider();
        var pool = CreatePool(["key-1"], clock);
        var key = pool.TryAcquire()!;

        pool.PutOnCooldown(key.Id, TimeSpan.FromSeconds(5));

        pool.TryAcquire().Should().BeNull();
        pool.GetRetryAfter().Should().Be(TimeSpan.FromSeconds(60));

        clock.Advance(TimeSpan.FromSeconds(59));
        pool.TryAcquire().Should().BeNull();

        clock.Advance(TimeSpan.FromSeconds(1));
        pool.TryAcquire().Should().NotBeNull();
    }

    private static GeminiKeyPool CreatePool(string[] keys, TimeProvider? timeProvider = null)
    {
        var options = new GeminiOptions
        {
            ApiKeys = keys,
            CooldownSeconds = 60,
            MaxAttempts = 3
        };

        return new GeminiKeyPool(options, timeProvider ?? TimeProvider.System);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
