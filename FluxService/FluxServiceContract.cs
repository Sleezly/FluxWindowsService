using HueController;
using System.Threading.Tasks;

namespace FluxService
{
    public class FluxServiceContract : IFluxServiceContract
    {
        private readonly Hue hue = Hue.GetOrCreate();
        
        /// <summary>
        /// Retrieval of Hue Status info.
        /// </summary>
        /// <returns></returns>
        public HueDetails Get()
        {
            return hue.Status;
        }

        /// <summary>
        /// Makes a request to the Flux service.
        /// </summary>
        /// <param name="On"></param>
        /// <returns></returns>
        public async Task Post(bool on, double lightLevel)
        {
            await hue.MakeRequest(on, lightLevel);
        }
    }
}
