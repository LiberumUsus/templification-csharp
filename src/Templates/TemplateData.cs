using Templification.Tags;
using Templification.Utils;
using Templification.Styles;


namespace Templification.Templates {
    //  Stores information about each template and its file etc
    public class TemplateData {

        public string name      = "";
        public string file_name = "";
        public string path      = "";
        public string template  = "" ;

        public TagTree    tag_tree = new TagTree();
        public StyleSheet styles   = new StyleSheet();

        // Initialize the TemplateData object
        public TemplateData init(string path) {
            this.path = path;
            this.file_name = Path.GetFileName(path);
            this.name = Utils.Utils.file_stem(this.file_name).ToLower().Trim();
            return this;
        }

        public string get_path() {
            return this.path;
        }

        public string get_name() {
            return this.name;
        }

        public string get_template() {
            return this.template;
        }

        public bool is_template_loaded() {
            return this.template.Length > 0;
        }

        public void load_file() {
            this.template = File.ReadAllText(this.path);
        }

        public Dictionary<string, TagTree> load_and_parse_file(CmdLineOptions options) {
            var template_trees = new Dictionary<string, TagTree>();
            // OPERATIONS
            this.template = File.ReadAllText(this.path);
            var treeList = TagParsing.parse_html_to_tag_tree(this.template, options);
            foreach (var tag_tree in treeList) {
                tag_tree.process_attrib_commands();
                tag_tree.index_commands();
                if (tag_tree.root.locate_default_attrib_merge_tag(0) > 0 ) {
                    tag_tree.root.tag.no_merge_attribs = true;
                    if (tag_tree.root.children.Count > 0 ) {
                        tag_tree.root.children[0].tag.no_merge_attribs = true;
                    }
                 }
                tag_tree.index_tags();
                tag_tree.index_slots();
                tag_tree.root.parse_style_blocks(true);
                if (tag_tree.type == TreeType.Standard) {
                    this.tag_tree = tag_tree;
                } else {
                    if (!template_trees.ContainsKey(tag_tree.root.tag.name)) {
                        template_trees.Add(tag_tree.tree_name.ToLower(), tag_tree);
                    }
                }
                if (this.tag_tree.root.children.Count > 0 ) {
                    //    this.tag_tree.root.children[0].tag.no_merge_attribs = true;
                }
                // COLLECT SCRIPTS FOR BUNDLING
                this.tag_tree.collect_scripts();
            }
            this.apply_local_tags();
            // SET VARS INFO
            var tname = this.name;
            this.tag_tree.root.tag.name = tname.ToLower();

            return template_trees;
        }

        public void apply_local_tags() {
            var style_sheet  =  this.tag_tree.root.style_sheet;
            this.tag_tree.root.apply_local_style_tags(style_sheet);
        }

    }  // END CLASS
}  // END NAMESPACE
