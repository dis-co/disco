using Iris.Core.Types;
using WampSharp.V2.Rpc;

namespace Iris.FrontEnd.Api.Interfaces
{
    public interface ICueService
    {
        [WampProcedure(DataUri.CueList)]
        IIrisData ListCues(string sid);

        [WampProcedure(DataUri.CueCreate)]
        IIrisData CreateCue(string sid, Cue cue);

        [WampProcedure(DataUri.CueRead)]
        IIrisData ReadCue(string sid, Cue cue);

        [WampProcedure(DataUri.CueUpdate)]
        IIrisData UpdateCue(string sid, Cue cue);

        [WampProcedure(DataUri.CueDelete)]
        IIrisData DeleteCue(string sid, Cue cue);
    }
}

