using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Repositories;

public class DvinRepository(IFolderResolver folderResolver, ISecretRepository secretRepository) : IDvinRepository {
    protected readonly IFolderResolver FolderResolver = folderResolver;
    protected readonly ISecretRepository SecretRepository = secretRepository;

    public async Task<IList<DvinApp>> LoadAsync(IErrorsAndInfos errorsAndInfos) {
        return await LoadAsync(true, errorsAndInfos);
    }

    private async Task<IList<DvinApp>> LoadAsync(bool resolve, IErrorsAndInfos errorsAndInfos) {
        var dvinAppsSecret = new SecretDvinApps();
        DvinApps secretDvinApps = await SecretRepository.GetAsync(dvinAppsSecret, errorsAndInfos);
        if (!resolve || errorsAndInfos.AnyErrors()) { return secretDvinApps; }

        await secretDvinApps.ResolveFoldersAsync(FolderResolver, errorsAndInfos);
        return secretDvinApps;
    }

    public async Task<DvinApp> LoadAsync(string id, IErrorsAndInfos errorsAndInfos) {
        IList<DvinApp> dvinApps = await LoadAsync(false, errorsAndInfos);
        DvinApp dvinApp = dvinApps.FirstOrDefault(d => d.Id == id);
        if (dvinApp != null) {
            await dvinApp.ResolveFoldersAsync(FolderResolver, errorsAndInfos);
        }
        return dvinApp;
    }
}