using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Components {
    public static class DvinContainerBuilder {
        public static ContainerBuilder UseDvinAndPegh(this ContainerBuilder builder, ICsArgumentPrompter csArgumentPrompter) {
            builder.UsePegh(csArgumentPrompter);
            builder.RegisterType<DvinRepository>().As<IDvinRepository>();
            return builder;
        }

        // ReSharper disable once UnusedMember.Global
        public static IServiceCollection UseDvinAndPegh(this IServiceCollection services, ICsArgumentPrompter csArgumentPrompter) {
            services.UsePegh(csArgumentPrompter);
            services.AddTransient<IDvinRepository, DvinRepository>();
            return services;
        }

        public static IServiceCollection UsePegh(this IServiceCollection services, ICsArgumentPrompter csArgumentPrompter) {
            services.AddSingleton(csArgumentPrompter);
            services.AddTransient<ICsLambdaCompiler, CsLambdaCompiler>();
            services.AddTransient<IDisguiser, Disguiser>();
            services.AddTransient<IFolderDeleter, FolderDeleter>();
            services.AddTransient<IFolderResolver, FolderResolver>();
            services.AddTransient<IFolderUpdater, FolderUpdater>();
            services.AddTransient<IPassphraseProvider, PassphraseProvider>();
            services.AddTransient<IPeghEnvironment, PeghEnvironment>();
            services.AddTransient<IPrimeNumberGenerator, PrimeNumberGenerator>();
            services.AddTransient<ISecretRepository, SecretRepository>();
            services.AddTransient<IStringCrypter, StringCrypter>();
            services.AddTransient<IXmlDeserializer, XmlDeserializer>();
            services.AddTransient<IXmlSerializer, XmlSerializer>();
            services.AddTransient<IXmlSchemer, XmlSchemer>();
            return services;
        }
    }
}
