using Templification.Styles;
using Templification.Data;
using System.Text;
using System.Text.RegularExpressions;
using Templification.Utils;

namespace Templification.Tags {


   public static class TagBranchExtensions {
       public static List<TagBranch> clone(this List<TagBranch> branches) {
            List<TagBranch> clones = new List<TagBranch>();
            foreach (var branch in branches) {
                clones.Add(branch.clone());
            }
            return clones;
        }
   }


    // Filled Tag Node
    public partial class TagBranch {

        public int    iter_idx;
        public int    indent;
        public bool   has_default_var;
        public string bundle_scripts = "";

        public TagData                            tag         = new TagData();
        public TagData                            closing_tag = new TagData();
        public TagBranch?                         parent      = null;
        public StyleSheet                         style_sheet = new StyleSheet();
        public Dictionary<string,List<TagBranch>> slot_map    = new Dictionary<string,List<TagBranch>>();
        public List<BranchCommand>                commands    = new List<BranchCommand>();
        public List<TagBranch>                    children    = new List<TagBranch>();


        // Print the outer section of the branch
        public void print_outer() {
            //(TagBranch self)
            var tag_start  =  this.tag;
            var tag_end    =  this.closing_tag;

            // ERROR CHECKS
            if (!tag_end.is_set() ) {
                return;
            }
            if (!tag_start.is_set() ) {
                return;
            }

            if (!string.IsNullOrEmpty(tag_start.source)) {
                return;
            }
            var sref  =  tag_start.source;
            if (!string.IsNullOrEmpty(sref)) {
                return;
            }

            if (Utils.Utils.in_bounds(sref, tag_start.outer.start, tag_end.outer.end) ) {
                Console.WriteLine(sref[tag_start.outer.start..tag_end.outer.end]);
            }
        }

        public void print_all() {
            //(TagBranch self)
            var tag_start =  this.tag;
            var tag_end   =  this.closing_tag;

            // ERROR CHECKS
            if (tag_start.is_set() && tag_start.tag_type != TagType.root ) { //}&& tag_start.name == "style" ) {
                tag_start.print_all();
            }

            foreach (var child in this.children ) {
                child.print_all();
            }
            if (tag_end.is_set() && tag_end.tag_type != TagType.root ){//&& tag_end.name == "style" ) {
                tag_end.print_all();
            }
        }

        public void collect_classes(Dictionary<string,bool> list ) {
            //(TagBranch self)
            if (this.tag.attribs.ContainsKey("class")) {
                var class_array  =  this.tag.attribs["class"].value.Split(" ");
                foreach (var item in class_array ) {
                    if (!list.ContainsKey(item) ) {
                        list[item] = true;
                    }
                }
            }
            foreach (var child in this.children ) {
                child.collect_classes(list);
            }
        }


        public string collect_scripts() {
            //(TagBranch self)
            var sbuild  =  new StringBuilder(100);

            if (this.tag.sub_type == TagSubType.script && this.tag.attribs.ContainsKey("target") && this.tag.attribs["target"].value == "bundle" ) {
                var part  =  this.tag.tstr.AllAfter(">");
                var end_tag_index  = part.LastIndexOf("<") ; // or  this.tag.tstr.Length
                part = part[..end_tag_index];
                sbuild.Append(part);
            }
            foreach (var child in this.children ) {
                sbuild.Append(child.collect_scripts());
            }
            return sbuild.ToString();
        }

        // Create string a of the object
        public string to_string(int indent) {
            //(TagBranch self)
            var tag_start  =  this.tag;
            var tag_end  =  this.closing_tag;
            var out_string  =  "";

            if (tag_start.name.ToLower() == "style" ) {
                return out_string;
            } else if ( tag_start.attribs.ContainsKey("target") && tag_start.sub_type == TagSubType.script && tag_start.attribs["target"].value != "bundle" ) {
                return tag_start.to_string(indent);
            } else if ( tag_start.attribs.ContainsKey("target") && tag_start.sub_type == TagSubType.script && tag_start.attribs["target"].value == "bundle" ) {
                return "";
            }

            // ERROR CHECKS
            if (tag_start.is_set() && tag_start.tag_type != TagType.root && tag_start.name.ToLower() != "void" ) {
                out_string += tag_start.to_string(indent);
            }

            var local_indent = (tag_start.tag_type != TagType.root ) ?  indent + 4  :  indent ;
            foreach (var child in this.children ) {
                out_string += child.to_string(local_indent);
            }

            if (tag_end.is_set() && tag_end.tag_type != TagType.root && tag_end.name.ToLower() != "void" ) {
                out_string += tag_end.to_string(indent);
            }
            return out_string;
        }

