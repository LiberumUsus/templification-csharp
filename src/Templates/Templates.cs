using Templification.Tags;
using Templification.Utils;
using Templification.Styles;

namespace Templification.Templates {


    public class Templates {

        // POPULATE TEMPLATE AND RETURN THE HYDRATED VERSION
        public static TagBranch use_template_tree(TagTree template, TagBranch usage, StyleSheet tree_styles) {
            usage.apply_local_style_tags(tree_styles);
            var skip_list   =  new Dictionary<int,string>();
            var orig_branch =  usage.clone();

            // PERFORM TEMPLATE LOGIC (MUST HAPPEN FIRST)
            var ptemplate  =  template.process_commands(usage);
            // COPY ALTERED TEMPLATE
            usage.copy(ptemplate.root);

            // LOOP OVER ORIGINAL SLOT MAP AND INSERT SLOT INFO INTO CORRECT SLOTS
            foreach (var KeyPair in orig_branch.slot_map ) {
                var key = KeyPair.Key;
                var slot_array = KeyPair.Value;
                var slot_attrib_filter  = new Dictionary<string, Attribs>() {
                    {"name", new Attribs {value = key}}
                };
                foreach (var item in slot_array ) {
                    skip_list[item.tag.get_id()] = item.tag.name;
                }
                usage.insert_into_where(slot_array, "slot", slot_attrib_filter);
            } // END LOOP OVER SLOT MAP;

            // FILL CHILDREN SLOTS BASED ON TAG NAME
            usage.insert_into_by_tag_name(orig_branch.exclude_children_by_id(skip_list), skip_list);
            // DEFAULT VARIABLE IS SET IN PARENT
            var default_is_set  = orig_branch.tag.attribs.ContainsKey("{default}");

            // FILL REST OF CHILDREN INTO SLOT IF IT IS AVAILABLE
            usage.insert_into(orig_branch.exclude_children_by_id(skip_list), "slot",
                              new Dictionary<string, Attribs>(){
                                  {"name", new Attribs{value = ""}}
                              },
                              usage.has_default(), default_is_set);

            usage.merge_with_template(orig_branch, usage.style_sheet);
            usage.clear_vars(1);
            usage.replace_vars(orig_branch);
            // LOCATE TAGS DEPENDING ON SLOT CONTENT
            var purge_slots  =  usage.locate_dep_slots();
            // REMOVE EMPTY SLOTS
            usage.remove_slots(true);
            // REMOVE SLOT DEPENDENCIES WHERE NO SLOT PROVIDED
            usage.remove_slots_deps(purge_slots);
            usage.apply_internal_attrib_commands(true);
            // SPECIAL CASE FINAL ATTRIB COMMANDS
            if (orig_branch.tag.internal_attribs.ContainsKey("__wrapin")) {
                var tagName  = orig_branch.tag.internal_attribs["__wrapin"].value;
                var tagNames = tagName.Split('.').ToList();
                tagNames.Reverse();
                foreach (var part in tagNames) {
                    wrap_branch(part, usage);
                }
            }
            return new TagBranch();
        }

        private static void wrap_branch(string tagName, TagBranch orig) {
            if (string.IsNullOrEmpty(tagName)) return;
            if (orig == null) return;
            var internals = orig.clone();
            // CLEAN TAGNAME
            tagName = tagName.Replace("<", "").Replace(">", "");
            var wrappedin = new TagBranch(tagName, orig);
            wrappedin.add_child(internals, false);
            orig.copy(wrappedin);
        }

    }  // END CLASS
}  // END NAMESPACE
