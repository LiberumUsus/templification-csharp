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

        public void load_and_parse_file(CmdLineOptions options) {
            // OPERATIONS
            this.template = File.ReadAllText(this.path);
            this.tag_tree = TagParsing.parse_html_to_tag_tree(this.template, options);
            this.tag_tree.process_attrib_commands();
            this.tag_tree.index_commands();
            if (this.tag_tree.root.locate_default_attrib_merge_tag(0) > 0 ) {
                this.tag_tree.root.tag.no_merge_attribs = true;
                if (this.tag_tree.root.children.Count > 0 ) {
                    this.tag_tree.root.children[0].tag.no_merge_attribs = true;
                }
            }
            this.tag_tree.index_tags();
            this.tag_tree.index_slots();
            this.tag_tree.root.parse_style_blocks(true);
            this.apply_local_tags();
            // SET VARS INFO
            var tname = this.name;

            this.tag_tree.root.tag.name = tname.ToLower();
        }

        public void apply_local_tags() {
            var style_sheet  =  this.tag_tree.root.style_sheet;
            this.tag_tree.root.apply_local_style_tags(style_sheet);
        }

    }  // END CLASS
}  // END NAMESPACE
