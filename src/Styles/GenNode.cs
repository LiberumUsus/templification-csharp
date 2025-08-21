using System.Text.RegularExpressions;
using Templification.Styles;
using Templification.Utils;

namespace Templification.Styles {

    class PatNode : GenNode {
        public string        value       = "";
        public List<Rule>    defaultRule = new List<Rule>();
        public List<GenNode> children    = new List<GenNode>();
    }

    class RuleNode : GenNode {
        public string     value = "";
        public List<Rule> rules = new List<Rule>();
    }

    class SubstNode : GenNode {
        public List<string> value    = new List<string>();
        public bool         autounit = true;
        public string       pname    = "";
        public List<Rule>   rules    = new List<Rule>();
    }

    class KeyModNode : GenNode {
        public List<string> value   = new List<string>();
        public List<string> key_mod = new List<string>();
    }

    public class GenNode {

        public List<Rule> find_rules(string cls_part, StyleGenerator gen_list, bool is_negative, string defaultUnit = "px") {
            var rules   =  new List<Rule>();
            var current =  cls_part.AllBefore("-");

            if (defaultUnit == "_") defaultUnit = "";

            if (this is PatNode ) {
                var pnode  =  this as PatNode;
                if (pnode == null) return rules;

                // PATTERN MATCH AND CHILD ITERATION
                if (pnode.value == current ) {
                    if (current == cls_part ) {
                        rules = pnode.defaultRule;
                    } else {
                        foreach (var child in pnode.children ) {
                            rules = child.find_rules(cls_part.AllAfter("-"), gen_list, is_negative);
                            if (rules.Count > 0 ) {
                                break;
                            }
                        }
                    }
                    // LOGIC REPLACEMENT
                } else if (pnode.value == "%" ) {
                    var nparts  = gen_list.num_based[current].Split(':');
                    var useUnit = "px";
                    gen_list.current_key = nparts[0];
                    if (nparts.Length > 1) useUnit = nparts[1];
                    foreach (var child in pnode.children ) {
                        rules = child.find_rules(cls_part.AllAfter("-"), gen_list, is_negative, useUnit);
                        if (rules.Count > 0 ) {
                            break;
                        }
                    }
                    // PATTERN VARIABLE REPLACEMENT
                } else if (pnode.value.StartsWith("{")) {
                    var map_name  = pnode.value.FindBetween("{", "}");
                    var smap      = new Dictionary<string,string>(gen_list.map_o_maps[map_name]);
                    var map_value =  "";
                    if (smap.ContainsKey(cls_part)) {
                        map_value = smap[cls_part];
                    } else if (map_name == "color") {
                        // TRY TO REPLACE COLOR BY STANDARD COLORS
                        if (StyleGenerator.is_hex_color(cls_part)) {
                            map_value = "#" + cls_part;
                        } else {
                            // TODO: UGLY HACK, THIS NEEDS TO BE CHANGED SO THAT THIS
                            // COLOR REPLAMENT WON"T REPLACE ANYTHING THAT IS NOT A COLOR (THIS SETUP)
                            // OR NEVER GETS CALLED IN THE FIRST PLACE
                            map_value = "";
                        }
                    }
                    if (map_value.Length > 0 ) {
                        var prules  =  pnode.defaultRule;
                        var i = 0;
                        foreach (var prule in prules ) {
                            prules[i++].rvalue = prule.rvalue.Replace("{" + map_name + "}", map_value);
                        }
                        rules = prules;
                    }
                }
            } else if (this is RuleNode ) {
                var rnode  = this as RuleNode;
                if (rnode != null && rnode.value == current ) {
                    return rnode.rules;
                }
            } else if (this is SubstNode ) {
                current = cls_part;
                var snode  =  this as SubstNode;
                if (snode != null && snode.value.Contains(current) ) {
                    var srules  =  snode.rules;
                    for (var i = 0; i < srules.Count; i++) {
                        srules[i].rvalue = srules[i].rvalue.Replace("{value}", current);
                    }
                    return srules;
                } else if (snode != null) {
                    // TRY REGEX MATCHING
                    var trex    = new Regex(snode.value[0]);
                    var matches = trex.Match(current.Trim());
                    if (matches.Success) {
                        var vdirs =  new List<string>();
                        var vdir  =  matches.Groups["dir"].Value;
                        foreach (var cdir in vdir ) {
                            if (gen_list.locations[cdir.ToString()].Contains('|') ) {
                                foreach (var subs in gen_list.locations[cdir.ToString()].Split('|') ) {
                                    vdirs.Add(subs);
                                }

                            } else {
                                vdirs.Add(gen_list.locations[cdir.ToString()]);
                            }
                        }
                        if (snode.pname == "rounded" ) {
                            for (var i = 0; i < vdirs.Count; i++) {
                                if (vdirs.Contains("top") ) {
                                    var tindx  =  vdirs.IndexOf("top");
                                    if (tindx + 1 < vdirs.Count ) {
                                        vdirs[tindx] = "top-" + vdirs[tindx + 1];
                                        vdirs[tindx + 1] = "";
                                    }
                                }
                                if (vdirs.Contains("bottom") ) {
                                    var tindx  =  vdirs.IndexOf("bottom");
                                    if (tindx + 1 < vdirs.Count ) {
                                        vdirs[tindx] = "bottom-" + vdirs[tindx + 1];
                                        vdirs[tindx + 1] = "";
                                    }
                                }
                            }
                        }
                        var vvalue  =  matches.Groups["value"].Value;
                        if (is_negative ) {
                            vvalue = "-" + vvalue;
                        }

                        var vunit = (matches.Groups["unit"].Value.Length > 0) ? matches.Groups["unit"].Value : defaultUnit;
                        vunit     = StyleGenerator.parse_unit(vunit);

                        if (!snode.autounit || snode.pname == "z") {
                            vunit = "";
                        }
                        // CONSIDER MAKING GENERIC WITH PNAME
                        if (snode.pname == "color" ) {
                            var smap  =  new Dictionary<string,string>(gen_list.map_o_maps["color"]);
                            if (smap.ContainsKey(vvalue) ) {
                                vvalue = smap[vvalue];
                            } else {
                                vvalue = (StyleGenerator.is_hex_color(vvalue) ) ?  "#" + vvalue  :  vvalue ;
                            }
                            vunit = "";
                        }

                        if (snode.value[0].Contains("operation")) {
                            var orules = snode.rules.clone();

                            for (var i = 0; i < orules.Count; i++) {
                                var key = orules[i].key.Replace("{base}", gen_list.current_key);
                                var rvalue = orules[i].rvalue;
                                foreach (var grpName in trex.GetGroupNames()) {
                                    var nvalue = matches.Groups[grpName].Value;
                                    var varId  = "{" + grpName + "}";
                                    key    = key.Replace(varId, nvalue);
                                    rvalue = rvalue.Replace(varId, nvalue);
                                }
                                orules[i].key    = key;
                                orules[i].rvalue = rvalue;
                            }

                            return orules;

                        } else if (vvalue.Length > 0 ) {
                            var trules  =  new List<Rule>();
                            if (vdirs.Count > 0 ) {
                                foreach (var direction in vdirs ) {
                                    if (direction.Length <= 0 ) {
                                        continue;
                                    }
                                    var srules = snode.rules.clone();
                                    for (var i = 0; i < srules.Count; i++) {
                                        if (srules[i].rvalue.Contains("{unit}")) {
                                            srules[i].rvalue = srules[i].rvalue.Replace("{unit}", vunit);
                                            srules[i].rvalue = srules[i].rvalue.Replace("{value}", vvalue);
                                        } else {
                                            srules[i].rvalue = srules[i].rvalue.Replace("{value}", vvalue + vunit);
                                        }
                                        srules[i].key = srules[i].key.Replace("{dir}", direction);
                                        if (srules[i].key.Contains("{base}")) {
                                            srules[i].key = srules[i].key.Replace("{base}", gen_list.current_key);
                                        }
                                    }
                                    trules.AddRange(srules);
                                }
                                return trules;
                            } else {
                                var srules = new List<Rule>(snode.rules);
                                for (var i = 0; i < srules.Count; i++) {
                                    if (srules[i].rvalue.Contains("{unit}")) {
                                        srules[i].rvalue = srules[i].rvalue.Replace("{unit}", vunit);
                                        srules[i].rvalue = srules[i].rvalue.Replace("{value}", vvalue);
                                    } else {
                                        srules[i].rvalue = srules[i].rvalue.Replace("{value}", vvalue + vunit);
                                    }
                                    if (srules[i].key.Contains("{base}")) {
                                        srules[i].key = srules[i].key.Replace("{base}", gen_list.current_key);
                                    }
                                }
                                return srules;
                            }
                        }
                    }
                }
            }
            return rules;
        }

    }  // END CLASS
}  // END NAMESPACE
