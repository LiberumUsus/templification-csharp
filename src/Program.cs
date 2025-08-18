// TEMPLIFICATION MAIN CLASS
//
// NOTE: This program was originally written in the V language.
//       it is not optimized for C# in anyway at all. Actually,
//       its not optimized for anything. but it works! :D


using System.Text.RegularExpressions;

namespace Templification {

    public class MainProg {

        static Crawler crawler = new Crawler();
        static Parser  parser  = new Parser();
        static int     console_width = 60;

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

            // Set console width
            console_width = Console.WindowWidth;

            // Command Line Flags for program commands
            var basedir_flag  = new CLI.CLIFlag {
                flag = "string",
                abbrev = 'b',
                name = "basedir",
                default_value = new string[]{"./"},
                description = "The, base directory for all others; [default './']",
                display_in_help = true
            };

            var template_flag  = new  CLI.CLIFlag {
                flag = "string",
                abbrev = 't',
                name = "template",
                default_value = new string[]{"templates"},
                description = "Directory, to be searched to find templates. [default 'BASEDIR/src/templates']",
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
                description = "Directory, for the output files to be saved. It [default 'BASEDIR/build']",
                default_value = new string[]{"build"},
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

            var rulefile_flag  = new  CLI.CLIFlag {
                flag = "string",
                name = "rules-file",
                description = "Name of rules file",
                default_value = new string[]{"css_rules.txt"},
                display_in_help = true
            };

            var colorfile_flag  = new  CLI.CLIFlag {
                flag = "string",
                name = "color-file",
                description = "Name of colors file",
                default_value = new string[]{"colors.txt"},
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
            app.AddFlag(colorfile_flag);
            app.AddFlag(rulefile_flag);

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
                    flag = "bool",
                        name = "preprocess_razor",
                        description = "Enable the preprocessing of razor files (beta) ..err (alpha)",
                        display_in_help = true
                        });

            app.AddFlag(new CLI.CLIFlag {
                    flag = "string",
                        abbrev = 'e',
                        name = "testcode",
                        description = "Run, Code Tests... this should be removed.. why is it in production!!!?",
                        });

            app.AddFlag(new CLI.CLIFlag {
                    flag = "string",
                        abbrev = 'c',
                        name = "config",
                        description = "Set a location for the config file. !Any additional flags will override flags set in the config file!",
                        display_in_help = true,
                        default_value = new string[]{".tmplific_config"},
                        });

            app.AddFlag(new CLI.CLIFlag {
                    flag = "bool",
                        name = "test",
                        description = "Don\"t, create files just do test",
                        display_in_help = true
                        });

