using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace FluxService
{
    public class FluxController : ApiController
    {
        private static readonly FluxServiceContract fluxServiceContract = new FluxServiceContract();

        [Route("api/flux")]
        [HttpGet]
        public HttpResponseMessage Get()
        {
            HttpResponseMessage response = new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(fluxServiceContract.Get()))
            };

            return response;
        }
    }
}
