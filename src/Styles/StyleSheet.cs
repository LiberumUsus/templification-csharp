using System.Text;
using Microsoft.VisualBasic;

namespace Templification.Styles {


    public class StyleSheet {

        public Dictionary<string,int>  class_map  = new Dictionary<string,int>();
        public List<CClass>            classes    = new List<CClass>();
        public Dictionary<string,Rule> style_cmds = new Dictionary<string, Rule>();
        public int id = -1;


        public StyleSheet clone() {
            //(StyleSheet self)
            return new StyleSheet {
                class_map = new Dictionary<string, int>(this.class_map),
                classes   = new List<CClass>(this.classes),
                id = this.id,
                style_cmds = new Dictionary<string, Rule>(this.style_cmds),
            };
        }

        public void add_class(CClass cls) {
            Random rand = new Random();
            //(StyleSheet self)
            if (cls.uid <= -1 ) {
                cls.uid = rand.Next();
            }
            this.classes.Add(cls);
        }

        public void insert_class(CClass cls, int index) {
            //(StyleSheet self)
            Random rand = new Random();
            if (cls.uid <= -1 ) {
                cls.uid = rand.Next();
            }
            if (index >= 0 && this.classes.Count > index ) {
                this.classes.Insert(index, cls);
            } else if (index < 0 ) {
                this.classes.Prepend(cls);
            } else {
                this.classes.Add(cls);
            }
        }

        public void merge(StyleSheet other) {
            //(StyleSheet self)
            foreach (var oclass in other.classes ) {
                var can_add  =  true;
                // IF CLASS NAMES MATCH, ENSURE THE ID"s DO NOT
                if (this.has_class(oclass.names[0]) ) {
                    var cclass  =  this.get_class(oclass.names[0], false);
                    can_add = !(cclass.id == oclass.id);
                }
                if (oclass.names.Count > 1 || can_add ) {
                    this.add_class(oclass);
                    this.add_class_to_map(oclass, this.classes.Count - 1, false);
                }
            }
        }

        public void assign_id(int id) {
            //(StyleSheet self)
            this.id = id;
            foreach (var cclass in this.classes ) {
                if (cclass.id == -1 ) {
                    cclass.id = id;
                }
            }
        }

        public void apply_applies() {
            //(StyleSheet self)
            // MAKE SURE ITS ALL MAPPED
            this.map_classes(false);

            // ITERATE OVER APPLIES AND APPLY RULES
            foreach (var cclass in this.classes ) {
                for (var i = 0; i < cclass.rules.Count; i ++) {
                    var rrule = cclass.rules[i];
                    if (rrule.type == RuleType.apply ) {
                        var wrote_first = false;
                        var class_list  = rrule.rvalue.Split(" ");
                        foreach (var lclass in class_list ) {
                            var dclass  =  "." + lclass;
                            if (!this.class_map.ContainsKey(dclass) ) {
                                continue;
                            }
                            var map_class  =  this.get_class(dclass, false);
                            foreach (var mcrule in map_class.rules ) {
                                if (!wrote_first ) {
                                    cclass.rules[i] = mcrule;
                                    wrote_first = true;
                                } else {
                                    cclass.rules.Add(mcrule);
                                }
                            }
                        }
                    } else if (rrule.@type == RuleType.local ) {
                        if (!cclass.names[0].StartsWith(".") && !cclass.names[0].StartsWith("#")
                            && cclass.id > 0 ) {
                            cclass.names[0] = cclass.names[0] + ".ss" + cclass.id.ToString();
                            cclass.local = true;
                            cclass.rules.RemoveAt(i);
                            continue;
                        }
                    } else if (rrule.@type == RuleType.global ) {
                        cclass.global = true;
                        cclass.id = -1;
                    } else if (rrule.@type == RuleType.importance ) {
                        cclass.importance = Int32.Parse(rrule.rvalue);
                    }
                }
            }
        }

        public void map_classes(bool with_id) {
            //(StyleSheet self)
            Random rand = new Random();
            var index = 0;
            this.class_map = new Dictionary<string,int>();
            foreach (var cls in this.classes ) {
                if (cls.uid <= -1 ) {
                    cls.uid = rand.Next();
                }
                this.add_class_to_map(cls, index, with_id);
                index++;
            }
        }

