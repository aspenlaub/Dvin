using Aspenlaub.Net.GitHub.CSharp.Dvin.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.TestApp.Controllers {
    [DvinExceptionFilter]
    public class HomeController : Controller {
        public IActionResult Index() {
            return Ok("Hello World");
        }
    }
}
