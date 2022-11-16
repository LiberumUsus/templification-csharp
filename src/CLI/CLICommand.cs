// Command line interface functionality
// CLICommand class used to create commands
// NOTE: Work in progress... well not polished anyway

namespace Templification.CLI {
    public class CLICommand {
        public Func<CLICommand, bool>?     execute;
        public Dictionary<string, CLIFlag> flags;
        public Dictionary<char, string>    flag_refs;
        public string                      name        = "";
        public string                      description = "";

        public CLICommand() {
            name      = "";
            execute   = null;
            flags     = new Dictionary<string,CLIFlag>();
            flag_refs = new Dictionary<char,string>();
        }

        public CLICommand(string cname, Func<CLICommand, bool> command): this(cname) {
            execute = command;
        }

        public CLICommand(string cname) {
            name      = cname;
            execute   = this.DoNothing;
            flags     = new Dictionary<string,CLIFlag>();
            flag_refs = new Dictionary<char,string>();
        }

        public bool DoNothing(CLICommand nothing) {
            return false;
        }

        public void AddFlag(CLIFlag flag) {
            flags.Add(flag.name, flag);
            if (flag.abbrev > 0) {
                flag_refs.Add(flag.abbrev, flag.name);
            }
        }

        public string GetString(string name) {
            var outval = "";
            if (string.IsNullOrEmpty(name)) return outval;

            char flagref  = name[0];
            if (flags.ContainsKey(name)) {
                outval = flags[name].value;
                if (string.IsNullOrEmpty(outval)) outval = flags[name].default_value[0];
            } else if (flag_refs.ContainsKey(flagref)) {
                outval = flags[flag_refs[flagref]].value;
                if (string.IsNullOrEmpty(outval)) outval = flags[name].default_value[0];
            }
            return outval;
        }

        public bool GetBool(string name) {
            if (string.IsNullOrEmpty(name)) return false;

            char flagref = name[0];
            if (flags.ContainsKey(name)) {
                return (flags[name].value == "true");
            } else if (flag_refs.ContainsKey(flagref)) {
                return (flags[flag_refs[flagref]].value == "true");
            }
            return false;
        }


        // GET THE FLAG WITH THE GIVEN NAME "name"
        public string GetFlag(string name, out CLIFlag flag) {
            flag = new CLIFlag();
            if (string.IsNullOrEmpty(name)) return "";

            var outname = name;
            char flagref = name[0];
            if (flags.ContainsKey(name)) {
                flag = flags[name];
            } else if (flag_refs.ContainsKey(flagref)) {
                flag = flags[flag_refs[flagref]];
                outname = flag_refs[flagref];
            }
            return outname;
        }



        public void Parse(string[] args) {
            string arg, avalue;
            int cmd_len;
            CLIFlag found_flag;
            bool skip_next = false;

            cmd_len = args.Length;
            for (var n = 0; n < cmd_len; n++) {
                arg = args[n];
                //// SKIP NON SWITCH VALUES
                if (arg[0] != '-' || skip_next) {
                    skip_next = false;
                    continue;
                }

                //// GET THE ACTUAL ARG USED IN THE HASHTABLE
                //// SET THE FOUND FLAG
                arg = GetFlag(arg.Substring(1), out found_flag);
                if (!string.IsNullOrEmpty(found_flag.name)) {
                    if (found_flag.flag == "string") {
                        avalue = args[n + 1];
                        found_flag.value = avalue.Trim();
                        skip_next = true;
                    } else {
                        found_flag.value = "true";
                    }
                    flags[arg.Trim()] =  found_flag;
                }
            }
        }


        // TODO: REMOVE PROGRAM SPECIFIC HELP KEYS AND ADD OPTION TO ENTRY FOR HELP TABLE
        public void execute_help() {
            CLIFlag found_flag;
            var keys = new List<string>();

            foreach (var key in flags.Keys) {
                if (flags[key].display_in_help) {
                    keys.Add(key);
                }
            }

            // NEED TO ITERATE OVER HASH MAP.... RUROH// :(
            Console.WriteLine("");
            Console.WriteLine("COMMAND HELP");
            Console.WriteLine("");
            int maxLength = 0;
            foreach (var key in keys) {
                maxLength = (key.Trim().Length > maxLength) ? key.Trim().Length : maxLength;
            }
            foreach (var key in keys) {
                if (key.Trim().Length == 0) continue;
                found_flag = flags[key.Trim()];

                if (found_flag.name.Trim().Length != 0) {
                    var ldiff = maxLength - found_flag.name.Trim().Length;
                    var padding = new string(' ', ldiff);
                    var abbrv = "     ";
                    if (found_flag.abbrev != 0) abbrv = " (-"+found_flag.abbrev+")";

                    Console.WriteLine("-" + found_flag.name.Trim() + padding + abbrv+ " : "+found_flag.description);
                }
            }

            Console.WriteLine("");

        }


        // -----------------------------------------------------------
        // MODULE public voidS
        // -----------------------------------------------------------
        public void print_header(string message, int lines, int spacebefore, int spaceafter) {
            print_banner("START", lines, spacebefore, spaceafter);
        }

        public void print_footer(string message, int lines, int spacebefore, int spaceafter) {
            print_banner("END", lines, spacebefore, spaceafter);
        }

        public void  print_banner(string message, int lines = 2, int spacebefore = 0, int spaceafter = 0) {
            for(var i = 0; i < spacebefore; i++) {
                Console.WriteLine("");
            }
            print_bars(lines, true);
            Console.WriteLine(message);
            print_bars(lines, false);
            for (var i = 0; i < spaceafter; i++) {
                Console.WriteLine("");
            }
        }


        public void print_bars(int count, bool iseven) {

            bool doprint;

            for (var i = 0; i < count; i++) {
                doprint = iseven ? i%2 == 0: i%2 == 0;
                if (doprint) {
                    Console.WriteLine("------------------------------------------------------------");

                }
            }
        }



    } // END CLASS
} // END NAMESPACE
