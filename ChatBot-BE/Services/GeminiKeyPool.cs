namespace ChatBot_BE.Services
{
    public sealed record GeminiKeyLease(int Id, string ApiKey);

    public sealed class GeminiKeyPool
    {
        private readonly object _sync = new();
        private readonly KeyState[] _keys;
        private readonly TimeProvider _timeProvider;
        private int _nextIndex;

        public GeminiKeyPool(GeminiOptions options, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(timeProvider);

            _keys = options.ApiKeys
                .Select((key, index) => new KeyState(index + 1, key))
                .ToArray();
            _timeProvider = timeProvider;
            DefaultCooldown = options.Cooldown;
        }

        public int Count => _keys.Length;
        public TimeSpan DefaultCooldown { get; }

        public GeminiKeyLease? TryAcquire(ISet<int>? excludedKeyIds = null)
        {
            lock (_sync)
            {
                var now = _timeProvider.GetUtcNow();

                for (var checkedCount = 0; checkedCount < _keys.Length; checkedCount++)
                {
                    var index = (_nextIndex + checkedCount) % _keys.Length;
                    var key = _keys[index];

                    if (key.Disabled || key.CooldownUntil > now || excludedKeyIds?.Contains(key.Id) == true)
                    {
                        continue;
                    }

                    _nextIndex = (index + 1) % _keys.Length;
                    return new GeminiKeyLease(key.Id, key.ApiKey);
                }

                return null;
            }
        }

        public void PutOnCooldown(int keyId, TimeSpan duration)
        {
            lock (_sync)
            {
                var key = GetKey(keyId);
                var cooldown = duration < DefaultCooldown ? DefaultCooldown : duration;
                key.CooldownUntil = _timeProvider.GetUtcNow().Add(cooldown);
            }
        }

        public void Disable(int keyId)
        {
            lock (_sync)
            {
                GetKey(keyId).Disabled = true;
            }
        }

        public TimeSpan GetRetryAfter()
        {
            lock (_sync)
            {
                var now = _timeProvider.GetUtcNow();
                var earliest = _keys
                    .Where(key => !key.Disabled && key.CooldownUntil > now)
                    .Select(key => key.CooldownUntil)
                    .DefaultIfEmpty(now.Add(DefaultCooldown))
                    .Min();

                var retryAfter = earliest - now;
                return retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(1);
            }
        }

        private KeyState GetKey(int keyId) =>
            _keys.First(key => key.Id == keyId);

        private sealed class KeyState
        {
            public KeyState(int id, string apiKey)
            {
                Id = id;
                ApiKey = apiKey;
            }

            public int Id { get; }
            public string ApiKey { get; }
            public DateTimeOffset CooldownUntil { get; set; }
            public bool Disabled { get; set; }
        }
    }
}
