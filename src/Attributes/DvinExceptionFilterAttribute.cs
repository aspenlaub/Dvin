using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Attributes {
    /// <summary>
    /// Usage example: .AddMvc(config => config.Filters.Add(new DvinExceptionFilterAttribute()))
    /// </summary>
    public class DvinExceptionFilterAttribute : ExceptionFilterAttribute {
        protected static IFolder ExceptionLogFolder;

        public static void SetExceptionLogFolder(IFolder exceptionLogFolder) {
            ExceptionLogFolder = exceptionLogFolder;
        }

        public override void OnException(ExceptionContext context) {
            ExceptionSaver.SaveUnhandledException(ExceptionLogFolder, context.Exception, nameof(Dvin), e => { });
            context.Result = new JsonResult(InternalServerError.Create("An exception was logged. We are sorry for the inconvenience."));
        }
    }
}
