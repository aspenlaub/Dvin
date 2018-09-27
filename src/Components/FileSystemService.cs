using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Extensions;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Components {
    public class FileSystemService : IFileSystemService {
        public IList<string> ListFilesInDirectory(IFolder folder, string pattern, SearchOption searchOption) {
            return folder.Exists() ? Directory.GetFiles(folder.FullName, pattern, searchOption).ToList() : new List<string>();
        }

        public string ReadAllText(string fileName) {
            return File.ReadAllText(fileName);
        }
    }
}
