using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Components {
    public static class DvinContainerBuilder {
        public static ContainerBuilder RegisterForDvin(this ContainerBuilder builder) {
            builder.RegisterType<DvinRepository>().As<IDvinRepository>();
            return builder;
        }
    }
}
