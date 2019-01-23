using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities {
    public class DvinApp : IDvinApp {
        [Key, XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlAttribute("executable")]
        public string Executable { get; set; }

        [XmlAttribute("port")]
        public int Port { get; set; }

        [XmlElement("solutionfolder")]
        public string SolutionFolder { get; set; }

        [XmlElement("releasefolder")]
        public string ReleaseFolder { get; set; }

        [XmlElement("publishfolder")]
        public string PublishFolder { get; set; }

        [XmlElement("exceptionlogfolder")]
        public string ExceptionLogFolder { get; set; }

        public void ResolveFolders(IComponentProvider componentProvider, IErrorsAndInfos errorsAndInfos) {
            SolutionFolder = componentProvider.FolderResolver.Resolve(SolutionFolder, errorsAndInfos).FullName;
            ReleaseFolder = componentProvider.FolderResolver.Resolve(ReleaseFolder, errorsAndInfos).FullName;
            PublishFolder = componentProvider.FolderResolver.Resolve(PublishFolder, errorsAndInfos).FullName;
            ExceptionLogFolder = componentProvider.FolderResolver.Resolve(ExceptionLogFolder, errorsAndInfos).FullName;
        }
    }
}
