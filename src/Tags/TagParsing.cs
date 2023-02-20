using Templification.Utils;
using Templification.Styles;
using System.Text.RegularExpressions;

namespace Templification.Tags {

    public static class TagParsing {

        // Convert html int texto a TagTree
        public static List<TagTree> parse_html_to_tag_tree(string source, CmdLineOptions options) {
            var tag_data = parse_html_to_tag_data(source, options);
            return tag_array_to_tag_tree(tag_data, source);
        }

        //   Parse html int TagData texto
        public static List<TagData> parse_html_to_tag_data(string source, CmdLineOptions options) {
            var findex       =  0;
            var tag_groups   =  new List<TagGroup>();
            var pat_tag_any  =  "(\\s*<.*?>)";
            var any_ex  = new Regex(pat_tag_any, RegexOptions.Singleline);

            var special_groups = collect_preprocess_blocks(source, options);
            var sub_source = "";
            var c_groups = new List<TagGroup>();
            for (var i = 0; i < (special_groups.Count / 2); i++) {
                // ------------------------------------------------------------
                // LOCATE TAGS
                var matchstr = source[special_groups[(i * 2)].start..special_groups[(i * 2) + 1].end];

                sub_source   = source[findex..special_groups[(i * 2)].start];
                if (sub_source != "\n" ) {
                    var mmatchs = any_ex.Matches(sub_source).ToList();
                    c_groups  =  TagGroup.make_location_groups(mmatchs, findex, sub_source.Length);
                    tag_groups.AddRange(c_groups);
                }
                tag_groups.Add(special_groups[i * 2]);
                tag_groups.Add(special_groups[(i * 2) + 1]);
                findex = special_groups[(i * 2) + 1].end;
            }
            sub_source = source[findex..source.Length];
            var csharpMatches = any_ex.Matches(sub_source).ToList();
            c_groups = TagGroup.make_location_groups(csharpMatches, findex, sub_source.Length);
            tag_groups.AddRange(c_groups);

            var tag_array = collect_tags(source, tag_groups);

            return tag_array;
        }

        // Collect all of the tags that can be found is int sourceo an List<TagData> array
        public static List<TagData> collect_tags(string source, List<TagGroup> groups) {
            var all_tags  =  new List<TagData>();

            foreach (var group in groups ) {
                var tag  =  create_tag_from_string(source, group);
                all_tags.AddRange(tag);
            }
            return all_tags;
        }

