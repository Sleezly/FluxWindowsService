using HueController;

namespace FluxService
{
    public class FluxServiceContract : IFluxServiceContract
    {
        Hue hue = Hue.GetOrCreate();
        
        /// <summary>
        /// Retrieval of Hue Status info.
        /// </summary>
        /// <returns></returns>
        public HueDetails Get()
        {
            return hue.Status;
        }

        /// <summary>
        /// Turn flux service On or Off.
        /// </summary>
        /// <param name="On"></param>
        /// <returns></returns>
        public bool Post(bool On, double LightLevel)
        {
            hue.LightLevel = LightLevel;

            // Ensure current status does not match requested state
            if (hue.Status.On != On)
            {
                if (On)
                {
                    hue.Start();
                }
                else
                {
                    hue.Stop();
                }
            }

            return true;
        }
    }
}