        void add_class_to_map(CClass cls, int index, bool with_id) {
            //(StyleSheet self)
            foreach (var name_block in cls.names ) {
                foreach (var cname in name_block.Split(" ") ) {
                    var fname  =  cname;
                    if (with_id && cls.id > 0 && !cls.global && fname != "active" ) {
                        fname += "-ss" + cls.id.ToString();
                    }
                    if (!this.class_map.ContainsKey(fname) ) {
                        this.class_map[fname] = index;
                    }
                }
            }
        }

        public bool has_class(string name) {
            //(StyleSheet self)
            var adjusted_name = (name.StartsWith(".") ) ?  name  :  "." + name ;

            return this.class_map.ContainsKey(adjusted_name);
        }

        public CClass get_class(string name, bool strict) {
            //(StyleSheet self)
            var adjusted_name = name;
            if (name.StartsWith(".")) {
                adjusted_name = name;
            } else if (!strict ) {
                adjusted_name = "." + name;
            } else {
                adjusted_name = name;
            }

            if (this.class_map.ContainsKey(adjusted_name) ) {
                var out_class  =  this.classes[this.class_map[adjusted_name]];
                out_class.sheet_index = this.class_map[adjusted_name];
                return out_class;
            } else {
                return  new CClass();
            }
        }

        public string str() {
            //(StyleSheet self)
            var out_build   =  new StringBuilder(200);
            var hold_levels =  new Dictionary<int,StringBuilder>();
            foreach (var cclass in this.classes ) {
                if (cclass.importance > 1 ) {
                    if (!hold_levels.ContainsKey(cclass.importance) ) {
                        hold_levels[cclass.importance] = new StringBuilder(200);
                    }
                    hold_levels[cclass.importance].Append(cclass.str());
                } else {
                    out_build.Append(cclass.str());
                }
            }
            var hkeys = hold_levels.Keys.ToList();
            hkeys.Sort();
            foreach (var key in hkeys ) {
                out_build.Append(hold_levels[key].ToString());
            }
            return out_build.ToString();
        }

        // MERGE STYLESHEETS, STRIP UNUSED CLASSES, GENERATE GENERATABLE CLASSES
        // AND APPLY APPLIES
        public StyleSheet polish_style_sheets(Dictionary<string,StyleSheet> all_sheets , StyleSheet master, Dictionary<string,bool> class_list , CmdLineOptions cmd_options) {
            // APPLY APPLIES
            master.apply_applies();
            // STRIP UNUSED CLASSES
            separate_classes(master, class_list);
            // GENERATE GENERATIBLE
            StyleGenerator.generate_generatable(master, class_list, cmd_options);
            // MERGE ALL SHEETS
            foreach (var ssheet in all_sheets.Values ) {
                master.merge(ssheet);
            }

            // APPLY APPLIES ONE MORE TIME FOR GENERATE ITEMS
            master.apply_applies();
            // PURGE UNUSED STYLES
            purge_style_sheet(master, class_list);
            return master;
        }

        public void separate_classes(StyleSheet master, Dictionary<string,bool> class_list ) {
            master.map_classes(true);
            // ITERATE OVER CLASS LIST AND DETERMINE USE
            foreach (var key in class_list.Keys) {
                class_list[key] = master.has_class(key);
            }
        }

        void purge_style_sheet(StyleSheet master, Dictionary<string,bool> class_list ) {
            var delete_list = new List<int>();
            var unique_ids  = new Dictionary<int,bool> ();
            // ITERATE OVER MASTER AND REMOVE ALL CLASSES THAT ARE NOT IN LIST
            var Index = 0;
            foreach (var cclass in master.classes ) {
                if (!unique_ids.ContainsKey(cclass.uid) && cclass.uid > -1 ) {
                    unique_ids[cclass.uid] = true;
                } else {
                    delete_list.Add(Index);
                }
                Index++;
            }
            delete_list.Reverse();
            foreach (var index in delete_list ) {
                master.classes.RemoveAt(index);
            }
        }
    }  // END CLASS
}  // END NAMESPACE
