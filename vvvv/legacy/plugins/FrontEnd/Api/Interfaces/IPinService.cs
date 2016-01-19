using WampSharp.V2.Rpc;
using Iris.Core.Types;

namespace Iris.FrontEnd.Api.Interfaces
{
    public interface IPinService
    {
        [WampProcedure(DataUri.PinUpdate)]
        void UpdatePin(string sid, PinData pin); 
    }
}