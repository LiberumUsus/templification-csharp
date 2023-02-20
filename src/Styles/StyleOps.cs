using System.Text;
using Templification.Utils;

namespace Templification.Styles {

    public class StyleOps {

        // Parse a CSS file or style block
        public static StyleSheet parse_styles(string source) {
            var classes           = new List<CClass>();
            var rand              = new Random();
            var style_sheet_cmds  = new Dictionary<string,Rule>();

            var current_class = new CClass{
                uid = rand.Next()
            };

            var cwatcher    = new Watcher("comment", "/\\* \\*/ \\").offsets(-1, 0);
            var cmd_watcher = new Watcher("cmds", "@ ; \\");
            var swatcher    = new Watcher("string", "\" \" \\");
            var bwatcher    = new Watcher("block", "{ } \\");
                bwatcher.can_nest = true;

            var rwatcher  = new Watcher("rule", ": ; \\");
            rwatcher.active = false;
            rwatcher.must_confirm = true;

            var nwatcher  = new Watcher("name", "} { \\").offsets(1, -1);
            nwatcher.set_start(0, WatchStage.second);


            var watchers        =  new List<Watcher>{cwatcher, swatcher, nwatcher, bwatcher, rwatcher, cmd_watcher};
            var style_sheet_id  =  rand.Next();
            var i = 0;

            foreach (var chr in source ) {
                foreach (var watch in watchers ) {
                    var points = watch.consume(chr, i);
                    var mpoint = points[0];

                    if (mpoint.b >= 0 ) {
                        if (watch.name == "comment" && mpoint.d < mpoint.b ) {
                            if (bwatcher.is_searching() ) {
                                bwatcher.suspend(true, i - 1);
                            }
                            if (nwatcher.is_searching() ) {
                                nwatcher.suspend(true, i - 1);
                            }
                            if (rwatcher.is_searching() ) {
                                rwatcher.suspend(true, i - 1);
                            }
                        }
                        if (watch.name == "rule" && mpoint.d < mpoint.b) {
                            if (bwatcher.is_searching()) {
                                bwatcher.suspend(true, i -1);
                            }
                        }
                        if (watch.name == "block" && mpoint.d <= 0 ) {
                            rwatcher.active = true;
                            rwatcher.set_start(i + 1, WatchStage.first_confirm);
                            if (cmd_watcher.is_searching() ) {
                                cmd_watcher.reset(false);
                            }
                        }

                        // SECOND POINT OBTAINED
                        if (mpoint.d > 0 ) {
                            var bound  =  watch.pop_match();
                            if (watch.name == "comment" ) {
                                bwatcher.suspend(false, i + 1);
                                nwatcher.suspend(false, i + 1);
                                rwatcher.suspend(false, i + 1);
                            }
                            if (watch.name == "rule") {
                                bwatcher.suspend(false, i + 1);
                            }

                            // CLASS && RULE CREATING
                            var line_value  =  get_source_section(source, bound, watch.pop_suspends());

                            // REPORTING SECTION
                            if (watch.reporting) {
                                Console.WriteLine("[" + watch.name + "]\t>>");
                                Console.WriteLine(line_value);
                            }

                            if (watch.name == "name" ) {
                                var nnames  =  line_value.Split(",");
                                foreach (var cnname in nnames ) {
                                    current_class.names.Add(cnname.Trim());
                                }
                            } else if (watch.name == "rule" ) {
                                if (bwatcher.is_searching() ) {
                                    watch.debug = false;
                                    var new_rule  = new Rule(line_value);
                                    current_class.rules.Add(new_rule);
                                    rwatcher.set_start(i + 1, WatchStage.first_confirm);
                                }
                            } else if (watch.name == "block" ) {
                                if (current_class.names.Count > 0 ) {
                                    classes.Add(current_class);
                                }
                                current_class = new CClass {
                                    uid = rand.Next(),
                                };
                                rwatcher.set_start(-1, WatchStage.first);
                                rwatcher.active = false;
                            } else if (watch.name == "cmds" ) {
                                if (bwatcher.is_searching() ) {
                                    var new_rule  = new Rule(line_value);
                                    current_class.rules.Add(new_rule);
                                    if (new_rule.type == RuleType.local ) {
                                        current_class.local = true;
                                    }
                                    rwatcher.reset(true);
                                    rwatcher.set_start(i + 1, WatchStage.first_confirm);
                                } else {
                                    if (line_value.ToLower().StartsWith("@style-id") ) {
                                        var new_rule  = new Rule(line_value);
                                        style_sheet_cmds[new_rule.key] = new_rule;
                                    }
                                    nwatcher.set_start(i + 1, WatchStage.second);
                                }
                            }
                        }
                    }
                }
                i++;
            }

            var style_sheet  = new  StyleSheet{
                classes = classes,
                id = style_sheet_id,
                style_cmds = style_sheet_cmds,
            };
            style_sheet.map_classes(true);
            style_sheet.apply_applies();

            return style_sheet;
        }

        static string get_source_section(string source, Utils.Bounds bound, List<Utils.Bounds> suspends) {
            var sbuild =  new StringBuilder(50);
            var start  =  bound.b;

            if (bound.b < 0 || bound.d > source.Length ) {
                Console.WriteLine("Not good" + bound.b + "::" + bound.d);
                return "";
            }
            foreach (var sup in suspends ) {
                if (sup.b >= 0 && sup.d < source.Length && start >= 0 && start < sup.b ) {
                    sbuild.Append(source[start..sup.b].Trim());
                    start = sup.d;
                }
            }
            if (start >= 0 && start < bound.d && bound.d < source.Length ) {
                sbuild.Append(source[start..bound.d]);
            }
            return sbuild.ToString().Trim();
        }

    }  // END CLASS
}  // END NAMESPACE
