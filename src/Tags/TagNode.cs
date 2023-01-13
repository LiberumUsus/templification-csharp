using System.Data;
using System.Text.RegularExpressions;
using Templification.Styles;
using static System.Net.Mime.MediaTypeNames;
using Templification.Utils;

namespace Templification.Tags {

    public partial class TagBranch {

        // CLONE A TAG NODE AND RETURN THE RESULT
        public TagBranch clone() {
            var cloned  = new TagBranch();
            cloned.copy(this);
            return cloned;
        }


        // Copy one int nodeo another
        public void copy(TagBranch other) {
            this.iter_idx        = 0;
            this.tag             = other.tag.clone();
            this.parent          = other.parent;
            this.indent          = other.indent;
            this.closing_tag     = other.closing_tag.clone();
            this.style_sheet     = other.style_sheet.clone();
            this.has_default_var = other.has_default_var;
            this.slot_map        = new Dictionary<string,List<TagBranch>>();
            this.commands        = new List<BranchCommand>();
            this.children        = new List<TagBranch>();

            //foreach (var key in other.slot_map.Keys) {
            //    Console.WriteLine("KEY: " + key);
            //    this.slot_map.Add(key, other.slot_map[key].clone());
            //}
            foreach (var command in other.commands) {
                this.commands.Add(command.clone());
            }
            foreach (var child in other.children ) {
                this.add_child(child.clone(), true);
            }

        }



        //  _____ _   ___   _____ ___ ___ ___   ___ _____ ___ _   _  ___ _____ ___
        // |_   _/_\ / __| |_   _| _ \ __| __| / __|_   _| _ \ | | |/ __|_   _/ __|
        //   | |/ _ \ (_ |   | | |   / _|| _|  \__ \ | | |   / |_| | (__  | | \__ \
        //   |_/_/ \_\___|   |_| |_|_\___|___| |___/ |_| |_|_\\___/ \___| |_| |___/

        public void process_attrib_commands() {
            if (this.tag.tag_type != TagType.root ) {
                foreach (var KeyPair in this.tag.attribs ) {
                    var attr = KeyPair.Value;
                    var name = KeyPair.Key;
                    switch(attr.type) {
                        case AttribType.command: {
                            switch(name) {
                                case "@separator": {
                                    TagUtils.create_tags_from_tstrs(this);
                                    break;
                                }
                                default: {
                                    break;
                                }
                            }
                            break;
                        }
                        default: {
                            break;
                        }
                    }
                }
            }
            foreach (var child in this.children ) {
                child.process_attrib_commands();
            }
        }



        //pub void apply_ops(TagBranch func_listfn) {
        //    this.apply_ops(func_list)
        //}



        //pub void apply_ops_to_all(TagBranch func_listfn) {
        //    this.apply_ops_to_all(func_list)
        //}


        public void clear_vars(int depth) {
            var skip_tags  = new Dictionary<string,bool>  {
                {"script"     ,true},
                {"void"       ,true},
                {"void_exact" ,true},
                {"style"      ,true}
            };

            if (skip_tags.ContainsKey(this.tag.name.ToLower()) ) {
                return;
            }

            if (this.tag.tag_type != TagType.root ) {
                foreach (var KeyPair in this.tag.attribs ) {
                    var attr = KeyPair.Value;
                    var name = KeyPair.Key;
                    if (attr.type == AttribType.variable ) {
                        if (!attr.value.ToLower().Contains("@html") ) {
                            this.tag.attribs.Remove(name);
                        }
                    } else if (attr.value.Contains("{") && depth < 0) {
                        if (!attr.value.ToLower().Contains("@html") ) {
                            var new_value  = Utils.Utils.clear_between(attr.value, "{", "}");
                            // CLEAN UP TRAILING OR CONDITIONS... SUPER HACKY.. PROBABLY NEED TO REWRITE
                            // AAAALL OF THE VAR/CLASS REPLACMENT FUNCTIONS :(
                            if (new_value.Contains("| ")) new_value = new_value.Replace("| ", " ");
                            if (new_value.EndsWith("|")) new_value = new_value.Substring(0,new_value.Length-1);
                            this.tag.attribs[name] = new Attribs {
                                value = new_value,
                                type = AttribType.standard,
                                options = attr.options,
                            };
                        }
                    }
                }
                // CLEAR TEXT, VARS NOT REPLACED CURRENTLY... I DONT THINK
                if (this.tag.tag_type == TagType.text ) {
                    if (!this.tag.tstr.ToLower().Contains("@html") ) {
                        this.tag.tstr = Utils.Utils.clear_between_with_regex(this.tag.tstr, "{", "}", "[^$].*");
                    }
                }
            }
            if (depth != 0 ) {
                foreach (var child in this.children ) {
                    child.clear_vars(depth - 1);
                }
            }
        }


