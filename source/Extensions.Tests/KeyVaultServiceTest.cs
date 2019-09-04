using Extensions.Common.KeyVault.Domain;
using Extensions.Common.KeyVault.Services;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.IO;
using Xunit;

namespace Extensions.Tests
{
    public class KeyVaultServiceTest : IDisposable
    {
        private string EnvironmentName { get; set; } = nameof(KeyVaultServiceTest);
        private string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        private KeyVaultConfig configuration = new KeyVaultConfig()
        {
            CertificateName = "certificate_name",
            ClientId = "client_id",
            ClientSecret = "client_secret",
            Identifier = "identifier"
        };

        public KeyVaultServiceTest()
        {
            dynamic dynamicConfiguration = new ExpandoObject();
            dynamicConfiguration.KeyVaultConfig = configuration;
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
            IKeyVaultService keyVaultService = new KeyVaultService(EnvironmentName, ContentRootPath);
            KeyVaultConfig config = keyVaultService.GetConfiguration();

            // assert
            Assert.Equal(configuration.CertificateName, config.CertificateName);
            Assert.Equal(configuration.ClientId, config.ClientId);
            Assert.Equal(configuration.ClientSecret, config.ClientSecret);
            Assert.Equal(configuration.Identifier, config.Identifier);
        }
    }
}