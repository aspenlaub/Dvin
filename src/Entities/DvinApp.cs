using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    public class DvinApp : IDvinApp {
        [Key, XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("DvinAppFolder")]
        public List<DvinAppFolder> DvinAppFolders { get; set; } = new List<DvinAppFolder>();

        [XmlAttribute("executable")]
        public string Executable { get; set; }

        [XmlAttribute("port")]
        public int Port { get; set; }
    }
}
