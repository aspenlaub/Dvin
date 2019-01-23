using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories {
    public class DvinRepository : IDvinRepository {
        private readonly IComponentProvider vComponentProvider;

        public DvinRepository(IComponentProvider componentProvider) {
            vComponentProvider = componentProvider;
        }

        public async Task<IList<DvinApp>> LoadAsync(IErrorsAndInfos errorsAndInfos) {
            return await LoadAsync(true, errorsAndInfos);
        }

        private async Task<IList<DvinApp>> LoadAsync(bool resolve, IErrorsAndInfos errorsAndInfos) {
            var dvinAppsSecret = new SecretDvinApps();
            var repository = new SecretRepository(new ComponentProvider());
            var secretDvinApps = await repository.GetAsync(dvinAppsSecret, errorsAndInfos);
            if (!resolve || errorsAndInfos.AnyErrors()) { return secretDvinApps; }

            secretDvinApps.ResolveFolders(vComponentProvider, errorsAndInfos);
            return secretDvinApps;
        }

        public async Task<DvinApp> LoadAsync(string id, IErrorsAndInfos errorsAndInfos) {
            var dvinApps = await LoadAsync(false, errorsAndInfos);
            var dvinApp = dvinApps.FirstOrDefault(d => d.Id == id);
            dvinApp?.ResolveFolders(vComponentProvider, errorsAndInfos);
            return dvinApp;
        }
    }
}
