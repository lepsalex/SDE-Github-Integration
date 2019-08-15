using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Octokit;
using Serilog;

namespace GithubIntegration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HooksController : ControllerBase
    {
        Hooks hooks = new Hooks();

        // POST api/hooks
        [HttpPost]
        public void ReceiveWebHook([FromBody] string value)
        {
            var payload = JsonConvert.DeserializeObject<IssueEventPayload>(value);
            hooks.Process(payload);
        }
    }
}
