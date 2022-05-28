using System;
using System.Collections.Generic;
using System.IO;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;

public interface IFileSystemService {
    IList<string> ListFilesInDirectory(IFolder folder, string pattern, SearchOption searchOption);
    string ReadAllText(string fileName);
    DateTime LastWriteTime(string fileName);
    bool FolderExists(string folderName);
    void DeleteFile(string fileName);
    void CopyFile(string fromFileName, string toFileName);
    void MoveFile(string fromFileName, string toFileName);
}