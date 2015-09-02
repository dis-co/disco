using System;

namespace Iris.Core.Couch
{
    public class ClusterDesign : DesignDoc
    {
        public ClusterDesign()
        {
            _id = "_design/cluster";

            views = new {
                all = new {
                    map =
                    @"function(doc) {
                        if(doc.Type === ""Cluster"")
                          emit(doc._id, null);
                      }"
                }
            };

            filters = new {
                clusters =
                @"function(doc, req) {
                     if(doc._id.match(/design/) || doc._id.match(/schema/) || (!doc._deleted && doc.Type != ""Cluster"")) {
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