        // Replace variables in node with values from orig_node
        // Commonly called after a node has been templatized (templates filled in)
        public void replace_vars(TagBranch orig_node) {
            var skip_tags  = new Dictionary<string,bool> {
                {"script", true},
                {"void", true},
                {"void_exact", true}
            };

            var var_regex        = new Regex("((?<prevar>[^ ]+)[|])?[{](?<vars>[^ ]+)[}]");
            var orig_attribs     = orig_node.tag.attribs.clone();
            var orig_var_attribs = orig_node.tag.get_attribs_bytype(AttribType.variable);
            var self_tag         =  this.tag;
            if (skip_tags.ContainsKey(self_tag.name.ToLower().Trim()) ) {
                // This tag (html elem) is in the skip list, so skip it
                return;
            }

            // REPLACE ATTRIBS
            // USING KEY TO LIST TO SUPPORT REPLACMENTS I>E> NON ENUMERATION
            var akeys = self_tag.attribs.Keys.ToList();
            foreach (var name in akeys ) {
                var attr = self_tag.attribs[name];

                if (attr.type == AttribType.variable ) {
                    if (orig_attribs.ContainsKey(name) ) {
                        self_tag.attribs.Remove(name);
                        self_tag.attribs[orig_attribs[name].value] = orig_attribs[name];
                        self_tag.attribs[orig_attribs[name].value].value = self_tag.attribs[orig_attribs[name].value].value.Trim();
                    }
                } else if (orig_var_attribs.Keys.Count > 0) {
                    string new_attrib_value = attr.value;
                    // DOES ATTRIBUTE CONTAIN A VARIABLE
                    if (attr.value.Contains("{")) {
                        // Replace special variable {default} with original node first node text, if it is text
                        var matches = var_regex.Matches(new_attrib_value);
                        foreach (var match in matches.ToList()) {
                            var prevars      = match.Groups["prevar"].Value;
                            var matched_vars = match.Groups["vars"].Value;
                            var varParts = matched_vars.Split('|');
                            var notReplaced = true;
                            foreach(var vp in varParts) {
                                var varKey = "{"+vp.ToLower()+"}";

                                if (orig_var_attribs.ContainsKey(varKey)) {
                                    var source_attrib = orig_var_attribs["{"+vp.ToLower()+"}"];
                                    new_attrib_value = new_attrib_value.Replace(match.Value, source_attrib.value);
                                    notReplaced = false;
                                    break; // GET FIRST FOUND
                                }
                            }
                            if (prevars.Length > 0 && notReplaced) {
                                new_attrib_value = new_attrib_value.Replace(match.Value, prevars);
                            }
                        }
                    }
                    // REPLACE VALUE WITH CHANGES
                    attr.value = new_attrib_value;
                }
                if (Regex.Match(attr.value, @"[{]\s*default\s*[}]").Success) {
                    if (orig_node.children.Count > 0 && orig_node.children[0].tag.tag_type == TagType.text ) {
                        attr.value = orig_node.children[0].tag.tstr.Trim();
                    }
                }

            }

            // REPLACE TEXT
            foreach (var key in orig_attribs.Keys) {
                var attr = orig_attribs[key];
                if (attr.type == AttribType.variable ) {
                    if (!attr.value.ToLower().Contains("@html") ) {
                        self_tag.tstr = self_tag.tstr.Replace(key, attr.value);
                    }
                }
            }

            // REPLACE {default} variable when it is in the text of a node not the attributes
            if (self_tag.tstr.Contains("{default}")) {
                if (orig_node.children.Count > 0 && orig_node.children[0].tag.tag_type == TagType.text ) {
                    self_tag.tstr = self_tag.tstr.Replace("{default}", orig_node.children[0].tag.tstr.Trim());
                }
            }

            replace_tag_str_vars(this, self_tag, orig_attribs);
            // REPLACE CHILDRENS VARIABLES, ALLOWS REPLACEMENT INTO CHILD NODES FROM TEMPLATE
            foreach (var child in this.children ) {
                child.replace_vars(orig_node);
            }

        } // END REPLACE_VARS FUNCTION



