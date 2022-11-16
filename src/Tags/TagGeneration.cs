using Templification.Utils;
using Templification.Tags;


namespace Templification.Tags {

    public class CreateTagState {

        public bool ended_in_delim;
        public bool inserted_non_text;
        public int  last_new_child;

        public List<int>                       remove_list  = new List<int>();
        public Dictionary<int,List<TagBranch>> new_children = new Dictionary<int,List<TagBranch>>();
    }



    public class CreateTagData {

        public int    index;
        public bool   wrap_all;
        public bool   strict_delim;
        public bool   has_indexer;
        public string separator = "";

        public Attribs                    tag_class  = new Attribs();
        public Attribs                    tag_type   = new Attribs();
        public Dictionary<string,Attribs> attrib_map = new Dictionary<string, Attribs>();

        public void init(TagBranch branch) {
            //(self CreateTagData)
            this.separator = branch.tag.attribs["@separator"].value;
            if (this.separator == "\\n" ) {
                this.separator = "\n";
            }
            this.wrap_all = branch.tag.attribs.ContainsKey("@wrap-all");
            this.strict_delim = branch.tag.attribs.ContainsKey("@strict-delim");
            this.extract_attribs(branch);
        }

        public void extract_attribs(TagBranch branch) {
            //(self CreateTagData)
            this.tag_type = (branch.tag.attribs.ContainsKey("@tag")) ? branch.tag.attribs["@tag"] : new Attribs();
            if (this.tag_type.value.Length <= 0 ) {
                // DEFAULT ATTRIB IS "div"
                this.tag_type = new Attribs {
                    value = "div",
                    type = AttribType.command,
                };
            }
            // TAG CLASS
            this.tag_class = (branch.tag.attribs.ContainsKey("@tag-class")) ? branch.tag.attribs["@tag-class"] : new Attribs();
            // CREATE ATTRIB MAP
            foreach (var keyPair in branch.tag.attribs ) {
                var key = keyPair.Key;
                var attrib = keyPair.Value;
                if (key.StartsWith("@tag-") ) {
                    var akey  =  key.AllAfter("-");
                    this.has_indexer = this.has_indexer || attrib.value.Contains("#");
                    this.attrib_map[akey] = new Attribs {
                        value = attrib.value,
                        type = AttribType.standard
                    };
                }
            }
        }
    }

    public static class TagUtils {

        // CREATE TAGS FROM THE DATA IN "branch"
        public static void create_tags_from_tstrs(TagBranch branch) {
            var cdata  = new CreateTagData();
            cdata.init(branch);
            // EXIT IF NO SEPARATOR
            if (cdata.separator.Length < 0 ) {
                return;
            }

            var state  = new CreateTagState();
            // LOOP OVER BRANCH CHILDREN
            var i = 0;
            foreach (var child in branch.children ) {
                cdata.index = i;

                switch(child.tag.tag_type) {
                    case TagType.text: {
                        if (child.tag.tstr.Contains(cdata.separator) ) {
                            process_parts(state, cdata, branch, child, i);
                        } else if (cdata.strict_delim && state.inserted_non_text ) {
                            //          println("no-separator")
                            state.new_children[state.last_new_child].Last().children.Add(child);
                            state.remove_list.Insert(0,i);
                            state.inserted_non_text = false;
                            state.ended_in_delim = child.tag.new_line.Contains(cdata.separator) && i > 0;
                        } else if (cdata.wrap_all ) {
                            state.remove_list.Insert(0,i);
                            var new_item  =  create_wrapping_tag(cdata, branch);
                            new_item.children.Add(child);
                            if (!state.new_children.ContainsKey(i)) state.new_children.Add(i, new List<TagBranch>());
                            state.new_children[i].Add(new_item);
                            state.last_new_child = i;
                            state.inserted_non_text = false;
                        }
                        break;
                    }
                    default: {
                        state.ended_in_delim = child.tag.new_line.Contains(cdata.separator) && i > 0;
                        if (state.ended_in_delim ) {
                            //          println("$i .. \\n")
                        }
                        if (cdata.strict_delim && !cdata.wrap_all && !state.ended_in_delim ) {
                            if (!state.new_children.ContainsKey(state.last_new_child)) {
                                state.new_children[state.last_new_child] = new List<TagBranch>();
                                var new_item  =  create_wrapping_tag(cdata, branch);
                                new_item.children.Add(child);
                                state.new_children[state.last_new_child].Add(new_item);
                            } else {
                                state.new_children[state.last_new_child].Last().children.Add(child);
                            }
                            state.remove_list.Insert(0,i);
                            state.inserted_non_text = true;
                        } else if (cdata.wrap_all || (cdata.strict_delim && state.ended_in_delim) ) {
                                state.remove_list.Insert(0,i);
                            var new_item  =  create_wrapping_tag(cdata, branch);
                            new_item.children.Add(child);
                            if (!state.new_children.ContainsKey(i)) state.new_children.Add(i, new List<TagBranch>());
                            state.new_children[i].Add(new_item);
                            state.last_new_child = i;
                            state.inserted_non_text = true;
                        }
                        break;
                    }
                }
                i++;
            }

            foreach (var j in state.remove_list ) {
                branch.children.RemoveAt(j);

                if (state.new_children.ContainsKey(j)) {
                    state.new_children[j].Reverse();
                    foreach (var child in  state.new_children[j]) {
                        branch.children.Insert(j, child);
                    }
                    state.new_children[j].Reverse();
                }
            }
        }