        // You have no ability to manage time
        public static List<TagData> create_tag_from_string(string source, TagGroup group) {
            var sub_source  =  source[group.start..group.end];

            var pat_script_start  = @"(?<"+APP.TAG_SPACE+@">\s*)?<(?<"+APP.TAG_NAME+@">script)\s*(?<"+APP.TAG_ATTRIBS+@">.*)>";
            var pat_script_end    = @"(?<"+APP.TAG_SPACE+@">\s*)?</(?<"+APP.TAG_NAME+@">script)\s*>";
            var pat_tag_start     = @"(?<"+APP.TAG_SPACE+@">\s*)?<(?<"+APP.TAG_NAME+@">[a-zA-Z0-9_\-]+)\s*(?<"+APP.TAG_ATTRIBS+@">.*)>";
            var pat_tag_end       = @"(?<"+APP.TAG_SPACE+@">\s*)?</(?<"+APP.TAG_NAME+@">\w*)\s*>";
            var pat_tag_single    = @"(?<"+APP.TAG_SPACE+@">\s*)?<(?<"+APP.TAG_NAME+@">\w*)\s*(?<"+APP.TAG_ATTRIBS+@">[^>]*)/>";
            var pat_command       = @"^(?<space>\s*)?{[#:/](?<"+APP.TAG_NAME+@">\w*)\s*(?<"+APP.TAG_ATTRIBS+@">[^><]*)}";
            var pat_details_start = @"(?<"+APP.TAG_SPACE+@">\s*)?<(?<"+APP.TAG_NAME+@">"+APP.SUB_TYPE_FILEDETAILS.ToUpper()+@")\s*(?<"+APP.TAG_ATTRIBS+@">.*)>";
            var pat_details_end   = @"(?<"+APP.TAG_SPACE+@">\s*)?</(?<"+APP.TAG_NAME+@">"+APP.SUB_TYPE_FILEDETAILS.ToUpper()+@")\s*>";

            var end_ex           = new Regex(pat_tag_end,       RegexOptions.Singleline);
            var single_ex        = new Regex(pat_tag_single,    RegexOptions.Singleline);
            var start_ex         = new Regex(pat_tag_start,     RegexOptions.Singleline);
            var script_start_ex  = new Regex(pat_script_start,  RegexOptions.Singleline);
            var script_end_ex    = new Regex(pat_script_end,    RegexOptions.Singleline);
            var details_start_ex = new Regex(pat_details_start, RegexOptions.Singleline);
            var details_end_ex   = new Regex(pat_details_end,   RegexOptions.Singleline);
            var command_ex       = new Regex(pat_command);

            var tags  =  new List<TagData>();
            var tag  = new TagData{
                tag_type = TagType.empty,
            };

            var was_matched  = false;
            if (group.sub_type != TagSubType.cshtml && group.sub_type != TagSubType.comment) {
                if (!was_matched) was_matched = attempt_match(TagType.start,  script_start_ex,  sub_source, tag);
                if (!was_matched) was_matched = attempt_match(TagType.end,    script_end_ex,    sub_source, tag);
                if (!was_matched) was_matched = attempt_match(TagType.single, single_ex,        sub_source, tag);
                if (!was_matched) was_matched = attempt_match(TagType.start,  start_ex,         sub_source, tag);
                if (!was_matched) was_matched = attempt_match(TagType.end,    end_ex,           sub_source, tag);
                if (!was_matched) was_matched = attempt_match(TagType.start,  details_start_ex, sub_source, tag);
                if (!was_matched) was_matched = attempt_match(TagType.end,    details_end_ex,   sub_source, tag);
            }

            if (!was_matched && sub_source.Trim().Length > 0 ) {
                // MAKE THIS SPLIT UP TEXT BASED ON COMMANDS AND CREATE TAGS FOR EACH
                var cmd_matches = command_ex.Matches(sub_source).ToList();

                if (cmd_matches.Count > 0 ) {
                    var text_groups  =  Utils.Utils.make_location_groups(cmd_matches, sub_source.Length);
                    foreach (var tgroup in text_groups ) {
                        tags.Add(new TagData{
                                tstr = sub_source[tgroup.start..tgroup.end],
                                tag_type = TagType.text
                            });
                        if (command_ex.Match(tags.Last().tstr).Success) {
                            tags.Last().sub_type = TagSubType.command;
                        }
                    }
                } else {
                    tag.tstr = sub_source;
                    tag.name = APP.TEXT_NAME;
                    var tsub  =  sub_source.ToLower();
                    // :TODO: MAKE THIS  LESS HACKY
                    if (tsub.Contains("@html") && tsub.Contains("class") ) {
                        process_razor_cmd(sub_source, tag);
                    }
                    tag.tag_type = TagType.text;
                    tag.sub_type = group.sub_type;
                    var index  = sub_source.IndexOf(APP.TEXT_NEWLINE);
                    if  (index > 0) {
                        tag.new_line = APP.TEXT_NEWLINE;
                    }
                }
            } else if (tag.sub_type == TagSubType.cshtml){
                tag.tstr = sub_source;
                tag.name = APP.TEXT_NAME;
                var tsub  =  sub_source.ToLower();
                if (tsub.Contains("class")) {
                    process_razor_cmd(sub_source, tag);
                }
                tag.tag_type = TagType.text;
                var index  = sub_source.IndexOf(APP.TEXT_NEWLINE);
                if  (index > 0) {
                    tag.new_line = APP.TEXT_NEWLINE;
                }
            } else if (tag.tag_type != TagType.text ) {
                if (group.start == 0 ) {
                    tag.new_line = APP.TEXT_NEWLINE;
                }
                tag.source = source;
                tag.outer = new Re_group {
                    start = group.start,
                    end = group.end
                };
                tag.inner = tag.outer;
            }

            tags.Add(tag);
            return tags;
        }



