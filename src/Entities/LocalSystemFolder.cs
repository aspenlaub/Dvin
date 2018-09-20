using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    public class LocalSystemFolder {
        [XmlAttribute("machineid")]
        public string MachineId { get; set; }

        [XmlAttribute("secretsfolder")]
        public string SecretsFolder { get; set; }
    }
}
