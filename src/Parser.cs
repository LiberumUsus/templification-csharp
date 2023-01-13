using Templification.Tags;
using Templification.Styles;
using Templification.Data;
using Templification.Templates;
using Templification.Utils;
using System.Text;

namespace Templification {

    public class Parser {

        static int _spindex = -1;
        static char[] _spinArray = new char[] {'-','\\','|','/','-','\\','|'};
        static char[] _progArray = new char[] {'[','#','.',']'};

        // Parse all of the files that were collected during the
        // crawling operation.
        public uint parse_files(CrawlFiles crawl_files, CmdLineOptions cmd_line_options) {
            var master_classes = new Dictionary<string,bool>();
            var master_sheet   = new StyleSheet();
            var master_text    = new List<TextData>();

            FileStream? css_bundle_file;
            FileStream? js_bundle_file;

            if (Utils.Utils.open_file_with_hand_holding(cmd_line_options.out_css,
                                                    out css_bundle_file,
                                                    cmd_line_options,
                                                    "Cannot open css file: " + cmd_line_options.out_css)  < 0) {
                return 0;
            }
            if (Utils.Utils.open_file_with_hand_holding(cmd_line_options.out_js,
                                                    out js_bundle_file,
                                                    cmd_line_options,
                                                        "Cannot open js file: " + cmd_line_options.out_js) < 0) {
                return 0;
            }

            // Double check and make sure filestreams are not null
            if (css_bundle_file == null || js_bundle_file == null) return 0;

            // Default return value
            var file_count = (uint)crawl_files.input_files.Count;
            var max_width  =  Console.WindowWidth - 35;
            var index      = 0;

            if (max_width <= 0) max_width = 40;

            // Loop over input files and insert templates
            foreach (var kp in crawl_files.input_files ) {
                var fname       = kp.Key;
                var source_file = kp.Value;
                var tag_tree    = source_file.tag_tree;
                // new string variable to hold produced html
                var output_html  =  "";
                index++;

                // Current file
                if (cmd_line_options.debug_mode) {
                    Console.WriteLine(" [DEBUG] "+ "READING FILE: " + source_file.path);
                }

                // ════════════════════════════════════════════════════════════════════
                // PARSING OF INPUT FILE WITH TEMPLATE FILES

                // Parse the HTML tree in the current file
                parse_tree(tag_tree, crawl_files.template_files);
                // Clean Up Actions
                // Removed unpopulated variables
                tag_tree.root.clear_vars(-1);
                // Collect classes used in the tree
                tag_tree.collect_classes();
                // Collect scripts to bundle
                tag_tree.collect_scripts(); //<═══ END SECTION;

                // ════════════════════════════════════════════════════════════════════
                // MERGE INT DATAO MASTER COLLECTIONS

                // Collect tree class into list of master class list
                foreach (var key in tag_tree.class_list.Keys ) {
                    master_classes[key] = tag_tree.class_list[key];
                }

                // Merge tree css int styleso master styles
                master_sheet.merge(tag_tree.styles);

                // Loop over each branch in the tree and collect text int datao
                // master text data object
                var tag_branch  =  tag_tree.root as TagBranch;

                output_html = tag_branch.to_string(0);
                var page_text  =  tag_branch.get_text_data();

                foreach (var item in page_text ) {
                    item.source = Utils.Utils.file_stem(source_file.get_path());
                    item.subsource = Utils.Utils.file_stem(Utils.Utils.file_parent(source_file.get_path()));
                }
                master_text.AddRange(page_text);

                //<═══ END SECTION

                // ════════════════════════════════════════════════════════════════════
                // WRITING OUTPUT HTML

                // Write collected javascript to bundle file
                if (tag_tree.bundle_scripts.Trim().Length > 0) {
                    js_bundle_file.Write(Encoding.UTF8.GetBytes(tag_tree.bundle_scripts));
                }

                // Get pointer to HTML tree root element
                var root  =  tag_tree.root;

                // Get output path for output file
                var ifile_path  =  trim_path_prefix_add_ext(source_file.get_path(), cmd_line_options.in_dir, cmd_line_options.out_ext);
                var output_path =  cmd_line_options.out_dir + ifile_path;

                // Test mode fake write or actual write below
                if (cmd_line_options.test_mode && cmd_line_options.debug_mode) {
                    Console.WriteLine(" [DEBUG] "+ "[test-mode]");
                    Console.WriteLine(" [DEBUG] "+ "Writing files");
                } else {
                    if (cmd_line_options.debug_mode) {
                        Console.WriteLine(" [DEBUG] "+ "Writing file: " + output_path);
                    } else {
                        if (Console.CursorTop-1 < 0) {
                            Console.WriteLine(" [DEBUG] "+ "Writing file: " + output_path);
                        } else {
                            Console.SetCursorPosition(0, Console.CursorTop -1);
                            Console.WriteLine(" [INFO] Processing: " + progDisplay(index, file_count, max_width));
                        }
                    }

                    var strDirName = Path.GetDirectoryName(output_path);
                    if (!string.IsNullOrEmpty(strDirName)) Directory.CreateDirectory(strDirName);
                    var fwriter = new System.IO.StreamWriter(output_path);
                    fwriter.Write(output_html);
                    fwriter.Close();
                } //<═══ END SECTION;
                if (cmd_line_options.debug_mode) {
                    Console.WriteLine(" [DEBUG] "+ "               <<< File Parsing Done\n");
                }
            } //<═══ END FILE LOOP;

            // ════════════════════════════════════════════════════════════════════
            // WRITE STYLES TO BUNDLE AND DATA TO DATABASE

            // Merge stylesheets, strip unused classes, generate generatable classes
            // and apply applies
            Console.WriteLine(" [INFO] "+ "Polishing up CSS style sheet");
            master_sheet = master_sheet.polish_style_sheets(crawl_files.css_files, master_sheet, master_classes, cmd_line_options);

            // Write CSS bundle file to css output path
            css_bundle_file.Write(Encoding.UTF8.GetBytes(master_sheet.str())) ; // or

            // Write datbase file
            //report_textdata_to_database(cmd_line_options.out_dir + "/SiteInfo.sqlite",master_text);
            // <═══ END SECTION
            // ════════════════════════════════════════════════════════════════════
            // CLOSE OPEN FILES

            // DELETE THE CSS FILE IF NOTHING WAS WRITEN
            if (css_bundle_file.Length == 0 && File.Exists(cmd_line_options.out_css)) {
                Console.WriteLine(" [INFO] Nothing written to CSS file, deleting.");
                File.Delete(cmd_line_options.out_css);
            }
            css_bundle_file.Close();
            // DELETE THE JS FILE IF NOTHING WAS WRITEN
            if (js_bundle_file.Length == 0 && File.Exists(cmd_line_options.out_js)) {
                Console.WriteLine(" [INFO] Nothing written to JS file, deleting.");
                File.Delete(cmd_line_options.out_js);
            }
            js_bundle_file.Close();

            return file_count;
        }

