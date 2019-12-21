using System.Collections.Generic;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    [XmlRoot("DvinApps")]
    public class DvinApps : List<DvinApp>, ISecretResult<DvinApps> {
        public DvinApps Clone() {
            var clone = new DvinApps();
            clone.AddRange(this);
            return clone;
        }

        public void ResolveFolders(IFolderResolver folderResolver, IErrorsAndInfos errorsAndInfos) {
            ForEach(dvinApp => dvinApp.ResolveFolders(folderResolver, errorsAndInfos));
        }
    }
}
