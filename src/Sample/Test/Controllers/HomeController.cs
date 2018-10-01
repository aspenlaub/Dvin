using Aspenlaub.Net.GitHub.CSharp.Dvin.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Sample.Test.Controllers {
    [DvinExceptionFilter]
    public class HomeController : Controller {
        public IActionResult Index() {
            return View();
        }
    }
}
