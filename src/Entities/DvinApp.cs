using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;

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

        [XmlIgnore]
        public bool AreFoldersBeingResolved { get; set; }

        private string vSolutionFolder;
        [XmlElement("solutionfolder")]
        public string SolutionFolder {
            get => ReturnFolderThrowExceptionIfUnresolved(vSolutionFolder);
            set => vSolutionFolder = value;
        }

        private string vReleaseFolder;
        [XmlElement("releasefolder")]
        public string ReleaseFolder {
            get => ReturnFolderThrowExceptionIfUnresolved(vReleaseFolder);
            set => vReleaseFolder = value;
        }

        private string vPublishFolder;
        [XmlElement("publishfolder")]
        public string PublishFolder {
            get => ReturnFolderThrowExceptionIfUnresolved(vPublishFolder);
            set => vPublishFolder = value;
        }

        private string vExceptionLogFolder;
        [XmlElement("exceptionlogfolder")]
        public string ExceptionLogFolder {
            get => ReturnFolderThrowExceptionIfUnresolved(vExceptionLogFolder);
            set => vExceptionLogFolder = value;
        }

        private string ReturnFolderThrowExceptionIfUnresolved(string folder) {
            if (AreFoldersBeingResolved || !folder.Contains("$")) {
                return folder;
            }

            throw new Exception($"{nameof(DvinApp)} folders have not resolved, please use the {nameof(IDvinApp)} extension method");
        }
    }
}
