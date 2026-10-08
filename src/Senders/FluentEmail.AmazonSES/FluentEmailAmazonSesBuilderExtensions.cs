using FluentEmail.AmazonSES;
using FluentEmail.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FluentEmailAmazonSesBuilderExtensions
    {
        public static FluentEmailServicesBuilder AddAmazonSesSender(this FluentEmailServicesBuilder builder, SesSenderOptions options)
        {
            builder.Services.TryAdd(ServiceDescriptor.Singleton<ISender>(_ => new SesSender(options)));
            return builder;
        }

        public static FluentEmailServicesBuilder AddAmazonSesSender(
            this FluentEmailServicesBuilder builder,
            string region,
            string accessKey = null,
            string secretKey = null,
            string configurationSetName = null)
        {
            return builder.AddAmazonSesSender(new SesSenderOptions
            {
                Region = region,
                AccessKey = accessKey,
                SecretKey = secretKey,
                ConfigurationSetName = configurationSetName
            });
        }
    }
}
