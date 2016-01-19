using WampSharp.V2.Rpc;

using Iris.Core.Types;

namespace Iris.FrontEnd.Api.Interfaces
{
    public interface ICueListService
    {
        [WampProcedure(DataUri.CueListList)]
        IIrisData ListCueLists(string sid);

        [WampProcedure(DataUri.CueListCreate)]
        IIrisData CreateCueList(string sid, CueList list);

        [WampProcedure(DataUri.CueListRead)]
        IIrisData ReadCueList(string sid, CueList list);

        [WampProcedure(DataUri.CueListUpdate)]
        IIrisData UpdateCueList(string sid, CueList list);

        [WampProcedure(DataUri.CueListDelete)]
        IIrisData DeleteCueList(string sid, CueList list);
    }
}
