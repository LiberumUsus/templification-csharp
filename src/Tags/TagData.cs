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

        public TagData() {

        }
        public TagData(string tagName, TagType ttype, TagSubType subType, string tstrData = "") {
            name     = tagName;
            tag_type = ttype;
            sub_type = subType;
            tstr     = tstrData;
        }

        public bool is_set() {
            return this.name.Length > 0 && this.tag_type != TagType.empty;
        }

        public string str() {
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



        public Dictionary<string, Attribs> get_attribs_bytype(AttribType type) {
            var newMap = new Dictionary<string, Attribs>();
            foreach(var keyPair in attribs) {
                if (keyPair.Value.type == type) {
                    newMap[keyPair.Key] = keyPair.Value;
                }
            }
            return newMap;
        }

        // Merge attributes int providedo existing
        public void merge_attribs(Dictionary<string,Attribs> attribs , bool only_important) {
            var mergeable  = new Dictionary<string, bool> {
                {"class", true},
                {"style", true}
            };

            foreach (var KPair in attribs ) {
                var key    = KPair.Key;
                var attrib = KPair.Value;
                if ((!attrib.options.Contains(APP.ATTRIB_FLAG_IMPORTANT) && only_important) ||
                    (attrib.options.Contains(APP.ATTRIB_FLAG_IMPORTANT) && !only_important) ) {
                    continue;
                }
                if (attrib.type != AttribType.variable ) {
                    // OVERRIDE ATTRIB COMPLETELY
                    if (!this.attribs.ContainsKey(key)) {
                        this.attribs[key] = attrib;
                    } else if (attrib.options.Contains(APP.ATTRIB_FLAG_OVERRIDE)) {
                        this.attribs[key].merge(attrib);
                    } else if (this.attribs[key].options.Contains(APP.ATTRIB_FLAG_OVERRIDE)) {
                        // DO NOTHING, LET THE ORIGINAL CLASS VALUES WIN
                    } else if (this.attribs[key].options.Contains(APP.ATTRIB_FLAG_DELETE) ) {
                        // DELETE ATTRIB IN TARGET
                        this.attribs[key].active = false;
                    } else if (mergeable.ContainsKey(key) ) {
                        var attrib_value  =  this.attribs[key].Value;
                        // OVERRIDE EXISTING CLASSES THAT HAVE MATCHING PREFIXES
                        // E.G. class%t="border-2" ==> template: class="border-1"
                        // final result is: class="border-2" ... NOT: class="border-1 border-2"
                        if (attrib.options.Contains(APP.ATTRIB_FLAG_SUBST) ) {
                            var new_value  =  Utils.Utils.replace_class_with_prefix(attrib_value, attrib.Value);
                            this.attribs[key].Value = new_value;
                        } else {
                            this.attribs[key].Value = attrib_value.Trim() + " " + attrib.Value;
                        }
                    }
                } else {
                    this.attribs[key] = attrib;
                }
            }
        }


        // Merge bounds from other tag
        public void merge_bounds(TagData otag) {
            var new_outer  = new Re_group {
                start = this.outer.start,
                end = otag.outer.end,
            };
            this.outer = new_outer;
        }

        // TagData Get unique id
        public int get_id() {
            return this.id;
        }

        // TagData Set unique id
        public void generate_id() {
            var rand = new Random();
            this.id = rand.Next();
        }

        // Print TagData a object
        public void print_all() {
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
            var out_string  =  "";
            var padding  =  new String(' ', indent);

            if (this.tag_type == TagType.text || this.sub_type == TagSubType.void_exact || this.sub_type == TagSubType.script ) {
                if (this.tstr.Trim().Length > 0 ) {
                    switch(this.sub_type) {
                        case TagSubType.script: {
                            out_string = "\n" + padding + this.tstr;
                            break;
                        }
                        case TagSubType.comment: {
                            out_string = "\n" + padding;
                            out_string += this.tstr.Trim();
                            break;
                        }
                        default: {
                            out_string = this.tstr;
                            break;
                        }
                    }

                }
            } else if (this.tag_type != TagType.empty && this.sub_type != TagSubType.void_exact ) {
                var sbuilder  =  new StringBuilder(50);
                sbuilder.Append("<");
                if (this.tag_type == TagType.end ) {
                    sbuilder.Append("/");
                }
                sbuilder.Append(this.name);

                foreach (var KeyPair in this.attribs ) {
                    var key    = KeyPair.Key;
                    var attrib = KeyPair.Value;
                    sbuilder.Append(attrib.to_string(key));
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
            if (!string.IsNullOrEmpty(this.source)) {
                var sref  =  this.source;
                Console.WriteLine(sref[this.outer.start..this.outer.end]);
            }
        }

        // Initialize function TagData for
        public void init(string name, string source, Re_group bounded) {
            this.name = name;
            this.source = source;
            this.outer = bounded;
            this.inner = bounded;
        }
    }  // END CLASS
}  // END NAMESPACE
