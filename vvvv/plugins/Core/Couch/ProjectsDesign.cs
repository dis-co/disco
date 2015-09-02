using System;

namespace Iris.Core.Couch
{
    public class ProjectsDesign : DesignDoc
    {
        public ProjectsDesign()
        {
            _id = "_design/projects";

            views = new {
                all = new {
                    map =
                    @"function(doc) {
                        if(doc.Type === ""Project"")
                          emit(doc._id, null);
                      }"
                },
                active = new {
                    map =
                    @"function(doc) {
                        if(doc.Type === ""Project"" && doc.Loaded)
                          emit(doc._id, null);
                      }"
                }
            };

            filters = new {
                projects =
                @"function(doc, req) {
                     if(doc._id.match(/design/) || doc._id.match(/schema/) || (!doc._deleted && doc.Type != ""Project"")) {
                       return false;
                     }
                     return true;
                  }"
            };
        }

        public override string Uri()
        {
            return "projects/" + _id;
        }
    }
}
