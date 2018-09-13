using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    public class DvinApp {
        [Key, XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("DvinAppFolder")]
        public List<DvinAppFolder> DvinAppFolders { get; set; } = new List<DvinAppFolder>();

        [XmlAttribute("executable")]
        public string Executable { get; set; }

        [XmlAttribute("releaseport")]
        public int ReleasePort { get; set; }

        [XmlAttribute("debugport")]
        public int DebugPort { get; set; }

        [XmlAttribute("obsolete")]
        public bool Obsolete { get; set; }
    }
}