        public static void process_razor_cmd(string sub_source, TagData tag) {
            var razor_class_pat  = "{\\s*[\"]class[\"]\\s*,\\s*[\"](?<classes>.*)[\"]}";
            var razor_ex  = new Regex(razor_class_pat);
            var rgroups  =  Utils.Utils.make_location_groups(razor_ex.Matches(sub_source).ToList(), 0);
            foreach (var rgroup in rgroups ) {
                var razormatches = razor_ex.Match(sub_source[rgroup.start..rgroup.end]);
                var classes  =  razormatches.Groups["classes"].Value;
                classes = "class=" + "'" + classes + "'";
                var class_attribs = attrib_str_to_map(classes);
                tag.attribs = new Dictionary<string,Attribs>(class_attribs);
            }
        }

        // Attempt to match the string provided with the provided regex.
        // If a match occcurs then create a tag and return it via the tag ref
        static bool attempt_match(TagType mtype, Regex regx , string block, TagData tag) {
            var has_match  =  false;

            var rmatch = regx.Match(block);
            has_match = rmatch.Success;
            if (has_match) {
                var name       =  rmatch.Groups[APP.TAG_NAME].Value;
                var attrib_out =  attrib_str_to_map(rmatch.Groups[APP.TAG_ATTRIBS].Value);
                var wspace     =  rmatch.Groups[APP.TAG_SPACE].Value;

                tag.name = name;
                tag.attribs = new Dictionary<string, Attribs>(attrib_out);
                // INT MOVE INTERNAL ATTRIBS TO DIFFERENT MAP
                foreach (var keyPair in tag.attribs ) {
                    var aname = keyPair.Key;
                    var attr  = keyPair.Value;
                    if (aname.StartsWith(APP.PREFIX_INTERNAL_ATTRIB) ) {
                        tag.internal_attribs[aname] = attr;
                        if (aname == APP.ATTRIB_TEMPLATE) {
                            tag.sub_type = TagSubType.template;
                        }
                        tag.attribs.Remove(aname);
                    }
                }

                tag.tag_type = mtype;
                if (name.ToLower().Trim() == APP.SUB_TYPE_STYLE.ToLower()) {
                    tag.sub_type = TagSubType.style;
                } else if (name.ToLower().Trim() == APP.SUB_TYPE_SCRIPT.ToLower()) {
                    tag.sub_type = TagSubType.script;
                } else if (name.ToLower().Trim() == APP.SUB_TYPE_VOIDEX.ToLower()) {
                    tag.sub_type = TagSubType.void_exact;
                } else if (name.StartsWith(APP.PREFIX_TEMPLATE.ToLower())) {
                    tag.sub_type = TagSubType.template;
                } else if (name.ToLower().Trim() == APP.SUB_TYPE_FILEDETAILS.ToLower()) {
                    tag.sub_type = TagSubType.filedetails;
                }

                if (wspace.Contains(APP.TEXT_NEWLINE) ) {
                    tag.new_line = APP.TEXT_NEWLINE;
                }
            }
            return has_match;
        }