            app.AddFlag(new CLI.CLIFlag {
                    flag = "bool",
                        name = "autocreate_dirs",
                        abbrev = 'a',
                        description = "Automatically create output dirs",
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
            if (!app.Parse(args)) {
                app.execute_help();
                return 0;
            }

            // Slightly weird parsing here... don't have a better way yet
            if (app.GetString("config").Length > 0) {
                Console.WriteLine("Processing config file");
                process_config_file(app);
            }

            // Perform "Normal Operations" ... should be based on command setup, but ya...
            normal_operations(app);

            return 0;
        }



        // Function called for normal operations performed by the program as opposed to tests or help etc.
        public static bool normal_operations(CLI.CLICommand cmd) {
            var show_help  = cmd.GetBool("help");
            var debug_mode = cmd.GetBool("debug");

            // Help is requsted run this command and nothing else.
            if (show_help) {
                cmd.execute_help();
                return true;
            }


            var base_dir       =  cmd.GetString("basedir");
            var in_path        =  get_cli_path(cmd, "input",     "src",        base_dir);
            var template_path  =  get_cli_path(cmd, "template",  "templates",  in_path);
            var style_dir      =  get_cli_path(cmd, "style-dir", "./src/",     base_dir,true);
            var css_in         =  get_cli_path(cmd, "css_in",    "src",        base_dir);
            var out_path       =  get_cli_path(cmd, "output",    "bin",        base_dir);
            var out_css        =  get_cli_path(cmd, "out_css",   "bundle.css", out_path);
            var out_js         =  get_cli_path(cmd, "out_js",    "bundle.js",  out_path);

            var ext  = cmd.GetString("extension");


            // Create new crawldata object for templification operations
            // #TODO: Need a better way to convert command line args into args
            //        variable for the rest of the code
            var cmd_line_flags = new  CmdLineOptions {
                in_dir         = in_path,
                template_dir   = template_path,
                style_dir      = style_dir,
                css_dir        = css_in,
                out_css        = out_css,
                out_ext        = ext,
                out_js         = out_js,
                out_dir        = out_path,
                color_file     = cmd.GetString("color-file", "colors.txt"),
                rules_file     = cmd.GetString("rules-file", "css_rules.txt"),
                test_mode        = cmd.GetBool("test"),
                auto_make_dirs   = cmd.GetBool("autocreate_dirs"),
                preprocess_razor = cmd.GetBool("preprocess_razor"),
                debug_mode       = debug_mode
            };

            print_command_line_flags(cmd_line_flags, base_dir);
            //════════════════════════════════════════════════════════════════════
            // Begin templification and crawl directories
            var crawl_files  = crawler.crawl_directories(cmd_line_flags);
            var parsed_files = (uint)0;
            try {
                parsed_files = parser.parse_files(crawl_files, cmd_line_flags);
            } catch (Exception e) {
                Console.WriteLine("[ERROR] >> " + e.Message + " <<\n");
                if (debug_mode) {
                    Console.WriteLine(e.StackTrace);
                }
            }

            if (cmd_line_flags.test_mode) {
                Console.WriteLine(" [INFO]  " + parsed_files + " Files pretended to be parsed [test-mode] :) ");
            } else {
                Console.WriteLine(" [INFO]  " + parsed_files + " Parsed Files");
            }
            Console.WriteLine("\n══════════════════════ END OF LINE ═════════════════════════════════");
            return true;
        }



        // Print values set in the command line.
        // This does not produce an exhaustive list, just the "interesting"
        // settings.
        static void print_command_line_flags(CmdLineOptions cmd_line_flags, string base_dir) {
            Console.WriteLine("\n " + new string('═', console_width-2));
            Console.WriteLine("                       FLAGS  ");
            Console.WriteLine(" " + new string('═', console_width-2));
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
            Utils.Utils.print_tableln(" [INFO] AUTO CREATE: "   + cmd_line_flags.auto_make_dirs, new List<int>{22, -1}, ":");
            Console.WriteLine(" " + new string('═', console_width-2));
            Console.WriteLine("\n");
        }



        // return the path for the CLI flag prepending the `default` directory
        // when the path provided does not start with `./` or `/`
        static string get_cli_path(CLI.CLICommand cmd, string flag_name, string defaultStr, string base_dir, bool is_dir = false) {
            var out_value  = cmd.GetString(flag_name);
            if (is_dir && !out_value.EndsWith('/')) out_value += "/";
            if (string.IsNullOrEmpty(out_value)) out_value = defaultStr;
            if (!out_value.StartsWith("/") && !out_value.StartsWith("./") ) {
                out_value = base_dir + "/" + out_value;
                out_value = out_value.Replace("//", "/");
            }
            return out_value;
        }



        // Load options from file and parse them
        static void process_config_file(CLI.CLICommand cmd) {
            var base_dir   = cmd.GetString("basedir");
            var configPath =  get_cli_path(cmd, "config", "", base_dir);
            if (configPath.Length > 0 && File.Exists(configPath)) {
                var configFileLines = File.ReadAllText(configPath).Split('\n');
                var arg_str = "";
                foreach (var line in configFileLines) {
                    if (line.StartsWith("#")) continue;

                    arg_str += line + " ";
                }

                arg_str = Regex.Replace(arg_str, @"\s+", " ");
                cmd.Parse(arg_str.Trim().Split(" "));
            }
        }


    } // END CLASS
} // END NAMESPACE
