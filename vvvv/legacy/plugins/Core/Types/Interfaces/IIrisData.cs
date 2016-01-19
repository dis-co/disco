using System;

namespace Iris.Core.Types
{
    public interface IIrisData : IDisposable
    {
        string ToJSON();
        string ToUriPath();
    }
}