        // Collect tag attributes in a string into a map of name to value
        static Dictionary<string,Attribs> attrib_str_to_map(string attribs) {
            var mapping  =  new Dictionary<string,Attribs>();
            if (attribs.Length <= 0 ) {
                return mapping;
            }

            var reggy  = new Regex("([^\"'= ]+)[=][{\"][^\"]*[\"}]");
            var reggy2 = new Regex("([^\"'= ]+)[=][{'][^']*['}]");
            var int_locations  = reggy.Matches(attribs);
            var int_locations2 = reggy2.Matches(attribs);
            var matches  = Utils.Utils.match_strings_from_location_ints(int_locations.ToList(), attribs, true);
            // Last param = false because we already gathered nonconforming entries in previous call
            var matches2 = Utils.Utils.match_strings_from_location_ints(int_locations2.ToList(), attribs, false);

            matches.AddRange(matches2);

            if (matches.Count <= 0 ) {
                var empty_rex  = new Regex("\\s+");
                matches = empty_rex.Replace(attribs, " ").Split(" ").ToList();
            }

            foreach (var item in matches ) {
                if (item.Trim().Length <= 0 ) {
                    continue;
                }
                var value     =  "";
                var options   =  "";
                var attr_type =  AttribType.standard;
                var parts     =  item.SplitNth("=", 1);
                var attr_name =  parts[0].Trim();
                var key       =  attr_name.ToLower();

                value = (parts.Length > 1) ? parts[1] : "";
                if (value.Length <= 0 ) {
                    value = key;
                } else {
                    value = value.TrimStart(new char[]{'\'','"'}).TrimEnd(new char[]{'\'','"'}).Trim();
                }
                var percent_index  = key.IndexOf(APP.ATTRIB_OPTION_FLAG);
                if (key.StartsWith("@") ) {
                    attr_type = AttribType.command;
                } else if (percent_index > -1 ) {
                    var key_and_options  =  parts[0].SplitNth(APP.ATTRIB_OPTION_FLAG, 1);
                    key = key_and_options[0].Trim();
                    options = key_and_options.Length > 1 ? key_and_options[1] : "";
                } else if (key.StartsWith("{")) {
                    attr_type = AttribType.variable;
                } else {
                }

                mapping[key] = new Attribs {
                    Value = value,
                    Name = attr_name,
                    options = options,
                    type = attr_type,
                };
            }

            return mapping;
        }

        // Given a tag array that is IN ORDER create a tree based on starts and ends
        public static List<TagTree> tag_array_to_tag_tree(List<TagData> tags, string source) {
            var start_tags   = new Dictionary<string,List<TagBranch>>();
            TagBranch current_node;
            var final_trees = new List<TagTree>();

            var root = new TagBranch{
                tag = new TagData {
                    name = "root",
                    tag_type = TagType.root,
                }
            };
            current_node = root;
            foreach (var tag in tags ) {
                // ADD TAG TO MAPPING IF IT IS A START TAG
                var name = tag.name;
                switch(tag.tag_type) {
                    case TagType.start: {
                        // COLLECT THIS TAG
                        ensure_map_has_array(start_tags, name);
                        var new_node  = new TagBranch{
                            tag = tag,
                        };
                        new_node.parent = current_node;
                        current_node.add_child(new_node, true);
                        current_node = new_node;
                        start_tags[name].Add(new_node);
                        break;
                    }
                    case TagType.single:
                    case TagType.text: {
                        current_node.add_child_from_data(tag, true);
                        break;
                    }
                    case TagType.end: {
                        if (start_tags.ContainsKey(name) ) {
                            var startTagArray = start_tags[name];

                            if (startTagArray.Count > 0 ) {
                                var stag = startTagArray.Last();

                                current_node = stag;
                                current_node.closing_tag = tag;
                                current_node.tag.merge_bounds(tag);

                                // STORE TEXT EXACTLY IF IT IS A SCRIPT TAG
                                if (current_node.tag.sub_type == TagSubType.script ||
                                    current_node.tag.sub_type == TagSubType.comment ||
                                    current_node.tag.sub_type == TagSubType.filedetails) {
                                    current_node.tag.tstr = source[current_node.tag.outer.start..current_node.tag.outer.end];
                                } else if (current_node.tag.sub_type == TagSubType.void_exact ) {
                                    var vstart  =  current_node.tag.outer.start + (APP.SUB_TYPE_VOIDEX.Length + 2);
                                    var vend    =  current_node.tag.outer.end - (APP.SUB_TYPE_VOIDEX.Length + 3);
                                    current_node.tag.tstr = source[vstart..vend];
                                }

                                startTagArray.RemoveAt(startTagArray.Count-1);
                                if (stag.parent != null)current_node = stag.parent;
                                // PUT THE ALTERED ARRAY BACK IN THE MAP
                                start_tags[name] = startTagArray;
                            }
                        }
                        break;
                    }
                    default: {break;}
                }
            }

            // BREAK UP TEMPLATES FROM TREE IF AT TOP LEVEL
            for (int i = root.children.Count-1; i >= 0; i--) {
                var child = root.children[i];
                if (child.tag.sub_type == TagSubType.template) {
                    var new_tree = new TagTree();
                    new_tree.type  = TreeType.SubTemplate;
                    var template_name = child.tag.name.StartsWith(APP.PREFIX_TEMPLATE) ? child.tag.name.Substring(2) : child.tag.name;
                    new_tree.tree_name = template_name;
                    child.tag.name = "root";
                    child.tag.tag_type = TagType.root;
                    child.tag.sub_type = TagSubType.empty;
                    child.closing_tag.tag_type = TagType.root;
                    child.closing_tag.sub_type = TagSubType.empty;
                    new_tree.init(child);
                    final_trees.Add(new_tree);
                    root.children.RemoveAt(i);
                }
            }

            if (root.children.Count > 0) {
                var final_tree = new TagTree();
                final_tree.init(root);
                final_trees.Add(final_tree);
            }

            return final_trees;
        }

