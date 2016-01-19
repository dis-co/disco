using System;

using Iris.Core.Types;

namespace Iris.Core.Events
{
    public class CueListEventArgs : EventArgs
    {
        public String SessionId;
        public CueList CueList;

        public CueListEventArgs (string _sid, CueList list)
        {
            CueList = list;
            SessionId = _sid;
        }
    }
}