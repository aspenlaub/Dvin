using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Components;

public static class DvinContainerBuilder {
    public static ContainerBuilder UseDvinAndPegh(this ContainerBuilder builder, string applicationName) {
        builder.UsePegh(applicationName);
        builder.RegisterType<DvinRepository>().As<IDvinRepository>();
        return builder;
    }

    public static IServiceCollection UseDvinAndPegh(this IServiceCollection services, string applicationName) {
        services.UsePegh(applicationName);
        services.AddTransient<IDvinRepository, DvinRepository>();
        return services;
    }
}