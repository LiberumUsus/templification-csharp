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



    // Class to store HTML element attributes
    public class Attribs {

        public string value    = "";
        public string options  = "";
        public AttribType type = new AttribType();



        public Attribs clone() {
            var cloned = new Attribs() {
                value   = this.value,
                options = this.options,
                type    = this.type
            };

            return cloned;
        }

    }  // END CLASS
}  // END NAMESPACE
