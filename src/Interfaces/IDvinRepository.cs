using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces {
    public interface IDvinRepository {
        Task<IList<DvinApp>> LoadAsync();
        Task<DvinApp> LoadAsync(string id);
        Task<LocalSystemFolder> LoadFolderAsync();
        string MySecretRepositoryFolder();
    }
}
