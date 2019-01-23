using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces {
    public interface IDvinRepository {
        Task<IList<DvinApp>> LoadAsync(IErrorsAndInfos errorsAndInfos);
        Task<DvinApp> LoadAsync(string id, IErrorsAndInfos errorsAndInfos);
    }
}
