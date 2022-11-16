using System.Text.RegularExpressions;
using Templification.Utils;

namespace Templification.Styles {


    public enum OpType {
        simple,
        color,
        multi,
        empty
    }

    public class StyleGenerator {
        private const string hchars = "ABCDEFabcdef0123456789";

        public string current_key = "";
        public Dictionary<string,List<Rule>> style_map = new Dictionary<string,List<Rule>>();
        public Dictionary<string,Dictionary<string,string>> map_o_maps = new Dictionary<string,Dictionary<string,string>>();
        public Dictionary<string,string> locations  = new Dictionary<string,string> {
            {"t", "top"},
            {"b", "bottom"},
            {"l", "left"},
            {"r", "right"},
            {"x", "left|right"},
            {"y", "top|bottom"},
        };
        public Dictionary<string,string> units = new Dictionary<string,string>{
            {"per", "%"},
            {"r", "rem"},
            {"p", "px"}
        };
        // MAKE THIS MORE PROGRAMATIC :/
        public Dictionary<string,string> oddities = new Dictionary<string,string>{
            {"py", "p-y"},
            {"px", "p-x"},
            {"pl", "p-l"},
            {"pr", "p-r"},
            {"pt", "p-t"},
            {"pb", "p-b"},
            {"my", "m-y"},
            {"mx", "m-x"},
            {"ml", "m-l"},
            {"mr", "m-r"},
            {"mt", "m-t"},
            {"mb", "m-b"}
        };
        public Dictionary<string,string> num_based = new Dictionary<string,string>{
            {"p" , "padding"},
            {"m" , "margin"},
            {"h" , "height"},
            {"w" , "width"},
            {"t" , "top"},
            {"l" , "left"},
            {"r" , "right"},
            {"b" , "bottom"},
            {"underline" , "text-underline-offset"},
            {"maxw" , "max-width"},
            {"minw" , "min-width"},
            {"maxh" , "max-height"},
            {"minh" , "min-height"},
            {"gap" , "gap"},
            {"outline" , "outline-width"}
        };

