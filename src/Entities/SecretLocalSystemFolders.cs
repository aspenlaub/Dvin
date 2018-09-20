using System;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    public class SecretLocalSystemFolders : ISecret<LocalSystemFolders> {
        private LocalSystemFolders vDefaultValue;
        public LocalSystemFolders DefaultValue => vDefaultValue ?? (vDefaultValue = new LocalSystemFolders {
            new LocalSystemFolder {
                MachineId = Environment.MachineName,
                SecretsFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Aspenlaub.Net\SecretRepository"
            }
        });

        public string Guid => "CD8D46E1-140F-4F41-BDB1-A2159ABE59F6";
    }
}