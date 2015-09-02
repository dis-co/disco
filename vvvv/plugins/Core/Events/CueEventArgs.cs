using System;

using Iris.Core.Types;

namespace Iris.Core.Events
{
    public class CueEventArgs : EventArgs
    {
        public String SessionId;
        public Cue Cue;

        public CueEventArgs (string _sid, Cue cue)
        {
            Cue = cue;
            SessionId = _sid;
        }
    }
}