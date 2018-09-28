using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    public class SecretDvinApps : ISecret<DvinApps> {
        private DvinApps vDefaultValue;
        public DvinApps DefaultValue => vDefaultValue ?? (vDefaultValue = new DvinApps {
            new DvinApp {
                Id = "GraspNetCore",
                Description = "This is a nonsense entry with the sole purpose of providing a valid DvinApps secret",
                Port = 50114,
                Executable = "Aspenlaub.Net.GraspNetCore.exe",
                DvinAppFolders = new List<DvinAppFolder> {
                    new DvinAppFolder { MachineId = "AlicesMachine", SolutionFolder = @"D:\Users\Alice\GraspNetCore", PublishFolder = @"D:\Users\Alice\GraspNetCoreBin\Publish" },
                    new DvinAppFolder { MachineId = "BobsMachine", SolutionFolder = @"C:\Users\Bob\GraspNetCore", PublishFolder = @"C:\Users\Bob\GraspNetCoreBin\Publish" },
                }
            }
        });

        public string Guid => "4C40BA08-3ED6-4019-BD2C-33390E0EEF74";
    }
}
