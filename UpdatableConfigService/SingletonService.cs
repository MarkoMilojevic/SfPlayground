using Microsoft.Extensions.Options;

namespace UpdatableConfigService
{
    public class SingletonService
    {
        private readonly IOptionsMonitor<SingletonServiceConfigSection> config;

        public SingletonService(IOptionsMonitor<SingletonServiceConfigSection> config)
        {
            this.config = config;
        }

        public string SingletonParameter => this.config.CurrentValue.SingletonParameter;
    }
}
