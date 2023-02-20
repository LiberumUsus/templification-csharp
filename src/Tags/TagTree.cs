using System.Text;

using Templification.Utils;
using Templification.Styles;
using Templification.Data;

namespace Templification.Tags {

    public enum TreeType {
        Standard,
        SubTemplate
    }


    // Tree Structure for operating on TagBranch a root node
    public class TagTree {

        public Dictionary<string, StringBuilder> bundled_scripts = new Dictionary<string, StringBuilder>();

        public string                             tree_name   = "";
        public TagBranch                          root        = new TagBranch();
        public StyleSheet                         styles      = new StyleSheet();
        public List<TagBranch>                    in_to_out   = new List<TagBranch>();
        public Dictionary<string,TagTree> local_templates     = new Dictionary<string,TagTree>();
        public Dictionary<string,List<TagBranch>> tag_map     = new Dictionary<string,List<TagBranch>>();
        public Dictionary<string,List<TagBranch>> slot_map    = new Dictionary<string,List<TagBranch>>();
        public Dictionary<string,bool>            class_list  = new Dictionary<string,bool>();
        public FileDetails?                       fileDetails = null;
        public TreeType                           type        = TreeType.Standard;



        // INITIALIZE THE TAG TREE WITH A ROOT NODE
        public void init(TagBranch root) {
            this.root = root;
            // Populate fileDetails from tag if it exists
            setFileDetails(this.root);
        }



        private void setFileDetails(TagBranch node) {
            // Only set it the first time
            if (node.tag.sub_type == TagSubType.filedetails && fileDetails == null) {
                this.fileDetails = new FileDetails();
                var details = node.tag.tstr;
                var lines   = details.Split("\n");
                foreach (var line in lines) {
                    var parts = line.Split("=");
                    if (parts[0].ToLower().Trim() == "fileoutname" && parts.Length > 1) {
                        this.fileDetails.FileOutName = parts[1].Trim();
                    }
                }
            }
            if (this.fileDetails == null) {
                foreach (var child in node.children) {
                    setFileDetails(child);
                }
            }
        }



        public TagTree clone() {
            //(self TagTree)
            var new_tree  = new TagTree {
                root        = this.root.clone(),
                styles      = this.styles.clone(),
                tree_name   = this.tree_name,
                type        = this.type,
                fileDetails = this.fileDetails != null ? this.fileDetails.clone() : null,
                local_templates = new Dictionary<string, TagTree>()
            };

            new_tree.index_tags();
            new_tree.index_slots();
            return new_tree;
        }



        public void collect_classes() {
            //(self TagTree)
            this.root.collect_classes(this.class_list);
        }



        public void collect_scripts() {
            this.root.collect_scripts(this.bundled_scripts);
        }



        // Get the tag map, the whole thing
        public Dictionary<string,List<TagBranch>> get_map() {
            //(self TagTree)
            return this.tag_map;
        }



        // Get the style sheet for this Tree
        public StyleSheet get_styles() {
            //(self TagTree)
            return this.styles;
        }



        // Get the slot map, the whole thing
        public Dictionary<string,List<TagBranch>> get_slots() {
            //(self TagTree)
            return this.slot_map;
        }



        // Get TagBranch a array from the tag map given a tag name
        public List<TagBranch> get_tag(string tag) {
            //(self TagTree)
            switch(this.has_tag(tag)) {
                case true:      {
                    return this.tag_map[tag];
                }
                case false: {
                    return new List<TagBranch>();
                }
            }

        }



        // return true or false as to whether a tag key exists in the tag map
        public bool has_tag(string tag) {
            return this.tag_map.ContainsKey(tag);
        }



        // Index all of the tags in the root into the tree map
        public void index_tags() {
            index_tag_tree(this.root, this.tag_map, this.in_to_out);
        }



        // Index all of the slot int attributeso the slot map
        // this is performed over the entire TagTree
        public void index_slots() {
            index_slot_attribs(this.root, this.slot_map);
        }



        public void index_commands() {
            this.root.index_tag_commands();
        }



        // Process commands that are in the attributes section of a node
        public void process_attrib_commands() {
            this.root.process_attrib_commands();
        }



