using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;

[XmlRoot("DvinApps")]
public class DvinApps : List<DvinApp>, ISecretResult<DvinApps> {
    public DvinApps Clone() {
        var clone = new DvinApps();
        clone.AddRange(this);
        return clone;
    }

    public async Task ResolveFoldersAsync(IFolderResolver folderResolver, IErrorsAndInfos errorsAndInfos) {
        foreach (var dvinApp in this) {
            await dvinApp.ResolveFoldersAsync(folderResolver, errorsAndInfos);
        }
    }
}