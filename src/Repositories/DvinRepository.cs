using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Components;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories {
    public class DvinRepository : IDvinRepository {
        public async Task<IList<DvinApp>> LoadAsync() {
            var dvinAppsSecret = new SecretDvinApps();
            var repository = new SecretRepository(new ComponentProvider());
            var errorsAndInfos = new ErrorsAndInfos();
            var secretDvinApps = await repository.GetAsync(dvinAppsSecret, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                throw new Exception("Could not load dvin apps");
            }

            return secretDvinApps;
        }

        public async Task<DvinApp> LoadAsync(string id) {
            var dvinApps = await LoadAsync();
            var dvinApp = dvinApps.FirstOrDefault(d => d.Id == id);
            if (dvinApp == null) {
                throw new Exception($"Could not load dvin app {id}");
            }

            return dvinApp;
        }
    }
}
