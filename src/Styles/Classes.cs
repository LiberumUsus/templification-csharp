using Templification.Utils;
using System.Text;

namespace Templification.Styles {

    public enum ClassType {
        clas,
        multiset,
        id,
    };

    public class CClass {

        public int          uid         = -1;
        public List<string> names       = new List<string>();
        public List<Rule>   rules       = new List<Rule>();
        public ClassType    type        = new ClassType();
        public int          id          = -1;
        public int          importance  = 1;
        public bool         global      = false;
        public bool         local       = false;
        public int          sheet_index = 0;


        public CClass clone() {
            //(self CClass)
            Random rand = new Random();
            return new CClass{
                uid        = rand.Next(),
                names      = new List<string>(this.names),
                rules      = new List<Rule>(this.rules),
                type       = this.type,
                id         = this.id,
                importance = this.importance,
                global     = this.global,
                local      = this.local,
            };
        }

        public string str() {
            //(self CClass)
            var out_build  =  new StringBuilder(100);
            var Index = 0;
            out_build.Append("\n");
            var many_names  =  this.names.Count > 2;
            foreach (var name in this.names ) {
                if (Index++ > 0 ) {
                    out_build.Append(",");
                    if (many_names ) {
                        out_build.Append("\n");
                    } else {
                        out_build.Append(" ");
                    }
                }
                var name_parts  =  name.Split(" ");
                var j = 0;
                foreach (var npart in name_parts ) {
                    var npname = (npart.LastIndexOf(":") >= 0) ? npart.AllBefore(":") : npart;
                    var npqual = (npart.IndexOf(":") >= 0) ? npart.AllAfter(":") : npart;
                    if (npqual == npname ) {
                        npqual = "";
                    }
                    out_build.Append(npname.Replace("/", "\\/"));
                    // SUPER HACKY FIXME!!
                    if (npart.StartsWith(".") && this.id > 0 && !this.global && npart != ".active" ) {
                        out_build.Append("-ss" + this.id.ToString());
                    }
                    if (npqual.Length > 0 ) {
                        out_build.Append(":" + npqual);
                    }
                    if (j < name_parts.Length - 1 ) {
                        out_build.Append(" ");
                    }
                    j++;
                }
            }

            out_build.Append(" {\n");
            foreach (var rule in this.rules ) {
                if (rule.type != RuleType.importance && rule.type != RuleType.global ) {
                    out_build.Append("  " + rule.str());
                    out_build.Append("\n");
                }
            }

            out_build.Append("}\n");
            return out_build.ToString();
        }

        public List<string> get_names_with_ids() {
            //(self CClass)
            var out_list  =  new List<string>();
            foreach (var name in this.names ) {
                var name_parts  =  name.Split(" ");
                foreach (var npart in name_parts ) {
                    if (npart.StartsWith(".") && this.id > 0 && !this.global ) {
                        out_list.Add(npart + "-ss" + this.id.ToString());
                    } else {
                        out_list.Add(npart);
                    }
                }
            }
            return out_list;
        }


    }  // END CLASS
}  // END NAMESPACE
