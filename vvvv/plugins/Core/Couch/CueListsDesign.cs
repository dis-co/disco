using System;

namespace Iris.Core.Couch
{
    public class CueListsDesign : DesignDoc


    {
        public CueListsDesign()
        {
            _id = "_design/cuelists";

            views = new {
                all = new {
                    map =
                    @"function(doc) {
                        if(doc.Type === ""CueList"")
                          emit(doc._id, null);
                      }"
                }
            };

            filters = new {
                cuelists =
                @"function(doc, req) {
                     if( doc._id.match(/design/) ||
                       (!doc._deleted && doc.Type != ""CueList"")) {
                       return false;
                     }
                     return true;
                  }"
            };
        }

        public override string Uri()
        {
            return _id;
        }
    }
}