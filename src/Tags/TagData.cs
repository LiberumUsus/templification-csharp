using Templification.Utils;
using Templification.Data;
using System.Text;
using Microsoft.VisualBasic;

namespace Templification.Tags {
    // Tag Data structure, contains all information about a tag
    public class TagData {

        public int    id               = 0;
        public bool   no_merge_attribs = false;
        public string name             = "";
        public string source           = "";
        public string tstr             = "";
        public string new_line         = "";

        public TagType                    tag_type         = TagType.empty;
        public TagSubType                 sub_type         = TagSubType.empty;
        public Re_group                   outer            = new Re_group();
        public Re_group                   inner            = new Re_group();
        public Dictionary<string,Attribs> attribs          = new Dictionary<string,Attribs>();
        public Dictionary<string,Attribs> internal_attribs = new Dictionary<string,Attribs>();



        public bool is_set() {
            //(TagData self)
            return this.name.Length > 0 && this.tag_type != TagType.empty;
        }

        public string str() {
            //(TagData self)
            var sbuild  =  new StringBuilder(50);

            sbuild.Append(this.name + "\n");
            sbuild.Append(this.attribs.ToString() + "\n");
            sbuild.Append(this.tag_type.ToString() + "\n");
            sbuild.Append(this.sub_type.ToString() + "\n");
            sbuild.Append(this.tstr + "\n");
            sbuild.Append(this.new_line + "\n");
            return sbuild.ToString();
        }

        // Clone TagData a and return the tag
        public TagData clone() {
            //(TagData self)
            var new_source  =  "";
            if (!string.IsNullOrEmpty(this.source)) {
                new_source = this.source;
            }
            var new_tag  = new TagData{
                name             = this.name,
                attribs          = new Dictionary<string,Attribs>(),
                internal_attribs = new Dictionary<string,Attribs>(),
                outer            = this.outer,
                inner            = this.inner,
                tag_type         = this.tag_type,
                sub_type         = this.sub_type,
                source           = new_source,
                tstr             = this.tstr,
                new_line         = this.new_line,
                id               = this.id,
                no_merge_attribs = this.no_merge_attribs,
            };

            foreach (var keyPair in this.attribs) {
                new_tag.attribs.Add(keyPair.Key, keyPair.Value.clone());
            }
            foreach (var keyPair in this.internal_attribs) {
                new_tag.internal_attribs.Add(keyPair.Key, keyPair.Value.clone());
            }

            return new_tag;
        }

        // Merge attributes int providedo existing
        public void merge_attribs(Dictionary<string,Attribs> attribs , bool only_important) {
            //(TagData self)
            var mergeable  = new Dictionary<string, bool> {
                {"class", true},
                {"style", true}
            };

            foreach (var KPair in attribs ) {
                var key = KPair.Key;
                var attrib = KPair.Value;
                if ((!attrib.options.Contains("!") && only_important) ||
                    (attrib.options.Contains("!") && !only_important) ) {
                    continue;
                }
                if (attrib.type != AttribType.variable ) {
                    if (!this.attribs.ContainsKey(key) || attrib.options.Contains("o") ) {
                        this.attribs[key] = attrib;
                    } else if (attrib.options.Contains("d") ) {
                        this.attribs.Remove(key);
                    } else if (mergeable.ContainsKey(key) ) {
                        var attrib_value  =  this.attribs[key].value;
                        if (attrib.options.Contains("t") ) {
                            var new_value  =  Utils.Utils.replace_class_with_prefix(this.attribs[key].value, attrib.value);
                            this.attribs[key].value = new_value;
                        } else {
                            this.attribs[key].value = attrib_value.Trim() + " " + attrib.value;
                        }
                    }
                } else {
                    this.attribs[key] = attrib;
                }
            }
        }


        // Merge bounds from other tag
        public void merge_bounds(TagData otag) {
            //(TagData self)
            var new_outer  = new Re_group {
                start = this.outer.start,
                end = otag.outer.end,
            };
            this.outer = new_outer;
        }

        // TagData Get unique id
        public int get_id() {
            //(TagData self)
            return this.id;
        }

        // TagData Set unique id
        public void generate_id() {
            var rand = new Random();
            //(TagData self)
            this.id = rand.Next();
        }

        // Print TagData a object
        public void print_all() {
            //(TagData self)
            switch(this.tag_type) {
                case TagType.text: {
                    Console.WriteLine(this.tstr);
                    break;
                }
                default: {
                    if (!string.IsNullOrEmpty(this.source)) {
                        if (Utils.Utils.in_bounds(this.source, this.inner.start, this.inner.end) ) {
                            var sref  =  this.source;
                            Console.WriteLine(sref[this.inner.start..this.inner.end]);
                        }
                    }
                    break;
                }
            }
        }

        // return string a rep of the object
        public string to_string(int indent) {
            //(TagData self)
            var out_string  =  "";
            var padding  =  new String(' ', indent);

            if (this.tag_type == TagType.text || this.sub_type == TagSubType.void_exact || this.sub_type == TagSubType.script ) {
                if (this.tstr.Trim().Length > 0 ) {
                    switch(this.sub_type) {
                        case TagSubType.script: {
                            out_string = "\n" + padding;
                            break;
                        }
                        default: {
                            break;
                        }
                    }

                    out_string += this.tstr;
                }
            } else if (this.tag_type != TagType.empty && this.sub_type != TagSubType.void_exact ) {
                var sbuilder  =  new StringBuilder(50);
                sbuilder.Append("<");
                if (this.tag_type == TagType.end ) {
                    sbuilder.Append("/");
                }
                sbuilder.Append(this.name);
                foreach (var KeyPair in this.attribs ) {
                    var key = KeyPair.Key;
                    var attrib = KeyPair.Value;
                    if (key.Length <= 0 || attrib.type == AttribType.command ) {
                        continue;
                    }

                    sbuilder.Append(" ");
                    sbuilder.Append(key);
                    if (key != attrib.value ) {
                        sbuilder.Append("=\"" + attrib.value + "\"");
                    }
                }
                if (this.tag_type == TagType.single ) {
                    sbuilder.Append("/");
                }
                sbuilder.Append(">");
                out_string = sbuilder.ToString();
            }

            if (this.new_line == "\n" ) {
                out_string = this.new_line + padding + out_string;
            }

            return out_string;
        }

        // string Write info to data file
        public TextData get_text_data() {
            //(TagData self)
            var tdat  = new TextData();
            if (this.tag_type == TagType.text ) {
                if (this.tstr.Trim().Length > 0 ) {
                    tdat.value = this.tstr + "\n";
                }
            }
            return tdat;
        }

        // Print outer bounds from source TagData for
        public void print_outer() {
            //(TagData self)
            if (!string.IsNullOrEmpty(this.source)) {
                var sref  =  this.source;
                Console.WriteLine(sref[this.outer.start..this.outer.end]);
            }
        }

        // Initialize function TagData for
        public void init(string name, string source, Re_group bounded) {
            //(TagData self)
            this.name = name;
            this.source = source;
            this.outer = bounded;
            this.inner = bounded;
        }
    }  // END CLASS
}  // END NAMESPACE
