using HueController;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;

namespace FluxService
{
    public class FluxController : ApiController
    {
        private static FluxServiceContract fluxServiceContract = new FluxServiceContract();

        [Route("api/flux")]
        [HttpGet]
        public HttpResponseMessage Get()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ObjectContent<HueDetails>(fluxServiceContract.Get(), new JsonMediaTypeFormatter(), "application/json");

            return response;
        }

        [HttpPost]
        [Route("api/flux")]
        public async Task<IHttpActionResult> Post()
        {
            string request = await Request.Content.ReadAsStringAsync();

            HuePost huePost = JsonConvert.DeserializeObject<HuePost>(request);

            if (fluxServiceContract.Post(huePost.On.Equals("on", System.StringComparison.InvariantCultureIgnoreCase), huePost.LightLevel))
            {
                return Ok();
            }

            return BadRequest();
        }
    }
}
