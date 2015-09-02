using System;

using Iris.Core.Types;

namespace Iris.Core.Events
{
    public class PatchEventArgs : EventArgs
    {
        public String SessionId;
        public IIrisPatch Patch;

        public PatchEventArgs (string _sid, IIrisPatch patch)
        {
            Patch = patch;
            SessionId = _sid;
        }
    }
}