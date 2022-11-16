// TEMPLIFICATION MAIN CLASS
//
// NOTE: This program was originally written in the V language.
//       it is not optimized for C# in anyway at all. Actually,
//       its not optimized for anything. but it works! :D


namespace Templification {

    public class MainProg {

        static Crawler crawler = new Crawler();
        static Parser  parser  = new Parser();

        //════════════════════════════════════════════════════════════════════
        //  _____ ___ __  __ ___ _    ___ ___ ___ ___   _ _____ ___ ___  _  _
        // |_   _| __|  \/  | _ \ |  |_ _| __|_ _/ __| /_\_   _|_ _/ _ \| \| |
        //   | | | _|| |\/| |  _/ |__ | || _| | | (__ / _ \| |  | | (_) | .` |
        //   |_| |___|_|  |_|_| |____|___|_| |___\___/_/ \_\_| |___\___/|_|\_|
        //
        //════════════════════════════════════════════════════════════════════

        //════════════════════════════════════════════════════════════════════
        // Templification:
        // This program takes an html file(s) as a source, then scans it and
        // any number of template html files in a templates directory.
        // It then produces output files based on the input files and the
        // insertion of templates.
        // E.G.
        // \--\--\- Src\
        //     \        \- source.html <body><header/><widget><button>Click...
        //      \
        //       \- Templates\
        //                    \- Button.html
        //                     - Widget.html
        //                     - Header.html
        // \--\- OutDir\
        //              \- output.html <body><div class="header">Hello</d...
        //════════════════════════════════════════════════════════════════════