        public static Dictionary<string,GenNode> init_generator() {
            var rule_map   = new Dictionary<string,GenNode>();
            var font_nodes = new PatNode {
                value = "font",
                children = new List<GenNode>{
                    new SubstNode{
                        value = new List<string>{@"(?<value>\d+)"},
                        autounit = false,
                        rules = new List<Rule>{new Rule{
                                key = "font-weight",
                                rvalue = "{value}"
                            }}
                        },
                    new SubstNode {
                        value = new List<string>{@"h-(?<value>\d+)(?<unit>-\S*)?"},
                            autounit = true,
                            rules = new List<Rule>{new Rule{
                                key = "font-size",
                                rvalue = "{value}"
                            }}
                        }
                }
            };
            var bg_nodes = new  PatNode{
                value = "bg",
                children = new List<GenNode>{
                    new PatNode{
                        value = "{color}",
                        defaultRule = new List<Rule>{new Rule{
                                key = "background",
                                rvalue = "{color}"
                            }}
                    }}
                };
            var text_nodes  = new  PatNode{
                value = "text",
                children = new List<GenNode>{
                    new PatNode{
                        value = "{color}",
                        defaultRule = new List<Rule>{new Rule{
                                    key = "color",
                                    rvalue = "{color}",
                            }}
                    }}
            };
            var fill_nodes  = new  PatNode{
                value = "fill",
                children = new List<GenNode>{
                    new PatNode{
                        value = "{color}",
                        defaultRule = new List<Rule>{new Rule{
                                key = "fill",
                                rvalue = "{color}",
                            }
                        }
                    }
                }
            };
            var rounded_nodes  = new  PatNode{
                value = "rounded",
                defaultRule = new List<Rule>{new Rule{
                        key = "border-radius",
                        rvalue = "0.25rem",
                    }},
                children = new List<GenNode>{
                    new SubstNode{
                        value = new List<string>{@"^(?<dir>[tlrbxy]+)-(?<value>\d+)(?<unit>-\S*)?"},
                        rules = new List<Rule>{new Rule{
                                key = "border-{dir}-radius",
                                    rvalue = "{value}"
                            }},
                            pname = "rounded",
                    },
                    new SubstNode{
                        value = new List<string>{@"^(?<value>\d+)(?<unit>-\S+)?"},
                        rules = new List<Rule>{new Rule{
                                key = "border-radius",
                                    rvalue = "{value}"
                            }}
                        }
                }
            };
            var border_nodes  = new  PatNode{
                value = "border",
                defaultRule = new List<Rule>{new Rule{
                        key = "border-width",
                        rvalue = "1px"},
                    new Rule{
                        key = "border-style",
                        rvalue = "solid",
                    }},
                children = new List<GenNode>{
                    new SubstNode{
                        value = new List<string>{@"^(?<dir>[tlrbxy]+)-(?<value>[a-fA-F0-9]{6})"},
                            pname = "color",
                            rules = new List<Rule>{new Rule{
                                key = "border-{dir}-color",
                                    rvalue = "{value}"
                                }}
                        },
                    new SubstNode {
                        value = new List<string>{@"^(?<dir>[tlrbxy]+)-(?<value>[a-zA-Z]+(-\d+)?)"},
                            pname = "color",
                            rules = new List<Rule>{new Rule{
                                key = "border-{dir}-color",
                                    rvalue = "{value}"
                                }}
                        },
                    new SubstNode{
                        value = new List<string>{@"^(?<dir>[tlrbxy]+)-(?<value>\d+)(?<unit>-\S*)?"},
                        rules = new List<Rule>{new Rule{
                                key = "border-{dir}-width",
                                    rvalue = "{value}"
                                }, new Rule{
                                key = "border-{dir}-style",
                                    rvalue = "solid",
                            }}
                        },
                    new SubstNode{
                        value = new List<string>{@"^(?<value>\d+)(?<unit>-\S+)?"},
                        rules = new List<Rule>{new Rule{
                                key = "border-width",
                                    rvalue = "{value}"
                                }, new Rule{
                                key = "border-style",
                                rvalue = "solid",
                            }}
                        },
                    new PatNode{
                        value = "{color}",
                        defaultRule = new List<Rule>{new Rule{
                                key = "border-color",
                                    rvalue = "{color}",
                            }}
                        },
                }
            };
            var num_nodes  = new  PatNode{
                value = "%",
                defaultRule = new List<Rule>{new Rule{
                        key = "{base}",
                        rvalue = "1px",
                    }},
                children = new List<GenNode>{
                    new SubstNode{
                        value = new List<string>{@"(?<dir>[tlrbxy]+)-(?<value>\d+)(?<unit>-\S*)?"},
                        rules = new List<Rule>{new Rule{
                                key = "{base}-{dir}",
                                rvalue = "{value}"
                            }}
                        },
                    new SubstNode{
                        value = new List<string>{@"(?<value>\d+)(?<unit>-\S+)?"},
                        rules = new List<Rule>{new Rule{
                                key = "{base}",
                                rvalue = "{value}"
                            }}
                    },
                }
            };

            rule_map["num_based"] = num_nodes;
            rule_map["fill"]      = fill_nodes;
            rule_map["rounded"]   = rounded_nodes;
            rule_map["border"]    = border_nodes;
            rule_map["font"]      = font_nodes;
            rule_map["text"]      = text_nodes;
            rule_map["bg"]        = bg_nodes;

            // generate_rules_from_strings(rule_map)

            return rule_map;
        }

