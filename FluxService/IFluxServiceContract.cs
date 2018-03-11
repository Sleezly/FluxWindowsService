using HueController;
using System.ServiceModel;

namespace FluxService
{
    [ServiceContract]
    public interface IFluxServiceContract
    {
        [OperationContract]
        HueDetails Get();

        [OperationContract]
        bool Post(bool On);
    }
}