        //         PROCESS STRING A WITH INT DELIMSO PARTS
        static void process_parts(CreateTagState state , CreateTagData cdata, TagBranch branch, TagBranch child, int i) {
            var parts  =  child.tag.tstr.Split(cdata.separator);

            // DETERMINE DELIM STATE
            state.ended_in_delim = parts.Last().Length <= 0;

            if (!state.ended_in_delim ) {
                // SPECIAL CASE FOR NEWLINES
                state.ended_in_delim = child.tag.new_line.Contains(cdata.separator) && i > 0;
            }
            if (state.ended_in_delim ) {
                //    println("$i .. \\n")
            }
            // ITERATE OVER PARTS
            if (parts.Length > 0 ) {
                state.remove_list.Insert(0,i);
                var j = 0;
                foreach (var part in parts ) {
                    if (state.inserted_non_text ) {
                        var new_node  = new  TagBranch{
                            tag = new TagData{
                                name = "",
                                tag_type = TagType.text,
                                tstr = part,
                            }
                        };
                        var tchild  =  state.new_children[state.last_new_child].Last();
                        new_node.parent = tchild;
                        state.new_children[state.last_new_child].Last().children.Add(new_node);
                        state.inserted_non_text = false;
                        continue;
                    }
                    if (cdata.has_indexer ) {
                        cdata.index = i + j;
                    }
                    var new_item = create_wrapping_tag(cdata, branch);
                    new_item.children.Add(new  TagBranch{
                            tag = new TagData {
                                name = "text",
                                tag_type = TagType.text,
                                tstr = part,
                            },
                            parent = new_item
                        });
                    state.last_new_child = i;
                    if (!state.new_children.ContainsKey(i)) state.new_children.Add(i, new List<TagBranch>());
                    state.new_children[i].Add(new_item.clone());
                    j++;
                }
            }
            state.inserted_non_text = false;
        }

        // Create wrapping tag.
        static TagBranch create_wrapping_tag(CreateTagData cdata, TagBranch branch) {
            var local_attribs = cdata.attrib_map.clone();

            if (cdata.index > -1 ) {
                foreach (var keyattrib in local_attribs ) {
                    var key = keyattrib.Key;
                    var attrib = keyattrib.Value;
                    if (attrib.value.Contains("#") ) {
                        local_attribs[key].value = attrib.value.Replace("#", cdata.index.ToString());
                    }
                }
            }

            var return_tag  = new  TagBranch{
                tag = new TagData {
                    name = cdata.tag_type.value,
                    tag_type = TagType.start,
                    new_line = "\n",
                    attribs = local_attribs,
                },
                closing_tag = new TagData{
                    name = cdata.tag_type.value,
                    tag_type = TagType.end,
                    new_line = "\n"
                }
            };
            return_tag.parent = branch;
            return return_tag;
        }

    }  // END CLASS
}  // END NAMESPACE
