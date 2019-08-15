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

        // POST api/hooks
        [HttpPost]
        public void ReceiveWebHook([FromBody] string value)
        {
            // Map JSON string to object
            var payload = JsonConvert.DeserializeObject<IssueEventPayload>(value);

            Log.Information("Action: ", payload.Action);
            Log.Information("Issue: ", payload.Issue);
            Log.Information("Repository: ", payload.Repository);
            Log.Information("Sender: ", payload.Sender);
        }
    }
}