        // Process Tag text for variables
        void replace_tag_str_vars(TagBranch self, TagData self_tag, Dictionary<string,Attribs> orig_attribs  ) {
            // AFTER DEFAULTS REPLACED WE CAN RUN THIS
            // TODO: THIS WHOLE SECTION BELOW NEEDS TO GO SOMEWHERE ELSE AND CLEAN UP THIS WHOLE FUNTION :C
            if (self_tag.tstr.Contains("{")) {
                var varex  = new Regex("{[$]?[a-zA-Z:|]+}");
                var var_matches  =  Utils.Utils.make_location_groups(varex.Matches(self_tag.tstr).ToList(), 0);
                foreach (var vmatch in var_matches ) {
                    var vmatch_str = self_tag.tstr[vmatch.start..vmatch.end];
                    // +1 and -1 exclude { and } in the match
                    var varstr    =  self_tag.tstr[(vmatch.start + 1)..(vmatch.end - 1)];
                    var varslist  =  varstr.Split("|");
                    var value_set =  false;
                    // Prefer user supplied variable values

                    foreach (var vars in varslist ) {
                        if (orig_attribs.ContainsKey("{" + vars + "}")) {
                            self_tag.tstr = self_tag.tstr.Replace(vmatch_str, orig_attribs[("{" + vars + "}")].value);
                            value_set = true;
                            break;
                        }
                    }
                    // Replace with builtin values
                    if (!value_set) {
                        foreach (var vars in varslist ) {
                            if (vars == ":DATE:" ) {
                                self_tag.tstr = self_tag.tstr.Replace(vmatch_str, DateTime.Now.ToString());
                                break;
                            } else if (vars.StartsWith("$") ) {
                                var tparent  =  this.parent;
                                if (tparent != null ) {
                                    var tattribs  =  tparent.tag.attribs;
                                    var vattrname  =  vars[1..];
                                    if (tattribs.ContainsKey(vattrname) ) {
                                        self_tag.tstr = self_tag.tstr.Replace(vmatch_str, tattribs[vattrname].value);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }



        public bool merge_with_template(TagBranch template_node, StyleSheet style_sheet) {
            // PREP CLASSES
            this.apply_local_style_tags(style_sheet);
            // MERGE ATTRIBS
            return this.merge_attribs(template_node.tag.attribs, false);
        }


        // APPLY ANY INTERNAL ATTRIB COMMANDS
        public void apply_internal_attrib_commands(bool isFinalCall = false) {
            if (isFinalCall) {
            } else {
                // TBD
            }
        }


        public int locate_default_attrib_merge_tag(int found_tags) {
            var local_count  =  found_tags;

            var i = 0;
            foreach (var child in this.children ) {
                var start_count  =  local_count;
                if (child.tag.internal_attribs.ContainsKey("__attr_default") ) {
                    local_count += 1;
                    break;
                } else {
                    local_count = child.locate_default_attrib_merge_tag(local_count);
                }
                if (local_count > start_count && i > 1 ) {
                    if (this.children[i-2].tag.sub_type == TagSubType.command ) {
                        this.children[i].tag.no_merge_attribs = true;
                    }
                }
                i++;
            }

            return local_count;
        }


        public void apply_local_style_tags(StyleSheet style_sheet) {
            this.make_class_replacement(style_sheet);
            this.make_class_insert_by_tag(style_sheet);
            foreach (var child in this.children ) {
                child.apply_local_style_tags(style_sheet);
            }
        }

        void make_class_replacement(StyleSheet style_sheet) {
            if (!this.tag.attribs.ContainsKey("class")) return;
            var class_attrib = this.tag.attribs["class"];
            var top_classes  = class_attrib.value.Split(" ");

            var index  = 0;
            foreach (var tclass in top_classes ) {
                var mutTClass = tclass;
                // REPLACE ENDING % SIGNS WITH "per"
                if (mutTClass.EndsWith("%")) mutTClass = mutTClass.Substring(0,mutTClass.Length-1) + "per";
                // RETAIN OR OPERATORS IN CLASSES UNTIL VARIABLE REPLACEMENT
                var ptclasses = mutTClass.Split('|');
                for (int i = 0; i < ptclasses.Length; i++) {
                    var ptclass = ptclasses[i];
                    if (style_sheet.has_class(ptclass) ) {
                        var cclass  =  style_sheet.get_class(ptclass, false);
                        var cid = (!cclass.global && ptclass != "active" ) ?  "-ss" + cclass.id.ToString()  :  "" ;
                        ptclasses[i] = ptclass + cid;
                    }
                }
                top_classes[index] = string.Join('|', ptclasses).Trim();
                index++;
            }

            var updated_classes  = string.Join(' ', top_classes).Trim();
            if (this.tag.attribs.ContainsKey("class") && updated_classes.Length > 0 ) {
                this.tag.attribs["class"].value = updated_classes;
            }
        }

        // Add the matching class id to any element that has a local command in the style sheet
        void make_class_insert_by_tag(StyleSheet style_sheet) {
            var class_attrib = this.tag.attribs.ContainsKey("class") ? this.tag.attribs["class"] : new Attribs(); // Attribs  new or();
            var top_classes  =  class_attrib.value.Split(" ");
            if (style_sheet.has_class(this.tag.name) ) {
                var cclass  =  style_sheet.get_class(this.tag.name, true);
                if (cclass.local && cclass.id > 0 ) {
                    var class_id  =  "ss" + cclass.id.ToString();
                    if (top_classes[0].Length <= 0 ) {
                        this.tag.attribs["class"] = new Attribs {
                            value = class_id,
                            type = AttribType.standard
                        };
                    } else {
                        // NEW CLASS CREATION AND MERGING
                        var new_class  =  cclass.clone();
                        new_class.names = new List<string>{"." + this.tag.name + "-sm"};
                        top_classes.Prepend(new_class.names[0][1..] + "-ss" + cclass.id.ToString());
                        this.tag.attribs["class"].value = string.Join(" ", top_classes);
                        style_sheet.insert_class(new_class, cclass.sheet_index);
                    }
                }
            }
        }



        // Merge attributes provided with the current nodes tag attributes
        public bool merge_attribs(Dictionary<string,Attribs> attribs , bool only_if_default) {
            var applied  =  false;
            var search_below  =  this.tag.tag_type == TagType.root || this.tag.tag_type == TagType.text || this.tag.no_merge_attribs;

            if (search_below ) {
                foreach (var child in this.children ) {
                    if (child.tag.name.ToLower() == "style" || child.tag.name.ToLower() == "script" ) {
                        continue;
                    }
                    applied = child.merge_attribs(attribs, this.tag.no_merge_attribs || only_if_default);
                    if (applied && !only_if_default ) {
                        break;
                    }
                }
            } else if (only_if_default && this.tag.internal_attribs.ContainsKey("__attr_default") ) {
                this.tag.merge_attribs(attribs, false);
                applied = true;
            } else if (!only_if_default ) {
                this.tag.merge_attribs(attribs, false);
                applied = true;
            }
            // MERGE IMPORTANT ATTRIBS NOMATTER WHAT
            // HAPPENS ONLY WHEN SELF NOT THE DEFAULT
            if (this.tag.no_merge_attribs ) {
                this.tag.merge_attribs(attribs, true);
            }

            return applied;
        }


        public List<TagBranch> exclude_children_by_id(Dictionary<int,string> ids ) {
            var filtered_list  = new List<TagBranch>();
            foreach (var child in this.children ) {
                var cid  =  child.tag.get_id();
                if (!ids.ContainsKey(cid) && cid != 0 ) {
                    filtered_list.Add(child);
                }
            }
            return filtered_list;
        }



        // Print all of a tag node
        //pub void print_all() {
        //  switch(self) {
        //    TagBranch { this.print_all() }
        //    else { "" }
        //  }
        //}



        // CLONE A TAG NODE AND RETURN THE RESULT AS A REF
        //public TagBranch clone_to_ref() {
        //    var cloned  = new  TagBranch{
        //        iter_idx    = this.iter_idx,
        //        tag         = this.tag.clone(),
        //        slot_map    = new Dictionary<string,List<TagBranch>>(this.slot_map),
        //        indent      = this.indent,
        //        closing_tag = this.closing_tag.clone(),
        //        commands    = new List<BranchCommand>(this.commands),
        //        parent      = this.parent,
        //    };
        //    // ADDING CHILDREN CREATE SLOT MAPS
        //    foreach (var child in this.children ) {
        //        cloned.add_child(child.clone(), false);
        //    }
        //    return cloned;
        //}



        // INSERT INT CHILDRENO THE TAG NODE
        // NOTE* NEED TO UPDATE FUNCTION TO USE THE INDEX PARAMETER AT SOME POINT
        public int insert_at(List<TagBranch> childs, int index) {
            var new_index  =  index;
            // ERROR CHECKS
            if (childs.Count <= 0 ) {
                return index;
            }
            if (index < 0 || index > this.children.Count ) {
                return index;
            }

            for (var i = 0; i < childs.Count; i++ ) {
                this.children.Insert(i + index, childs[i]);
            }
            return new_index + childs.Count - 1;
        }



        // INSERT INT CHILDRENO THE TAG NODE
        // INSERT THE CHILDREN BASED ON THEIR TAG NAME TO THE SLOT NAME THAT MATCHES
        public void insert_into_by_tag_name(List<TagBranch> childs, Dictionary<int,string> skip_list ) {
            // ERROR CHECKS
            if (childs.Count <= 0 ) {
                return;
            }

            var skip_first  =  this.has_default_var;

            for (var i = 0; i < this.children.Count; i++) {
                var tag_info  =  this.children[i].tag;
                var tag_attr_name = tag_info.attribs.ContainsKey("name") ? tag_info.attribs["name"].value.ToLower() : "";
                if (tag_info.name.ToLower() == "slot" && tag_attr_name.Length > 0 ) {
                    var copied_first  =  false;
                    var insert_index  =  1;
                    foreach (var child in childs ) {
                        if (tag_attr_name == child.tag.name.ToLower() ) {
                            if (skip_first ) {
                                skip_first = false;
                                continue;
                            }
                            skip_list[child.tag.get_id()] = child.tag.name;
                            if (!copied_first ) {
                                copied_first = true;
                                this.children[i].copy(child);
                            } else {
                                this.children.Insert(i + insert_index, child);
                                insert_index += 1;
                            }
                        } else {
                            this.children[i].insert_into_by_tag_name(childs, skip_list);
                        }
                    }
                } else {
                    this.children[i].insert_into_by_tag_name(childs, skip_list);
                }
            }
        }



        // INSERT INT CHILDRENO THE TAG NODE
        // NOTE* NEED TO UPDATE FUNCTION TO USE THE INDEX PARAMETER AT SOME POINT
        public void insert_into(List<TagBranch> childs, string tag_name, Dictionary<string,Attribs> attribs , bool has_default, bool default_var_set) {
            // ERROR CHECKS
            if (childs.Count <= 0 ) {
                return;
            }
            if (tag_name.Length <= 0 ) {
                return;
            }
            var skip_first_child  =  (has_default || this.has_default_var) && !default_var_set;
            var cstart = (skip_first_child ) ?  1  :  0 ;
            var copied_first  =  false;
            var insert_index  =  1;
            var no_mapping  =  attribs.Count <= 0;

            for (var i = 0; i < this.children.Count; i++ ) {
                var tag_info  =  this.children[i].tag;
                if (tag_info.name.ToLower() == tag_name.ToLower() ) {
                    if (TagParsing.contains_attrib_map(tag_info.attribs, attribs) || no_mapping ) {
                        for (var j = cstart; j < childs.Count; j++ ) {
                            if (!copied_first ) {
                                copied_first = true;
                                this.children[i].copy(childs[j]);
                            } else {
                                this.children.Insert(i + insert_index, childs[j]);
                                insert_index += 1;
                            }
                        }
                        break;
                    }
                } else {
                    this.children[i].insert_into(childs, tag_name, attribs, skip_first_child,default_var_set);
                }
            }
            return;
        }



        // INSERT INT CHILDRENO THE TAG NODE
        // NOTE* NEED TO UPDATE FUNCTION TO USE THE INDEX PARAMETER AT SOME POINT
        public void insert_into_where(List<TagBranch> childs, string tag_name, Dictionary<string,Attribs> attribs ) {
            // ERROR CHECKS
            if (childs.Count <= 0 ) {
                return;
            }
            if (tag_name.Length <= 0 ) {
                return;
            }

            var skip_first = this.has_default_var;
            var no_mapping = attribs.Count <= 0;

            for (var i=0; i < this.children.Count; i++ ) {
                var tag_info  =  this.children[i].tag;
                if (tag_info.tag_type != TagType.start && tag_info.tag_type != TagType.single ) {
                    continue;
                }
                if (tag_info.name.ToLower() == tag_name.ToLower() ) {
                    if (TagParsing.contains_attrib_map(tag_info.attribs, attribs) || no_mapping ) {
                        foreach (var child in childs ) {
                            if (skip_first ) {
                                skip_first = false;
                                continue;
                            }
                            child.merge_with_template(this.children[i], this.style_sheet);
                        }
                        this.children.RemoveAt(i);
                        insert_node_by_attrib_options(this, childs, tag_name, i);
                        break;
                    } else {
                        this.children[i].insert_into_where(childs, tag_name, attribs);
                    }
                } else {
                    this.children[i].insert_into_where(childs, tag_name, attribs);
                }
            }
        }



        // INSERT THE NODE OR ITS CHILDREN BASED ON THE ATTRIB OPTIONS IN ITS TAG
        void insert_node_by_attrib_options(TagBranch parent, List<TagBranch> childs, string tag_name, int index) {
            var cindex  =  index;
            var i  =  0;
            //    tag_type: text
            var ablank  = new  TagData{
                name   = "",
                tstr   = "\n",
                source = ""
            };
            ablank.tstr = "\n" + parent.previous_indent(index);
            ;
            if (cindex < 0 ) {
                return;
            }
            if (cindex > parent.children.Count ) {
                cindex = parent.children.Count;
            }

            foreach (var child in childs ) {
                i += 1;
                if (child.tag.attribs[tag_name].options.Contains("s") ) {
                    foreach (var inner_child in child.children ) {
                        parent.children.Insert(cindex, inner_child);
                        parent.children[cindex].tag.attribs.Remove(tag_name);
                        cindex += 1;
                    }
                    parent.merge_with_template(child, parent.style_sheet);
                } else {
                    parent.children.Insert(cindex, child);
                    parent.children[cindex].tag.attribs.Remove(tag_name);
                    cindex += 1;
                    if (cindex > index && i < childs.Count ) {
                        parent.children.Insert(cindex, new TagBranch {
                                tag = ablank.clone()
                            });
                        parent.children.Last().parent = parent;
                    }
                    cindex += 1;
                }
            }
        }



        string previous_indent(int index) {
            var indent  =  "";

            if (this.children.Count > 0 && index > 0 && index - 1 <= this.children.Count ) {
                var value  =  this.children[index - 1].tag.tstr;
                if (value.Length > 0 && value.Trim().Length == 0 ) {
                    indent = value.Replace("\n", "");
                }
            }

            return indent;
        }



        public Dictionary<string,bool> get_purge_slots() {
            var purge_slots  =  new Dictionary<string,bool>();
            foreach (var child in this.children ) {
                if (child.tag.name == "slot" ) {
                    var sname  =  child.tag.attribs["name"].value;
                    if (child.children.Count <= 0 && sname.Length > 0 ) {
                        purge_slots[sname] = true;
                    }
                }
                var cpurges  =  child.get_purge_slots();
                foreach (var key in cpurges.Keys ) {
                    purge_slots[key] = true;
                }
            }

            return purge_slots;
        }



        // INSERT INT CHILDRENO THE TAG NODE
        // NOTE* NEED TO UPDATE FUNCTION TO USE THE INDEX PARAMETER AT SOME POINT
        public Dictionary<string,bool> remove_slots(bool keep_contents) {
            var index  =  0;
            var remove_list  =  new List<int>();
            var purge_slots  =  new Dictionary<string,bool>();
            for (var i = 0; i < this.children.Count; i++) {
                var ctag  =  this.children[i].tag;
                if (ctag.name == "slot" ) {
                    remove_list.Add(i);
                    if (ctag.attribs.ContainsKey("name") && this.children[i].children.Count <= 0 ) {
                        purge_slots[ctag.attribs["name"].value] = true;
                    }
                }
            }
            foreach (var i in remove_list ) {
                var ctag     =  this.children[i].tag;
                var tag_type =  ctag.tag_type;
                index = this.insert_at(this.children[i].children, i + 1);
                this.children.RemoveAt(i);
                //   FIRST REMOVE BLANK ENDINGS
                if (tag_type == TagType.start ) {
                    if (index - 1 < this.children.Count
                        && Utils.Utils.empty_with_newline(this.children[index - 1].tag.tstr) ) {
                        this.children.RemoveAt(index - 1);
                    }
                    if (i < this.children.Count && Utils.Utils.empty_with_newline(this.children[i].tag.tstr) ) {
                        this.children.RemoveAt(i);
                    }
                } else if (tag_type == TagType.end ) {
                    if (i < this.children.Count && Utils.Utils.empty_with_newline(this.children[i].tag.tstr) ) {
                        var orig  =  this.children[i].tag.tstr;
                        this.children[i].tag.tstr = "\n" + orig.Split("\n")[1];
                    }
                }
            }

            foreach (var child in this.children ) {
                var pslots  =  child.remove_slots(keep_contents);
                foreach (var key in pslots.Keys) {
                    purge_slots[key] = true;
                }
            }
            return purge_slots;
        }



        public Dictionary<string,bool> locate_dep_slots() {
            var purge_slots  =  new Dictionary<string,bool>();
            for (var i = 0; i < this.children.Count; i++ ) {
                var ctag  =  this.children[i].tag;
                if (ctag.name == "slot" ) {
                    if (ctag.attribs.ContainsKey("name") && this.children[i].children.Count <= 0 ) {
                        purge_slots[ctag.attribs["name"].value] = true;
                    }
                }
            }
            foreach (var child in this.children ) {
                var pslots  =  child.locate_dep_slots();
                foreach (var key in pslots.Keys) {
                    purge_slots[key] = true;
                }
            }
            return purge_slots;
        }



        // REMOVE SLOT DEPENDENCIES
        public void remove_slots_deps(Dictionary<string,bool> purge_slots ) {
            var remove_list  = new List<int>();
            var depends_list = new Dictionary<string,int>();

            // FIND ALL TAGS WITH A DEPENDS ATTRIBUTE
            for (var i = 0; i < this.children.Count; i++) {
                var ctag = this.children[i].tag;
                if (ctag.attribs.ContainsKey("depends") && ctag.attribs["depends"].value.Length > 0 ) {
                    depends_list[ctag.attribs["depends"].value] = i;
                }
            }

            // ADD CHILDREN THAT DEPEND ON SLOTS THAT REMAIN
            foreach (var key in depends_list.Keys ) {
                var child_index = depends_list[key];
                if (purge_slots.ContainsKey(key) && purge_slots[key]) {
                    remove_list.Add(child_index);
                }
            }

            // REVERSE THE LIST FOR REMOVAL SO INDEXES DON'T CHANGE
            remove_list.Reverse();

            // REMOVE ALL TAGS THAT DEPENDED ON A NON PROVIDED SLOT
            foreach (var i in remove_list ) {
                var ctag   = this.children[i].tag;
                var dvalue = ctag.attribs.ContainsKey("depends") ? ctag.attribs["depends"].value : "";
                if (dvalue.Length > 0 && purge_slots.ContainsKey(dvalue) && purge_slots[dvalue]) {
                    this.children.RemoveAt(i);
                }
            }

            // ITERATE DOWN THROUGH CHILDRENS CHILDREN
            foreach (var child in this.children ) {
                child.remove_slots_deps(purge_slots);
            }
        }




        // Add a child node from TagBranch a object
        //pub void add_child(TagBranch child, bool create_attrib) {
        //  switch(self) {
        //    TagBranch {
        //      dumb :=  new TagBranch();
        //      unsafe {
        //        dumb = self
        //        dumb.add_child(child, create_attrib)
        //      }
        //    }
        //    else {}
        //  }
        //}



        // Function to obtain the stylesheet for the node
        public StyleSheet parse_style_blocks(bool merge_up) {
            var id_assigned  =  false;
            var the_sheet  = new StyleSheet();
            if (this.style_sheet.id != -1 ) {
                the_sheet = this.style_sheet;
            }


            foreach (var child in this.children ) {
                if (child.tag.name.ToLower().Trim() == "style" && the_sheet.id == -1 ) {
                    //... this is such a hack :/

                    var style_text  =  child.get_inner();
                    the_sheet = StyleOps.parse_styles(style_text);
                    foreach (var kp in the_sheet.style_cmds ) {
                        var key = kp.Key;
                        var rule = kp.Value;
                        if (key == "@style-id" ) {
                            this.tag.id = int.Parse(rule.rvalue);
                            the_sheet.assign_id(int.Parse(rule.rvalue));
                            id_assigned = true;
                        }
                    }
                }
                if (merge_up ) {
                    var other_sheet  =  child.parse_style_blocks(merge_up);
                    the_sheet.merge(other_sheet);
                }
            }
            // GIVE CLASSES UNIQUE IDs
            if (!id_assigned ) {
                the_sheet.assign_id(the_sheet.id);
            }
            this.style_sheet = the_sheet;


            return the_sheet;
        }



        // Get the inner content of the tag... might be buggy currently
        public string get_inner() {
            var text  =  "";

            if (this.tag.tag_type == TagType.start ) {
                var bound1   =  this.tag.inner;
                var bound2   =  this.closing_tag.inner;
                var stext    =  this.tag.source;
                var inbounds =  Utils.Utils.in_bounds(stext, bound1.end, bound2.start);
                if (inbounds && bound1.end < bound2.start ) {
                    text = stext[bound1.end..bound2.start];
                }
            }

            return text;
        }



        // return bool a based on whether or not the node as a default variable in it
        public bool has_default() {
            var has_default  =  false;

            has_default = this.has_default_var;
            if (!has_default ) {
                foreach (var child in this.children ) {
                    has_default = child.has_default();
                    if (has_default ) {
                        break;
                    }
                }
            }

            return has_default;
        }
    }  // END PARTIAL CLASS
}  // END NAMESPACE