        // # Templification Main Function
        // Entry function for Templification program
        // This program takes an html file(s) as a source, then scans it and
        // any number of template html files in a templates directory.
        // It then produces output files based on the inputs and the
        // insertion of templates and options int insertedo the source html.
        static int Main(string[] args) {

            // Command Line Flags for program commands
            var basedir_flag  = new CLI.CLIFlag {
                flag = "string",
                abbrev = 'b',
                name = "basedir",
                default_value = new string[]{"./examples"},
                description = "The, base directory for all others; [default './examples']",
                display_in_help = true
            };

            var template_flag  = new  CLI.CLIFlag {
                flag = "string",
                abbrev = 't',
                name = "template",
                default_value = new string[]{"template"},
                description = "Directory, to be searched to find templates. [default 'BASEDIR/template']",
                display_in_help = true
            };

            var input_flag  = new  CLI.CLIFlag {
                flag = "string",
                abbrev = 'i',
                name = "input",
                default_value = new string[]{"src"},
                description = "Directory, in which the source files to be compiled are located. [default 'BASEDIR/src']",
                display_in_help = true
            };

            var out_css_flag  = new  CLI.CLIFlag {
                flag = "string",
                name = "out_css",
                default_value = new string[]{"bundle.css"},
                description = "Path, to the css output file. [default 'OUTPUT/bundle.css']",
                display_in_help = true
            };

            var out_js_flag  = new  CLI.CLIFlag {
                flag = "string",
                name = "out_js",
                default_value = new string[]{"bundle.js"},
                description = "Path, to the js output file. [default 'OUTPUT/bundle.js']",
                display_in_help = true
            };

            var css_in_flag  = new  CLI.CLIFlag {
                flag = "string",
                name = "css_in",
                default_value = new string[]{"src"},
                description = "Path, to the css input files",
                display_in_help = true
            };

            var output_flag  = new  CLI.CLIFlag {
                flag = "string",
                abbrev = 'o',
                name = "output",
                description = "Directory, for the output files to be saved. It [default 'BASEDIR/bin']",
                default_value = new string[]{"bin"},
                display_in_help = true
            };

            var ext_flag  = new  CLI.CLIFlag {
                flag = "string",
                abbrev = 'x',
                name = "extension",
                description = "Output, filename extension, [default .html]",
                default_value = new string[]{"html"},
                display_in_help = true
            };

            var styledir_flag  = new  CLI.CLIFlag {
                flag = "string",
                name = "style-dir",
                description = "Location, of style files",
                default_value = new string[]{"./"},
                display_in_help = true
            };

            var to_run  = new  CLI.CLIFlag {
                flag = "string",
                abbrev = 'm',
                name = "cmd",
                description = "Run, the selected command",
                default_value = new string[]{""},
            };


            /*   ___ ___  __  __ __  __   _   _  _ ___  ___ */
            /*  / __/ _ \|  \/  |  \/  | /_\ | \| |   \/ __| */
            /* | (_| (_) | |\/| | |\/| |/ _ \| .` | |) \__ \ */
            /*  \___\___/|_|  |_|_|  |_/_/ \_\_|\_|___/|___/ */

            // Command to run "tests" code
            var test_command = new CLI.CLICommand("testcode");



            //════════════════════════════════════════════════════════════════════
            // Add commandline options for "tests" command
            test_command.AddFlag(basedir_flag);
            test_command.AddFlag(output_flag);
            test_command.AddFlag(template_flag);
            test_command.AddFlag(input_flag);
            test_command.AddFlag(ext_flag);
            test_command.AddFlag(to_run);

            // Default command to run normal operations
            var app = new CLI.CLICommand {
                name = "HTML_GENERATOR",
                description = "Simple, templating engine for html documents",
                execute = normal_operations,
                //commands = new string[]{test_command},
            };


            //════════════════════════════════════════════════════════════════════
            // Add commandline options for default command
            app.AddFlag(basedir_flag);
            app.AddFlag(output_flag);
            app.AddFlag(template_flag);
            app.AddFlag(input_flag);
            app.AddFlag(ext_flag);
            app.AddFlag(out_css_flag);
            app.AddFlag(out_js_flag);
            app.AddFlag(css_in_flag);
            app.AddFlag(styledir_flag);

            //════════════════════════════════════════════════════════════════════
            // Add commandline options that are ONLY available for default command
            app.AddFlag(new CLI.CLIFlag {
                    flag = "bool",
                        abbrev = 'd',
                        name = "debug",
                        description = "Show, debug messages",
                        display_in_help = true
                        });

            app.AddFlag(new CLI.CLIFlag {
                    flag = "string",
                        abbrev = 'c',
                        name = "testcode",
                        description = "Run, Code Tests... this should be removed.. why is it in production!!!?",
                        });

            app.AddFlag(new CLI.CLIFlag {
                    flag = "bool",
                        name = "test",
                        description = "Don\"t, create files just do test",
                        display_in_help = true
                        });

            app.AddFlag(new CLI.CLIFlag {
                    flag = "bool",
                        abbrev = 'h',
                        name = "help",
                        description = "Show, this help",
                        display_in_help = true
                        });

            // Setup menu
            //app.setup();

            // Parse command line and run command bound functions
            app.Parse(args);

            normal_operations(app);

            return 0;
        }



