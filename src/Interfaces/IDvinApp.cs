using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces {
    public interface IDvinApp {
        string Id { get; set; }
        // ReSharper disable once UnusedMember.Global
        string Description { get; set; }
        List<DvinAppFolder> DvinAppFolders { get; set; }
        string Executable { get; set; }
        int Port { get; set; }
    }
}