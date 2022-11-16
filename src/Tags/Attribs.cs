using System.Text.RegularExpressions;

namespace Templification.Tags {


    public static class AttribsExtensions {
        public static  Dictionary<string, Attribs> clone(this  Dictionary<string, Attribs> attrib_map) {
            Dictionary<string, Attribs> clones = new Dictionary<string, Attribs>();
            foreach (var key in attrib_map.Keys) {
                clones.Add(key, attrib_map[key].clone());
            }
            return clones;
        }
    }



    // Attrib type.
    // - Variable
    // - Command
    // - Standard
    public enum AttribType {
        variable,
        command,
        standard,
    }

    public class Attribs {

        public string value    = "";
        public string options  = "";
        public AttribType type = new AttribType();


        public Attribs clone() {
            var cloned = new Attribs() {
                value = this.value,
                options = this.options,
                type = this.type
            };

            return cloned;
        }


        //    Convert an array of locations to an array of groups
        public static  List<TagGroup> make_special_tag_groups(List<Match> locations, string source, TagSubType  sub_type, int offset) {
            var groups  =  new List<TagGroup>();

            foreach (var location in locations) {
                var start =  location.Index;
                var end   =  location.Index + location.Length;
                groups.Add(new TagGroup{
                        start    = start,
                        end      = end,
                        sub_type = sub_type,
                    });
                if (sub_type == TagSubType.cshtml ) {
                    groups.Add(new TagGroup{
                            start = end,
                            end = end,
                            sub_type = sub_type,
                        });
                }
            }

            return groups;
        }

        //    Convert an array of locations to an array of groups
        public static List<TagGroup> make_location_groups(List<Match> locations, int offset, int fill_index) {
            var groups  =  new List<TagGroup>();
            var findex  =  offset;

            foreach (var location in locations) {
                var start =  location.Index  + offset;
                var end   =  (location.Index + location.Length) + offset;
                if (fill_index > 0 ) {
                    if (findex < start ) {
                        groups.Add(new TagGroup{
                                start = findex,
                                end = start
                            });
                    }
                    findex = end;
                }
                groups.Add(new TagGroup{
                        start = start,
                        end = end
                    });
            }

            if (fill_index > 0 && findex > offset ) {
                groups.Add(new TagGroup{
                        start = findex,
                        end   = fill_index + offset
                    });
            }

            return groups;
        }
    }  // END CLASS
}  // END NAMESPACE