        // string Write info to data file
        public List<TextData> get_text_data() {
            //(TagBranch self)
            var tag_start  =  this.tag;
            var textdata  =  new List<TextData>();
            var query  =  @"(h[1-9])";
            var re  = new  Regex(query) ; // or  panic(err)

            if (tag_start.name.ToLower() == "style" || tag_start.sub_type == TagSubType.script
                || tag_start.name.ToLower() == "void" ) {
                return new List<TextData>();
            }

            if (tag_start.attribs.ContainsKey("data-search-skip") && tag_start.attribs["data-search-skip"].value.Length <= 0 ) {
                // ERROR CHECKS
                if (tag_start.is_set() && tag_start.tag_type != TagType.root && tag_start.name.ToLower() != "void" ) {
                    var tdat  =  tag_start.get_text_data();
                    if (tdat.value.Length > 0 ) {
                        textdata.Add(tdat);
                    }
                }

                foreach (var child in this.children ) {
                    var matches = re.Match(child.tag.name.ToLower());
                    if (matches.Success) {  //("h[123456789]") {
                        textdata.Add(new TextData{
                                element = "header",
                            });
                    } else if (textdata.Count <= 0 ) {
                        textdata.Add(new TextData{});
                    }
                    foreach (var ddata in child.get_text_data() ) {
                        textdata.Last().value += " " + ddata.value;
                    }
                }
            }
            return textdata;
        }



        // Iterator method TagBranch for
        public TagBranch next() {
            //(TagBranch self)
            if (this.iter_idx >= this.children.Count ) {
                this.iter_idx = 0;
                throw new Exception("");
            } else {
                var out_tag  =  this.children[this.iter_idx];
                this.iter_idx += 1;

                return out_tag;
            }
        }



        // Iterate over branch and children calling function list
        public void apply_ops(Func<TagBranch>[] func_list) {
            //(TagBranch self)

            foreach (var func in func_list ) {
                func.DynamicInvoke(this);
                foreach (var branch in this.children) {
                    func.DynamicInvoke(branch);
                }
            }

        }



        // Iterate over branch and children calling function list
        public void apply_ops_to_all(Func<TagBranch>[] func_list) {
            //(TagBranch self)


            foreach (var func in func_list ) {
                func.DynamicInvoke(this);
                foreach (var branch in this.children) {
                    branch.apply_ops_to_all(func_list);
                }
            }

        }



        // Collect tag int string nameso based on level
        public void iter_print() {
            //(TagBranch self)
            foreach (var tag in this.children ) {
                Console.WriteLine(" " + tag.tag.name);
                tag.iter_print();
            }
        }

        // Add a new TagBranch Child
        public void add_child(TagBranch child, bool create_attrib) {
            //(TagBranch self)
            // ADD TAG TO CHILDREN
            this.children.Add(child);
            // GET ADDED TAG REFERENCE
            var nchild  =  this.children.Last().tag;
            // GENERATE ID IF NEED
            if (nchild.get_id() == 0 ) {
                nchild.generate_id();
            }
            switch(child.tag.sub_type) {
                // ADD COMMAND TO COMMAND LIST IF THIS IS A COMMAND TAG
                case TagSubType.command: {
                    var new_command  = new BranchCommand();
                    new_command.init(child.tag.tstr, this.children.Count - 1);
                    this.commands.Add(new_command);
                    break;
                }
                default: {
                    // ADD SLOT TAGS TO SLOT MAP (DEFAULT ACTION)
                    if (create_attrib) {
                        if (!child.tag.attribs.ContainsKey("slot")) break;
                        var child_slot = child.tag.attribs["slot"].value;
                        if (child_slot.Length > 0 ) {
                            if (!this.slot_map.ContainsKey(child_slot) ) {
                                this.slot_map[child_slot] = new List<TagBranch>();
                            }
                            this.slot_map[child_slot].Add(this.children[this.children.Count - 1]);
                        }
                    }
                    break;
                }
            }
        }

        // Add a new TagBranch Child TagData from
        public void add_child_from_data(TagData child, bool create_attrib) {
            //(TagBranch self)
            this.add_child(new TagBranch{
                    tag = child.clone(),
                }, create_attrib);
            this.children.Last().parent = this;
        }


        public void index_tag_commands() {
            var if_prev = new List<string>{"else", "elseif", "if"};

            var last_command  = new BranchCommand();
            var delete_blocks = new List<int>();
            var last_index    = new Stack<int>();

            // WORK OUT COMMANDS
            var index = 0;
            foreach (var command in this.commands ) {
                if (last_index.Count > 0 ) {
                    last_command = this.commands[last_index.Last()];
                }
                switch(command.get_name()) {
                    case "if": {
                        last_index.Push(index);
                        break;
                    }
                    case "else":
                    case "elseif": {
                        if (if_prev.Contains(last_command.get_name()) ) {
                            this.commands[index].last_block = last_command.index;
                            if (last_index.Count > 0 ) {
                                this.commands[last_index.Last()].next_block = command.index;
                                last_index.Pop();
                            }
                            last_index.Push(index);
                        } else {
                            delete_blocks.Add(command.index);
                        }
                        break;
                    }
                    case "endif": {
                        if (if_prev.Contains(last_command.get_name()) ) {
                            this.commands[index].last_block = last_command.index;
                            if (last_index.Count > 0 ) {
                                this.commands[last_index.Last()].next_block = command.index;
                                last_index.Pop();
                            }
                        } else {
                            delete_blocks.Add(command.index);
                        }
                        break;
                    }
                    default: {
                        last_index.Push(index);
                        break;
                    }
                }
                index++;
            }
            // ITERATE THROUGH CHILDREN, THAT ARE NOT SLOTS
            foreach (var child in this.children ) {
                if (!child.tag.attribs.ContainsKey("slot")) {
                    child.index_tag_commands();
                }
            }
            // END TAGBRANCH CHECK
        }

    }  // END CLASS
}  // END NAMESPACE
