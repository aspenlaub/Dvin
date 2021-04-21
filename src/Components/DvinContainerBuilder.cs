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

        public static IServiceCollection UseDvinAndPegh(this IServiceCollection services, ICsArgumentPrompter csArgumentPrompter) {
            services.UsePegh(csArgumentPrompter);
            services.AddTransient<IDvinRepository, DvinRepository>();
            return services;
        }
    }
}
