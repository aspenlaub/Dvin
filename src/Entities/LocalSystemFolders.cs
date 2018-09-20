using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    public class LocalSystemFolders : List<LocalSystemFolder>, ISecretResult<LocalSystemFolders> {
        public LocalSystemFolders Clone() {
            var clone = new LocalSystemFolders();
            clone.AddRange(this);
            return clone;
        }
    }
}
