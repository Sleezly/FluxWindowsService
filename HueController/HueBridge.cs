using Q42.HueApi;

namespace HueController
{
    public class HueBridge
    {
        public HueClient Client { get; set; }

        public string BridgeId { get; set; }

        public string IpAddress { get; set; }
    }
}
