using WampSharp.V2.Rpc;

using Iris.Core.Types;

namespace Iris.FrontEnd.Api.Interfaces
{
    public interface IProjectService
    {
        [WampProcedure(DataUri.ProjectList)]
        IIrisData ListProjects(string sid);

        [WampProcedure(DataUri.ProjectCreate)]
        IIrisData CreateProject(string sid, Project body);

        [WampProcedure(DataUri.ProjectRead)]
        IIrisData ReadProject(string sid, Project body);

        [WampProcedure(DataUri.ProjectUpdate)]
        IIrisData UpdateProject(string sid, Project body);

        [WampProcedure(DataUri.ProjectDelete)]
        IIrisData DeleteProject(string sid, Project body);
    }
}
