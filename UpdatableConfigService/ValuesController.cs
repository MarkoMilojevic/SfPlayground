using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace UpdatableConfigService
{
    [Route("api/values")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly SingletonService singletonService;
        private readonly FabricPropertyConfigSection properties;
        private readonly MyConfigSection config;

        public ValuesController(
            SingletonService singletonService,
            IOptionsSnapshot<MyConfigSection> config,
            IOptionsSnapshot<FabricPropertyConfigSection> properties)
        {
            this.config = config.Value;
            this.singletonService = singletonService;
            this.properties = properties.Value;
        }

        [HttpGet]
        public IActionResult Get()
        {
            string result =
                $"SingletonService.SingletonParameter: {singletonService.SingletonParameter}" +
                $"{Environment.NewLine}" +
                $"MyConfigSection.MyParameter: {config.MyParameter}" +
                $"{Environment.NewLine}" +
                $"FabricProperty.CustomProperty: {properties.CustomProperty}";

            return Ok(result);
        }
    }
}