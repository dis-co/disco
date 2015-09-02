using System;

using Iris.Core.Types;

namespace Iris.Core.Events
{
    public class PinEventArgs : EventArgs
    {
        public IIrisPin Pin;
        public UpdateDirection Direction;

        public PinEventArgs (UpdateDirection dir, IIrisPin pin)
        {
            Direction = dir;
            Pin = pin;
        }
    }
}

