using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    public class DvinAppFolder {
        [XmlAttribute("machineid")]
        public string MachineId { get; set; }

        [XmlAttribute("solutionfolder")]
        public string SolutionFolder { get; set; }

        [XmlAttribute("releasefolder")]
        public string ReleaseFolder { get; set; }

        [XmlAttribute("publishfolder")]
        public string PublishFolder { get; set; }

        [XmlAttribute("exceptionlogfolder")]
        public string ExceptionLogFolder { get; set; }
    }
}
