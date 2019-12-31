using HueController;
using System.Threading.Tasks;

namespace FluxService
{
    public class FluxServiceContract : IFluxServiceContract
    {
        /// <summary>
        /// Hue instance.
        /// </summary>
        private readonly Hue hue = new Hue();
        
        /// <summary>
        /// Retrieval of Hue Status info.
        /// </summary>
        /// <returns></returns>
        public HueStatus Get()
        {
            return hue.GetStatus();
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
