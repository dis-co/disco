using WampSharp.V2.Rpc;

using Iris.Core.Types;

namespace Iris.FrontEnd.Api.Interfaces
{
    public interface IPatchService
    {
        [WampProcedure(DataUri.PatchList)]
        IIrisData ListPatches(string sid);
    }
}

