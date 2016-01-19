namespace Iris.Core.Couch
{
    public class CuesDesign : DesignDoc
    {
        public CuesDesign()
        {
            _id = "_design/cues";

            views = new {
                all = new {
                    map =
                    @"function(doc) {
                        if(doc.Type === ""Cue"")
                          emit(doc._id, null);
                      }"
                }
            };

            filters = new {
                cues =
                @"function(doc, req) {
                     if( doc._id.match(/design/) ||
                       (!doc._deleted && doc.Type != ""Cue"")) {
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