using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    public class SecretDvinApps : ISecret<DvinApps> {
        private DvinApps vDefaultValue;
        public DvinApps DefaultValue => vDefaultValue ?? (vDefaultValue = new DvinApps {
            new DvinApp {
                Id = "GraspNetCore",
                Description = "This is a nonsense entry with the sole purpose of providing a valid DvinApps secret",
                Port = 50114,
                Executable = "Aspenlaub.Net.GraspNetCore.exe",
                SolutionFolder = @"D:\Users\Alice\GraspNetCore",
                ReleaseFolder = @"D:\Users\Alice\GraspNetCoreBin\Release",
                PublishFolder = @"D:\Users\Alice\GraspNetCoreBin\Publish",
                ExceptionLogFolder = @"D:\Temp\Exceptions"
            }
        });

        public string Guid => "4C40BA08-3ED6-4019-BD2C-33390E0EEF74";
    }
}