        // Function called for normal operations performed by the program as opposed to tests or help etc.
        public static bool normal_operations(CLI.CLICommand cmd) {
            var show_help  = cmd.GetBool("help") ; // or  false
            var debug_mode = cmd.GetBool("debug") ; // or  false

            // Help is requsted run this command and nothing else.
            if (show_help) {
                cmd.execute_help();
                return true;
            }


            var base_dir       =  cmd.GetString("basedir") ; // or  "examples"
            var in_path        =  get_cli_path(cmd, "input",     "src",        base_dir);
            var template_path  =  get_cli_path(cmd, "template",  "template",   base_dir);
            var style_dir      =  get_cli_path(cmd, "style-dir", "./src/",     base_dir);
            var css_in         =  get_cli_path(cmd, "css_in",    "src",        base_dir);
            var out_path       =  get_cli_path(cmd, "output",    "bin",        base_dir);
            var out_css        =  get_cli_path(cmd, "out_css",   "bundle.css", out_path);
            var out_js         =  get_cli_path(cmd, "out_js",    "bundle.js",  out_path);

            var ext  = cmd.GetString("extension") ; // or  "cshtml"


            // HACK UPDATE JS OUT
            if (out_css != "bundle.css" && out_js == base_dir + "/" + "bundle.js" ) {
                var js_base  =  Utils.Utils.file_parent(out_css);
                if (Utils.Utils.file_stem(js_base) == "css" ) {
                    js_base = Utils.Utils.file_parent(js_base) + "/js";
                }
                out_js = js_base + "/bundle.js";
            }


            // Create new crawldata object for templification operations
            var cmd_line_flags  = new  CmdLineOptions {
                in_dir = in_path,
                template_dir = template_path,
                style_dir = style_dir,
                css_dir = css_in,
                out_css = out_css,
                out_ext = ext,
                out_js = out_js,
                out_dir = out_path,
            };


            print_command_line_flags(cmd_line_flags, base_dir);
            //════════════════════════════════════════════════════════════════════
            // Begin templification and crawl directories
            var crawl_files  = crawler.crawl_directories(cmd_line_flags);
            Console.WriteLine(crawl_files.input_files.Count);
            var parsed_files = (uint)0;
            try {
                parsed_files = parser.parse_files(crawl_files, cmd_line_flags);
            } catch (Exception e) {
                Console.WriteLine("[ERROR] >> " + e.Message + " <<\n");
                if (debug_mode) {
                    Console.WriteLine(e.StackTrace);
                }
            }

            Console.WriteLine("[INFO]  " + parsed_files + " Parsed Files :) ");
            Console.WriteLine("\n══════════════════════ END OF LINE ═════════════════════════════════");
            return true;
        }



        // Print values set in the command line.
        // This does not produce an exhaustive list, just the "interesting"
        // settings.
        static void print_command_line_flags(CmdLineOptions cmd_line_flags, string base_dir) {
            Console.WriteLine("\n════════════════════════════════════════════════════════════════════");
            Console.WriteLine("                       FLAGS  ");
            Console.WriteLine("════════════════════════════════════════════════════════════════════");
            Utils.Utils.print_tableln(" [INFO] BASEDIR: "       + base_dir, new List<int>{22, -1}, ":");
            Utils.Utils.print_tableln(" [INFO] OUT PATH: "      + cmd_line_flags.out_dir, new List<int>{22, -1}, ":");
            Utils.Utils.print_tableln(" [INFO] IN PATH: "       + cmd_line_flags.in_dir, new List<int>{22, -1}, ":");
            Utils.Utils.print_tableln(" [INFO] TEMPLATE PATH: " + cmd_line_flags.template_dir, new List<int>{22, -1}, ":");
            Utils.Utils.print_tableln(" [INFO] CSS IN: "        + cmd_line_flags.css_dir, new List<int>{22, -1}, ":");
            Utils.Utils.print_tableln(" [INFO] CSS OUT: "       + cmd_line_flags.out_css, new List<int>{22, -1}, ":");
            Utils.Utils.print_tableln(" [INFO] EXTENSION: "     + cmd_line_flags.out_ext, new List<int>{22, -1}, ":");
            Utils.Utils.print_tableln(" [INFO] JS OUT: "        + cmd_line_flags.out_js, new List<int>{22, -1}, ":");
            Utils.Utils.print_tableln(" [INFO] STYLE DIR: "     + cmd_line_flags.style_dir, new List<int>{22, -1}, ":");
            Utils.Utils.print_tableln(" [INFO] DB OUT DIR: "    + cmd_line_flags.out_dir + "/SiteInfo.sqlite", new List<int>{22, -1}, ":");
            Console.WriteLine("════════════════════════════════════════════════════════════════════\n\n");
        }



        // return the path for the CLI flag prepending the `default` directory
        // when the path provided does not start with `./` or `/`
        static string get_cli_path(CLI.CLICommand cmd, string flag_name, string defaultStr, string base_dir) {
            var out_value  = cmd.GetString(flag_name);
            if (string.IsNullOrEmpty(out_value)) out_value = defaultStr;
            if (!out_value.StartsWith("/") && !out_value.StartsWith("./") ) {
                out_value = base_dir + "/" + out_value;
                out_value = out_value.Replace("//", "/");
            }
            return out_value;
        }

    } // END CLASS
} // END NAMESPACE
