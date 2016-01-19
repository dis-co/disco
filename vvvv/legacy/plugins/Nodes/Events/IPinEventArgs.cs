using System;

using VVVV.PluginInterfaces.V2.Graph;

namespace Iris.Nodes.Events
{
    public class IPinEventArgs : EventArgs
    {
        public IPin2 Pin;

        public IPinEventArgs (IPin2 pin)
        {
            Pin = pin;
        }
    }
}