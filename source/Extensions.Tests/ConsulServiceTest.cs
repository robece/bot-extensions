using Consul;
using Extensions.Common.Consul.Domain;
using Extensions.Common.Consul.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.IO;
using Xunit;

namespace Extensions.Tests
{
    public class ConsulServiceTest : IDisposable
    {
        private string EnvironmentName { get; set; } = nameof(ConsulServiceTest);
        private string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        private ConsulConfig configuration = new ConsulConfig()
        {
            Address = "http://127.0.0.1",
            ServiceName = "service_name",
            ServiceID = "service_id",
            ServiceTag = "service_tag"
        };

        public ConsulServiceTest()
        {
            dynamic dynamicConfiguration = new ExpandoObject();
            dynamicConfiguration.ConsulConfig = configuration;
            var jsonConfiguration = JsonConvert.SerializeObject(dynamicConfiguration);
            File.WriteAllText(Path.Combine(ContentRootPath, $"appsettings.{EnvironmentName}.json"), jsonConfiguration);
        }

        public void Dispose()
        {
            File.Delete(Path.Combine(ContentRootPath, $"appsettings.{EnvironmentName}.json"));
        }

        [Fact]
        public void GetConfigurationTest()
        {
            // arrage

            // act
            ConsulService consulService = new ConsulService(EnvironmentName, ContentRootPath);
            ConsulConfig config = consulService.GetConfiguration();

            // assert
            Assert.Equal(configuration.Address, config.Address);
            Assert.Equal(configuration.ServiceName, config.ServiceName);
            Assert.Equal(configuration.ServiceID, config.ServiceID);
            Assert.Equal(configuration.ServiceTag, config.ServiceTag);
        }

        [Fact]
        public void InitializeTest()
        {
            // arrage
            var builder = new ConfigurationBuilder()
            .SetBasePath(ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();

            var configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();

            // act
            ConsulService consulService = new ConsulService(EnvironmentName, ContentRootPath);
            consulService.Initialize(services, configuration);

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            IConsulClient service = serviceProvider.GetRequiredService<IConsulClient>();

            // assert
            Assert.NotNull(service);
        }
    }
}