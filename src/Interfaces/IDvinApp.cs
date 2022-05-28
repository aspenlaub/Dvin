// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;

public interface IDvinApp {
    string Id { get; set; }
    string Description { get; set; }
    string Executable { get; set; }
    int Port { get; set; }
    bool AreFoldersBeingResolved { get; set; }
    string SolutionFolder { get; set; }
    string ReleaseFolder { get; set; }
    string PublishFolder { get; set; }
    string ExceptionLogFolder { get; set; }
}