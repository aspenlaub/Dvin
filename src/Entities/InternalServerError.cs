using System.Net;
// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;

public class InternalServerError {
    public HttpStatusCode StatusCode { get; set; }
    public string Message { get; set; }

    public static InternalServerError Create(string message) {
        return new() {
            StatusCode = HttpStatusCode.InternalServerError,
            Message = message
        };
    }
}