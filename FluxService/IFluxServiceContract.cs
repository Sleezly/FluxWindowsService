using HueController;
using System.ServiceModel;
using System.Threading.Tasks;

namespace FluxService
{
    [ServiceContract]
    public interface IFluxServiceContract
    {
        [OperationContract]
        HueStatus Get();

        [OperationContract]
        Task Post(bool On, double LightLevel);
    }
}
