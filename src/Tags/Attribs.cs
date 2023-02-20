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

        private string _name   = "";
        private string _value  = "";
        private string _key    = "";

        public string options  = "";
        public bool   active   = true;
        public AttribType type = new AttribType();


        public string Name  {get => _name; set => setName(value);}
        public string Value {get => _value; set => _value = value;}

        private void setName(string name) {
            var index = name.IndexOf(APP.ATTRIB_OPTION_FLAG);
            _name = name;
            _key  = index > 0 ? name.Substring(0, index) : _name;
        }


        public Attribs clone() {
            var cloned = new Attribs() {
                _value  = this._value,
                _name   = this._name,
                _key    = this._key,
                active  = this.active,
                options = this.options,
                type    = this.type,
            };

            return cloned;
        }

        public void merge(Attribs other) {
            this._value   = other._value;
            this._name    = other._name;
            this._key     = other._key;
            this.type     = other.type;
            if (this.options.Length <= 0) {
                this.options = other.options;
            } else {
                foreach (var chr in other.options) {
                    if (!this.options.Contains(chr)) this.options += chr;
                }
            }
        }

        // Parameter key is a failsafe... maybe remove one day
        public string to_string(string key) {
            if (!active) return "";
            if (_key == "") _key = key;
            if (type != AttribType.command && _key.Length > 0) {
                return " " + _key + (_key.Length > 0 ? "=\"" + _value + "\"" : "");
            } else {
                return "";
            }
        }

    }  // END CLASS
}  // END NAMESPACE
