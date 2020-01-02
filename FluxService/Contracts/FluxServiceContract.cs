using HueController;
using System.Threading.Tasks;

namespace FluxService
{
    public class FluxServiceContract : IFluxServiceContract
    {
        /// <summary>
        /// Retrieval of Hue Status info.
        /// </summary>
        /// <returns></returns>
        public HueStatus Get()
        {
            return FluxWindowsService.Hue.GetStatus();
        }

        /// <summary>
        /// Makes a request to the Flux service.
        /// </summary>
        /// <param name="On"></param>
        /// <returns></returns>
        public async Task Post(bool on)
        {
            await FluxWindowsService.Hue.Enable(on);
        }

    }
}
