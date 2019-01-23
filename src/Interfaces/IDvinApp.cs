// ReSharper disable UnusedMember.Global
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces {
    public interface IDvinApp {
        string Id { get; set; }
        string Description { get; set; }
        string Executable { get; set; }
        int Port { get; set; }
        string SolutionFolder { get; set; }
        string ReleaseFolder { get; set; }
        string PublishFolder { get; set; }
        string ExceptionLogFolder { get; set; }

        void ResolveFolders(IComponentProvider componentProvider, IErrorsAndInfos errorsAndInfos);
    }
}