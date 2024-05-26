using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.AspNetCore.Configuration;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace UpdatableConfigService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class UpdatableConfigService : StatefulService
    {
        public UpdatableConfigService(StatefulServiceContext context)
            : base(context)
        {
            // Empty
        }

        /// <inheritdoc/>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener>
            {
                new ServiceReplicaListener(
                    serviceContext =>
                    new KestrelCommunicationListener(
                        serviceContext,
                        "DemoConfigEndpoint",
                        (string url, AspNetCoreCommunicationListener listener) =>
                        {
                            WebHostBuilder builder = new WebHostBuilder();

                            builder
                                .UseKestrel()
                                .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                .UseUrls(url)
                                .UseContentRoot(Directory.GetCurrentDirectory())
                                .ConfigureAppConfiguration(
                                    (WebHostBuilderContext webHostBuilderContext, IConfigurationBuilder configBuilder) =>
                                    {
                                        configBuilder.AddServiceFabricConfiguration();
                                    })
                                .ConfigureLogging(
                                    (WebHostBuilderContext webHostBuilderContext, ILoggingBuilder loggingBuilder) =>
                                    {
                                    })
                                .ConfigureServices(
                                    (WebHostBuilderContext webHostBuilderContext, IServiceCollection services) =>
                                    {
                                        services.AddMvc();

                                        services.Configure<MyConfigSection>(
                                            webHostBuilderContext
                                                .Configuration
                                                .GetSection("Config:MyConfigSection"));

                                        services.Configure<SingletonServiceConfigSection>(
                                            webHostBuilderContext
                                                .Configuration
                                                .GetSection("Config:SingletonServiceConfigSection"));

                                        services
                                            .AddOptions<FabricPropertyConfigSection>()
                                            .Configure(
                                                (FabricPropertyConfigSection fabricPropertyConfigSection, FabricClient fabricClient) =>
                                                {
                                                    fabricPropertyConfigSection.CustomProperty =
                                                        ReadFabricProperty(
                                                            fabricPropertyConfigSection,
                                                            fabricClient,
                                                            "CustomProperty");
                                                });

                                        services
                                            .AddSingleton(new FabricClient())
                                            .AddSingleton<SingletonService>();
                                    })
                                .Configure(
                                    (IApplicationBuilder appBuilder) =>
                                    {
                                        appBuilder.UseMvc();
                                    });

                            IWebHost host = builder.Build();

                            return host;
                        })),
            };
        }

        private static string ReadFabricProperty(
            FabricPropertyConfigSection fabricPropertyConfigSection,
            FabricClient fabricClient,
            string propertyName)
        {
            fabricPropertyConfigSection.CustomProperty = "Default";

            try
            {

                NamedProperty np =
                    fabricClient
                        .PropertyManager
                        .GetPropertyAsync(
                            new Uri("fabric:/DemoLiveConfigApp"),
                            "CustomProperty")
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();

                byte[] valueBytes =
                    np.Metadata.ValueSize > 0
                        ? np.GetValue<byte[]>()
                        : Array.Empty<byte>();

                return Encoding.UTF8.GetString(valueBytes);
            }
            catch (FabricElementNotFoundException)
            {
                return "Property not found";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
    }
}
