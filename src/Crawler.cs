using Templification.Templates;
using Templification.Styles;

namespace Templification {


    // # Command Line Options
    // Used to pass information about the user supplied flags or defaults
    // to the program.
    // Examples would be:
    // - Input Directory for files to be processed
    // - Template Directory for files that are used during templification
    // - Output directory for CSS files
    // - Etc.
    public class CmdLineOptions {
        public string out_dir        = "";
        public string out_css        = "";
        public string out_js         = "";
        public string in_dir         = "";
        public string css_dir        = "";
        public string template_dir   = "";
        public string out_ext        = "";
        public string style_dir      = "";
        public string rules_file     = "";
        public string color_file     = "";
        public string config_path    = "";
        public bool   test_mode        = false;
        public bool   debug_mode       = false;
        public bool   auto_make_dirs   = false;
        public bool   preprocess_razor = false;
    }



    // # Crawl File Information
    // Contains directory information structures filled after
    // crawling directories specified in the command line or
    // that are set as defaults.
    public class CrawlFiles {
        public Dictionary<string, TemplateData> input_files;
        public Dictionary<string, TemplateData> template_files;
        public Dictionary<string, StyleSheet>   css_files;

        public CrawlFiles(Dictionary<string, TemplateData> _input_files,
                          Dictionary<string, TemplateData> _template_files,
                          Dictionary<string, StyleSheet>   _css_files) {
            input_files    = _input_files;
            template_files = _template_files;
            css_files      = _css_files;
        }

    }


    public class Crawler {

        // Collect information about files in the provided directory "crawl_directory"
        // that match the provided extension "ext_name" and return the information
        // in a map where the key is the file name.
        public Dictionary<string, TemplateData> collect_files_by_ext(string crawl_directory, string ext_name)  {
            var file_mapping = new Dictionary<string,TemplateData>();

            // BAIL IF THE DIRECTORY DOESNT EXIST
            if (!Directory.Exists(crawl_directory)) {
                Console.WriteLine("[ERROR] Input directory does not exist! " + crawl_directory);
                return file_mapping;
            }

            var file_array = Directory.GetFiles(crawl_directory, "*." + ext_name, SearchOption.AllDirectories);

            foreach(var entry in file_array) {
                var fpath = entry;
                var fname = Path.GetFileName(entry);
                fname = Utils.Utils.file_stem(fname).ToLower().Trim();
                file_mapping[fname] = new TemplateData();
                file_mapping[fname].init(fpath);
            }
            return file_mapping;
        }



        // Collect all of the `css` files in a provided directory `crawl_directory` and
        // return a map collection of <b><a href="styles.html#StyleSheet">StyleSheets</a></b>.
        public Dictionary<string,StyleSheet> collect_all_css_files(string crawl_directory)  {
            var sheets = new Dictionary<string,StyleSheet>();

            // BAIL IF THE DIRECTORY DOESNT EXIST
            if (!Directory.Exists(crawl_directory)) {
                return sheets;
            }
            var file_array = Directory.GetFiles(crawl_directory, "*.css", SearchOption.AllDirectories);

            foreach (var entry in file_array) {
                var fpath = entry;
                string fname = Path.GetFileName(entry);
                var css_content = File.ReadAllText(fpath);
                sheets[fname] = StyleOps.parse_styles(css_content);
            }
            return sheets;
        }



        // Crawl over the input and template directories, then create output files
        public CrawlFiles crawl_directories(CmdLineOptions options ) {
            var input_mapping    = crawl_and_load_files(options.in_dir, options);
            var template_mapping = crawl_and_load_files(options.template_dir, options);
            var css_stylesheets  = crawl_and_load_css(options.css_dir);

            return new CrawlFiles(input_mapping, template_mapping, css_stylesheets);
        }



        // Load the input files and parse them
        public Dictionary<string,TemplateData> crawl_and_load_files(string input_dir, CmdLineOptions options) {
            var input_files = collect_files_by_ext(input_dir, "html");
            // LOAD TEMPLATE DATA
            var file_keys = input_files.Keys.ToList();
            foreach(var fileKey in file_keys) {
                var file_templates = input_files[fileKey].load_and_parse_file(options);
                foreach (var tree_key in file_templates.Keys) {
                    var ttree = file_templates[tree_key];
                    if (!input_files.ContainsKey(tree_key.ToLower())) {
                        var tdata = new TemplateData() {
                            name = tree_key,
                            tag_tree = ttree,
                        };
                        tdata.apply_local_tags();
                        input_files.Add(tree_key.ToLower(), tdata);
                    }
                }
            }
            return input_files;
        }



        // Crawl CSS directory and load CSS files
        public Dictionary<string, StyleSheet> crawl_and_load_css(string input_dir) {
            return collect_all_css_files(input_dir);
        }
    }

}