        public TagTree process_commands(TagBranch usage) {
            var new_tree     = this.clone();
            var delete_parts = new List<int>();
            var found_match  = false;

            var tbranch  =  new_tree.root;
            // ITERATE OVER COMMANDS AND LOCATE NODES THAT WILL NOT BE INCLUDED
            // IN FINAL SELECTION
            foreach (var command in tbranch.commands ) {
                if (verify_command_value(command, usage) && !found_match ) {
                    found_match = true;
                    delete_parts.Add(command.index);
                } else {
                    for (var i = command.index; i < command.next_block; i++ ) {
                        delete_parts.Add(i);
                    }
                }
            }

            delete_parts.Reverse();
            // DELETE NODES THAT ARE EXCLUDED BASED ON COMMANDS
            foreach (var index in delete_parts) {
                if (index >= 0 && index < new_tree.root.children.Count) {
                    new_tree.root.children.RemoveAt(index);
                } else {
                    Console.WriteLine("BadRemoveIndex:" + index + ": COUNT " + new_tree.root.children.Count);
                }
            }

            found_match = false;
            delete_parts = new List<int>();
            foreach (var child in tbranch.children ) {
                if (!child.tag.attribs.ContainsKey(APP.ATTRIB_SLOT_NAME)) {
                    foreach (var command in child.commands ) {
                        if (verify_command_value(command, usage) && !found_match ) {
                            found_match = true;
                            delete_parts.Add(command.index);
                        } else {
                            for(var i = command.index; i < command.next_block; i++ ) {
                                delete_parts.Add(i);
                            }
                        }
                    }
                }
                foreach (var index in delete_parts) {
                    child.children.RemoveAt(index);
                }
            }

            new_tree.index_tags();
            new_tree.index_slots();
            return new_tree;
        }



        public bool verify_command_value(BranchCommand command, TagBranch usage) {
            var matches_attrib  =  false;
            if (command.name == "else" ) {
                return true;
            }
            var attribs  =  Utils.Utils.split_any(command.attribs, new List<string>{"&&", "||"});

            foreach (var attr in attribs ) {
                // TURN SQUARE INT BRACKETS INTO CURLY FOR VARIABLES
                var mod_attr  = attr.Replace("[","{").Replace("]","}");
                if (mod_attr.Trim().Length <= 0 ) {
                    continue;
                }
                if (mod_attr.Contains("=") ) {
                    var key_value  =  mod_attr.Trim().Split("=");
                    var key   = key_value[0].Trim();
                    var value = key_value[1].Trim();
                    if (usage.tag.attribs.ContainsKey(key) ) {
                        if (usage.tag.attribs[key].Value == value ) {
                            matches_attrib = true;
                        }
                    }
                } else if (usage.tag.attribs.ContainsKey(mod_attr) && usage.tag.attribs[mod_attr].Value.Length > 0 ) {
                    matches_attrib = true;
                }
            }

            return matches_attrib;
        }



        // Private recursive function to map tags in the tree
        bool index_tag_tree(TagBranch self, Dictionary<string,List<TagBranch>> mapping , List<TagBranch> in_to_out) {
            var name            = "";
            var has_default_var = false;
            var tdata           = self.tag;
            var removeList      = new List<TagBranch>();
            name                = tdata.name;

            Utils.Utils.ensure_map_has_entry(name, new List<TagBranch>(), mapping);

            if (name.Length > 0 ) {
                mapping[name].Add(self);
            }

            foreach (var child in self.children) {
                if (child.has_default_var || child.tag.tstr.Contains("{default}")) {
                    self.has_default_var = true;
                    has_default_var = true;
                }

                if (child.tag.sub_type == TagSubType.template) {
                    var newTree       = new TagTree();
                    var template_name = child.tag.name;
                    // TEMPLATE IS IN A TEMPLATE TAG
                    if (child.tag.internal_attribs.ContainsKey(APP.ATTRIB_TEMPLATE)) {
                        template_name = child.tag.internal_attribs[APP.ATTRIB_TEMPLATE].Value;
                        var root = new TagBranch();
                        root.tag.tag_type = TagType.root;
                        root.closing_tag.tag_type = TagType.root;
                        root.children.Add(child);
                        newTree.init(root);
                    } else if (child.tag.name.StartsWith(APP.PREFIX_TEMPLATE)) {
                        template_name              = child.tag.name.Substring(2);
                        child.tag.tag_type         = TagType.root;
                        child.tag.sub_type         = TagSubType.empty;
                        child.closing_tag.tag_type = child.tag.tag_type;
                        child.closing_tag.sub_type = child.tag.sub_type;
                        removeList.Add(child);
                        newTree.init(child);
                    }
                    template_name = template_name.ToLower().Trim();
                    if (!this.local_templates.ContainsKey(template_name)) {
                        this.local_templates.Add(template_name, newTree);
                    }
                }

                has_default_var = index_tag_tree(child, mapping, in_to_out) || has_default_var;
            }

            // REMOVE TEMPLATES
            foreach (var item in removeList) {
                self.children.Remove(item);
            }

            // MAP INNER TO OUTER TAGS
            in_to_out.Add(self);
            return has_default_var;
        }



        // Private recursive function to map slots in the tree
        void index_slot_attribs(TagBranch self, Dictionary<string,List<TagBranch>> mapping ) {
            var value  =  "";
            var tdata  =  self.tag;
            if (tdata.attribs.ContainsKey(APP.ATTRIB_SLOT_NAME) ) {
                value = tdata.attribs[APP.ATTRIB_SLOT_NAME].Value;
                Utils.Utils.ensure_map_has_entry(value, new List<TagBranch>(), mapping);
            }
            if (value.Length > 0 ) {
                mapping[value].Add(self);
            }
            foreach (var tag in self.children ) {
                index_slot_attribs(tag, mapping);
            }
        }

    }  // END CLASS
}  // END NAMESPACE
