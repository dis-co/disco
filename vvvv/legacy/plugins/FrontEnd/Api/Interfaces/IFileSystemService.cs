using Iris.Core.Types;
using WampSharp.V2.Rpc;
using System.Collections.Generic;

namespace Iris.FrontEnd.Api.Interfaces
{
    public interface IFileSystemService
    {
        [WampProcedure(DataUri.FsRead)]
        FsEntity GetEntity(string sid, string path);

        [WampProcedure(DataUri.DriveList)]
        List<Drive> GetDrives(string sid);
    }
}

