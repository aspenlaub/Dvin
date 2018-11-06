using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Components {
    public class FileSystemService : IFileSystemService {
        public IList<string> ListFilesInDirectory(IFolder folder, string pattern, SearchOption searchOption) {
            return folder.Exists() ? Directory.GetFiles(folder.FullName, pattern, searchOption).ToList() : new List<string>();
        }

        public string ReadAllText(string fileName) {
            return File.ReadAllText(fileName);
        }

        public DateTime LastWriteTime(string fileName) {
            return File.GetLastWriteTime(fileName);
        }

        public bool FolderExists(string folderName) {
            return Directory.Exists(folderName);
        }

        public void DeleteFile(string fileName) {
            File.Delete(fileName);
        }

        public void CopyFile(string fromFileName, string toFileName) {
            File.Copy(fromFileName, toFileName);
        }

        public void MoveFile(string fromFileName, string toFileName) {
            File.Move(fromFileName, toFileName);
        }
    }
}
