using System;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Components;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Helpers {
    public class SecretsHelper {
        public static void UpdateSecrets() {
            var secretLocalSystemFolders = new SecretLocalSystemFolders();
            var repository = new SecretRepository(new ComponentProvider());
            var errorsAndInfos = new ErrorsAndInfos();
            var localSystemFolders = repository.GetAsync(secretLocalSystemFolders, errorsAndInfos).Result;
            if (errorsAndInfos.AnyErrors()) {
                throw new Exception(string.Join("\r\n", errorsAndInfos.Errors));
            }

            var localSystemFolder = localSystemFolders.FirstOrDefault(f => f.MachineId.Equals(Environment.MachineName, StringComparison.InvariantCultureIgnoreCase));
            if (localSystemFolder == null) { return; }

            var mySecretsFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Aspenlaub.Net\SecretRepository";
            if (mySecretsFolder == localSystemFolder.SecretsFolder) {
                return;
            }

            foreach (var sourceFile in Directory.GetFiles(localSystemFolder.SecretsFolder, "*.xml")) {
                var destFile = mySecretsFolder + sourceFile.Substring(localSystemFolder.SecretsFolder.Length);
                if (!File.Exists(destFile) || File.ReadAllText(sourceFile) != File.ReadAllText(destFile)) {
                    File.Copy(sourceFile, destFile, true);
                }
            }
        }
    }
}
