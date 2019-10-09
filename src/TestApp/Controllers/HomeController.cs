using System;
using System.Net;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Attributes;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Components;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Dvin.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.TestApp.Controllers {
    [DvinExceptionFilter]
    public class HomeController : Controller {
        protected readonly IDvinRepository DvinRepository;

        public HomeController(IDvinRepository dvinRepository) {
            DvinRepository = dvinRepository;
        }

        public IActionResult Index() {
            return Ok("Hello World says your dvin app");
        }

        [HttpGet, Route("/Publish")]
        public async Task<IActionResult> Publish() {
            var errorsAndInfos = new ErrorsAndInfos();
            var dvinApp = await DvinRepository.LoadAsync(Constants.DvinSampleAppId, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                return StatusCode((int) HttpStatusCode.InternalServerError, errorsAndInfos.ErrorsToString());
            }

            var fileSystemService = new FileSystemService();
            dvinApp.Publish(fileSystemService, true, errorsAndInfos);
            return errorsAndInfos.AnyErrors()
                ? StatusCode((int)HttpStatusCode.InternalServerError, errorsAndInfos.ErrorsToString())
                : Ok("Your dvin app just published itself");
        }

        public IActionResult Crash() {
            throw new NotImplementedException("This is a deliberate crash");
        }
    }
}