        public static void generate_generatable(StyleSheet master, Dictionary<string,bool> class_list, CmdLineOptions cmd_options) {
            var gen_list  = new StyleGenerator();
            var styledir  = cmd_options.style_dir;
            // #TODO: Consider more flexible loading of style files
            var color_file = cmd_options.color_file;
            var rules_file = cmd_options.rules_file;
            gen_list.style_map = populate_stylemap_from_strings(styledir, rules_file);

            populate_simple_maps(gen_list, styledir, color_file);
            foreach (var kp in class_list ) {
                if (!kp.Value) {
                    generate_class(kp.Key, master, gen_list);
                }
            }
            for (var i = 0; i < master.classes.Count; i++) {
                var cclass = master.classes[i];
                foreach (var rule in cclass.rules ) {
                    if (rule.@type == RuleType.apply ) {
                        var classes  =  rule.rvalue.Split(" ");
                        foreach (var cls in classes ) {
                            if (!class_list.ContainsKey(cls) ) {
                                var new_class  =  generate_class(cls, master, gen_list);
                                if (new_class.names.Count == 1 && new_class.names[0].StartsWith(".") ) {
                                    // Prevent duplicate generation
                                    class_list[new_class.names[0][1..]] = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        static CClass generate_class(string cls, StyleSheet master, StyleGenerator gen_list) {
            var is_negative =  false;
            var baseclass   =  cls;
            var out_class   = new CClass();
            // Check for negatives
            if (cls.StartsWith("-") ) {
                is_negative = true;
                baseclass = baseclass[1..];
            }

            if (gen_list.style_map.ContainsKey(cls)) {
                out_class = new CClass {
                    names = new List<string>{"." + cls},
                    rules = new List<Rule>(),
                };
                out_class.rules.AddRange(gen_list.style_map[cls]);
                master.classes.Add(out_class);
            } else {
                var gnodes     = init_generator();
                var first_name = baseclass.AllBefore("-");

                // Check for odd patterns
                if (gen_list.oddities.ContainsKey(first_name) ) {
                    baseclass = baseclass.Replace(first_name, gen_list.oddities[first_name]);
                    first_name = baseclass.AllBefore("-");
                }

                if (gen_list.num_based.ContainsKey(first_name) ) {
                    first_name = "num_based";
                }
                var node  = gnodes.ContainsKey(first_name) ? gnodes[first_name] : new GenNode();
                var rules = node.find_rules(baseclass, gen_list, is_negative);
                if (rules.Count > 0 ) {
                    out_class = new CClass {
                        names = new List<string>{"." + cls},
                        rules = rules,
                    };
                    master.classes.Add(out_class);
                }
            }
            return out_class;
        }

        public static string parse_unit(string unit) {
            var new_unit  =  unit;
            var gen_list  = new StyleGenerator();
            if (unit.Length == 0 ) {
                new_unit = "px";
            } else if (gen_list.units.ContainsKey(unit) ) {
                new_unit = gen_list.units[unit];
            }
            return new_unit;
        }

        public static bool is_hex_digit(char chr) {
            var index = hchars.IndexOf(chr);
            return (index >= 0);
        }

        public static bool is_hex_color(string color) {
            var is_hex  =  true;
            foreach (var chr in color ) {
                is_hex = is_hex_digit(chr);
                if (!is_hex ) {
                    break;
                }
            }
            return is_hex;
        }

        static Dictionary<string,List<Rule>> populate_stylemap_from_strings(string style_dir, string file_name) {
            var lregex    = new Regex(@"\s+");
            var style_map = new Dictionary<string,List<Rule>>();

            var css_content = File.ReadAllLines(style_dir + file_name);
            if (css_content.Length <= 0 ) {
                return style_map;
            }

            foreach (var line in css_content ) {
                if (line.StartsWith("#") ) {
                    continue;
                }
                // FIND FIRST WHITESPACE CHAR IN STRING
                var wsindex  =  lregex.Match(line).Index;
                var key_rule =  line.SplitAt(wsindex);
                var key      =  key_rule[0].Trim();
                var rules    =  new List<Rule>();
                if (key_rule.Length <= 1 ) {
                    continue;
                }
                foreach (var srule in key_rule[1].Trim().Split(";") ) {
                    if (srule.Length > 0 ) {
                        rules.Add(new Rule{
                                key = srule.AllBefore(":").Trim(),
                                rvalue = srule.AllAfter(":").Trim(),
                                type = RuleType.standard
                            });
                    }
                }
                style_map[key] = rules;
            }

            return style_map;
        }

        static void populate_simple_maps(StyleGenerator gen_list, string style_dir, string color_file) {
            // #TODO: Replace with better/actual implementation
            var css_content  = File.ReadAllLines(style_dir + color_file);
            if (css_content.Length <= 0 ) {
                return;
            }
            var colors  =  new Dictionary<string,string>();
            foreach (var line in css_content ) {
                if (line.StartsWith("#") ) {
                    continue;
                }
                var parts  =  line.Split(":");
                colors[parts[0]] = parts[1];
            }
            gen_list.map_o_maps["color"] = new Dictionary<string, string>(colors);
        }

    }  // END CLASS
}  // END NAMESPACE
