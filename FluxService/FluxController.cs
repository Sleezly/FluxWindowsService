using Newtonsoft.Json;
using System.Net.Http;
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
            HttpResponseMessage response = new HttpResponseMessage
            {
                //Content = new ObjectContent<IDictionary<string, string>>(fluxServiceContract.Get(), new JsonMediaTypeFormatter(), "application/json")
                Content = new StringContent(JsonConvert.SerializeObject(fluxServiceContract.Get()))
            };

            return response;
        }

        [HttpPost]
        [Route("api/flux")]
        public async Task<IHttpActionResult> Post()
        {
            try
            {
                string request = await Request.Content.ReadAsStringAsync();
                HuePost huePost = JsonConvert.DeserializeObject<HuePost>(request);

                await fluxServiceContract.Post(huePost.On.Equals("on", System.StringComparison.OrdinalIgnoreCase), huePost.LightLevel);

                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}
