using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;

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

    private string _SolutionFolder;
    [XmlElement("solutionfolder")]
    public string SolutionFolder {
        get => ReturnFolderThrowExceptionIfUnresolved(_SolutionFolder);
        set => _SolutionFolder = value;
    }

    private string _ReleaseFolder;
    [XmlElement("releasefolder")]
    public string ReleaseFolder {
        get => ReturnFolderThrowExceptionIfUnresolved(_ReleaseFolder);
        set => _ReleaseFolder = value;
    }

    private string _PublishFolder;
    [XmlElement("publishfolder")]
    public string PublishFolder {
        get => ReturnFolderThrowExceptionIfUnresolved(_PublishFolder);
        set => _PublishFolder = value;
    }

    private string _ExceptionLogFolder;
    [XmlElement("exceptionlogfolder")]
    public string ExceptionLogFolder {
        get => ReturnFolderThrowExceptionIfUnresolved(_ExceptionLogFolder);
        set => _ExceptionLogFolder = value;
    }

    private string ReturnFolderThrowExceptionIfUnresolved(string folder) {
        if (AreFoldersBeingResolved || !folder.Contains("$")) {
            return folder;
        }

        throw new Exception($"{nameof(DvinApp)} folders have not resolved, please use the {nameof(IDvinApp)} extension method");
    }
}