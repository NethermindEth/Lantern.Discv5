using Lantern.Discv5.Enr;

namespace Lantern.Discv5.WireProtocol.Table;

public class TableOptions
{
    public int PingIntervalMilliseconds { get; }
    public int RefreshIntervalMilliseconds { get; }
    public int LookupIntervalMilliseconds { get; }
    public int MaxAllowedFailures { get; }
    public int LookupConcurrency { get; }
    public int MaxReplacementCacheSize { get; }
    public EnrRecord[] BootstrapEnrs { get; }

    private TableOptions(Builder builder)
    {
        PingIntervalMilliseconds = builder.PingIntervalMilliseconds;
        RefreshIntervalMilliseconds = builder.RefreshIntervalMilliseconds;
        LookupIntervalMilliseconds = builder.LookupIntervalMilliseconds;
        MaxAllowedFailures = builder.MaxAllowedFailures;
        LookupConcurrency = builder.LookupConcurrency;
        MaxReplacementCacheSize = builder.MaxReplacementCacheSize;
        BootstrapEnrs = builder.BootstrapEnrs;
    }

    public class Builder
    {
        public int PingIntervalMilliseconds { get; private set; } = 300000;
        public int RefreshIntervalMilliseconds { get; private set; } = 5000;
        public int LookupIntervalMilliseconds { get; private set; } = 3000;
        public int MaxAllowedFailures { get; private set; } = 3;
        public int LookupConcurrency { get; private set; } = 3;
        public int MaxReplacementCacheSize { get; private set; } = 3;
        public EnrRecord[] BootstrapEnrs { get; private set; } = Array.Empty<EnrRecord>();
        
        public Builder WithPingIntervalMilliseconds(int pingIntervalMilliseconds)
        {
            PingIntervalMilliseconds = pingIntervalMilliseconds;
            return this;
        }
        
        public Builder WithRefreshIntervalMilliseconds(int refreshIntervalMilliseconds)
        {
            RefreshIntervalMilliseconds = refreshIntervalMilliseconds;
            return this;
        }

        public Builder WithLookupIntervalMilliseconds(int lookupIntervalMilliseconds)
        {
            LookupIntervalMilliseconds = lookupIntervalMilliseconds;
            return this;
        }
        
        public Builder WithMaxAllowedFailures(int maxAllowedFailures)
        {
            MaxAllowedFailures = maxAllowedFailures;
            return this;
        }
        
        public Builder WithLookupConcurrency(int lookupConcurrency)
        {
            LookupConcurrency = lookupConcurrency;
            return this;
        }

        public Builder WithMaxReplacementCacheSize(int maxReplacementCacheSize)
        {
            MaxReplacementCacheSize = maxReplacementCacheSize;
            return this;
        }
        
        public Builder WithBootstrapEnrs(IEnumerable<EnrRecord> bootstrapEnrs)
        {
            BootstrapEnrs = bootstrapEnrs.ToArray();
            return this;
        }

        public TableOptions Build()
        {
            return new TableOptions(this);
        }
    }
}