        // SUPER SLOPPY ... FIX SOMETIME
        static void ensure_map_has_array(Dictionary<string,List<TagBranch>> mapping , string key) {
            if (!mapping.ContainsKey(key) ) {
                mapping[key] = new List<TagBranch>();
            }
        }

        // return true or false if a source map string of keys string and values
        // is contained in the local map
        public static bool contains_attrib_map(Dictionary<string,Attribs> local , Dictionary<string,Attribs> source ) {
            var all_contained  =  false;

            foreach (var kp in source ) {
                var key = kp.Key;
                var attrib = kp.Value;
                if (local.ContainsKey(key) ) {
                    all_contained = local[key].Value == source[key].Value;
                } else if (attrib.Value.Length == 0 ) {
                    // WHEN A KEY WITH NO VALUE IS GIVEN, INT THEENTION
                    // IS TO DETERMINE THAT THE KEY IS _NOT_ PRESENT
                    all_contained = true;
                }
                if (!all_contained ) {
                    break;
                }
            }
            return all_contained;
        }

        // Parse a CSS file or style block
        public static List<TagGroup> collect_preprocess_blocks(string source, CmdLineOptions options) {
            var out_tags     = new List<TagGroup>();
            var watchers     = new List<Watcher>();
            var watcherTypes = new Dictionary<string, TagSubType>();

            watchers.Add(new Watcher(APP.SUB_TYPE_RAZOR, "@Html ) \\", true)
                         .setActive(options.preprocess_razor)
                         .setInsensitive(true));
            watcherTypes.Add(APP.SUB_TYPE_RAZOR, TagSubType.cshtml);


            var detailsWatchString = "<" + APP.SUB_TYPE_FILEDETAILS.ToUpper() + ">";
                detailsWatchString += " </" + APP.SUB_TYPE_FILEDETAILS.ToUpper() + "> \\";
            watchers.Add(new Watcher("filedetails", detailsWatchString, true));
            watcherTypes.Add("filedetails", TagSubType.filedetails);

            watchers.Add(new Watcher(APP.SUB_TYPE_SCRIPT, "<script*> </script> \\", true).offsets(1, 0));
            watcherTypes.Add(APP.SUB_TYPE_SCRIPT, TagSubType.script);

            var voidExWatchString = "<" + APP.SUB_TYPE_VOIDEX + ">";
                voidExWatchString += " </" + APP.SUB_TYPE_VOIDEX + "> \\";
            var vwatcher = new Watcher(APP.SUB_TYPE_VOIDEX, voidExWatchString, true).offsets(1, 0).setInsensitive(true);
            watchers.Add(vwatcher);
            watcherTypes.Add(APP.SUB_TYPE_VOIDEX, TagSubType.void_exact);

            watchers.Add(new Watcher(APP.SUB_TYPE_COMMENTS, "<!-- --> \\", true).offsets(0, 0));
            watcherTypes.Add(APP.SUB_TYPE_COMMENTS, TagSubType.comment);
            var comwatcher = watchers.Last();

            var i = 0;
            foreach (var chr in source ) {
                foreach (var watch in watchers) {
                    var points     = watch.consume(chr, i);
                    var mpoint     = points[0];
                    var match_lens = points[1];

                    if (watch.name == APP.SUB_TYPE_RAZOR && vwatcher.is_searching() ) {
                        continue;
                    }

                    // CONTINUE UNTIL END OF COMMENT
                    if (watch.name == "void" && comwatcher.is_searching() ) {
                        continue;
                    }
                    // CONTINUE UNTIL END OF COMMENT
                    if (vwatcher.is_searching() && watch.name != "void") {
                        continue;
                    }

                    if (mpoint.b >= 0 ) {
                        // SECOND POINT OBTAINED
                        if (mpoint.d > 0 ) {
                            var startlen =  0;
                            var endlen   =  0;
                            var subtype  =  TagSubType.empty;

                            var bound  =  watch.pop_match();
                            switch(watch.name) {
                                case APP.SUB_TYPE_RAZOR: {
                                    if (!vwatcher.is_searching() ) {
                                        startlen = match_lens.b;
                                        endlen   = match_lens.d;
                                        subtype  = TagSubType.cshtml;
                                    } else {
                                        return out_tags;
                                    }
                                    break;
                                }
                                default: {
                                    if (watcherTypes.ContainsKey(watch.name)) {
                                        startlen = match_lens.b;
                                        endlen   = match_lens.d;
                                        subtype  = watcherTypes[watch.name];
                                    } else {
                                        startlen = 0;
                                    }
                                    break;
                                }
                            }

                            if (watch.name == APP.SUB_TYPE_RAZOR ) {
                                out_tags.Add(new TagGroup{
                                        start = mpoint.b + 1,
                                        end = mpoint.d,
                                        sub_type = subtype,
                                        type = TagType.start,
                                    });
                                out_tags.Add(new TagGroup{
                                        start = mpoint.d,
                                        end = mpoint.d,
                                        sub_type = subtype,
                                        type = TagType.end,
                                    });
                            } else if (watch.name == APP.SUB_TYPE_COMMENTS ) {
                                out_tags.Add(new TagGroup{
                                        start = mpoint.b + 1,
                                        end = mpoint.d,
                                        sub_type = subtype,
                                        type = TagType.text,
                                    });
                                out_tags.Add(new TagGroup{
                                        start = mpoint.d,
                                        end = mpoint.d,
                                        sub_type = subtype,
                                        type = TagType.text,
                                    });
                            } else {
                                out_tags.Add(new TagGroup{
                                        start = mpoint.b,
                                        end = mpoint.b + startlen + 1,
                                        sub_type = subtype,
                                        type = TagType.start,
                                    });
                                out_tags.Add(new TagGroup{
                                        start = mpoint.d - endlen,
                                        end = mpoint.d,
                                        sub_type = subtype,
                                        type = TagType.end,
                                    });
                            }
                        }
                    }
                } // End Watcher loop;
                i++;
            } // End Source loop;

            return out_tags;
        }
    }  // END CLASS
}  // END NAMESPACE