        // Parse a tag tree object for templates and replace them with template content.
        public void parse_tree(TagTree tag_tree, Dictionary<string, TemplateData> template_files ) {
            var child_tag_name  =  "";

            // Iterate over tags from inner to outer
            foreach (var tbranch in tag_tree.in_to_out ) {
                child_tag_name = tbranch.tag.name.ToLower();

                if (tbranch.tag.tag_type != TagType.root ) {
                    if (template_files.ContainsKey(child_tag_name)) {
                        // COPY TEMPLATE TO BRANCH NODE
                        var template_tree  =  template_files[child_tag_name].tag_tree;
                        parse_tree(template_tree, template_files);
                        // ------------------------------------ START MERGE/USE OF TEMPLATE
                        Templates.Templates.use_template_tree(template_tree, tbranch, tag_tree.styles);
                        // ------------------------------------ END MERGE/USE OF TEMPLATE
                    } else {
                        // REPLACE LOCAL VARS AS NEEDED
                        tbranch.replace_vars(new TagBranch());
                    }
                }
            }

            // Parse styles at tree level
            tag_tree.styles = tag_tree.root.parse_style_blocks(true);
            tag_tree.root.apply_local_style_tags(tag_tree.styles);
        }

        // Trim the given path `file_path` based on a provided prefix `input_dir` and
        // replace the existing extension with the provided extension `ext`
        string trim_path_prefix_add_ext(string file_path, string input_dir, string ext) {
            var new_path  =  file_path.StartsWith(input_dir) ? file_path.Substring(input_dir.Length) : file_path;
            new_path = new_path.Replace("//", "/");
            return new_path.AllBefore(".") + "." + ext;
        }


        static string progDisplay(int index, uint count, int width) {
            if (count < 0) return "??? eh?";
            float percent = (index / (float)count);
            var point     = (int)(percent * width);
            var bar =  "" + _progArray[0];
            var strcount =  "<" +index + ">";

            if (index == count) {
                bar += strcount + new string(_progArray[1], width-2);
            } else {
                point = (point - strcount.Length <= 0) ? 1 : point - strcount.Length;
                bar += strcount;
                bar += new string(_progArray[1], point);
                bar += new string(_progArray[2], (width - point) - 2);
            }
            bar += _progArray[3];
            return bar;
        }

        static char spinner() {
            _spindex += 1;
            if (_spindex >= _spinArray.Length) _spindex = 0;
            return _spinArray[_spindex];
        }


    } // END CLASS;
} // END NAMESPACE;
