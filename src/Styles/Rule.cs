using Templification.Utils;

namespace Templification.Styles {

   public enum RuleType {
        standard,
        apply,
        importance,
        local,
        global,
        style_cmd,
    }

   public static class RuleExtensions {
       public static List<Rule> clone(this List<Rule> rules) {
            List<Rule> clones = new List<Rule>();
            foreach (var rule in rules) {
                clones.Add(new Rule());
                clones.Last().key    = rule.key;
                clones.Last().rvalue = rule.rvalue;
                clones.Last().type   = rule.type;
            }
            return clones;
        }
   }

    public class Rule {

        private string   Key;
        private string   Value;
        public  bool     IsImportant = false;
        public  RuleType type = new RuleType();

        public string key {
            get { return Key; }
            set { Key = value.Trim(); }
        }
        public string rvalue {
            get { return Value; }
            set { Value = value.Trim(); }
        }


        public Rule() {
            Key = "";
            Value = "";
        }

        public Rule(string line_rule) {
            Key = "";
            Value = "";
            this.init(line_rule);
        }

        public string str() {
            //(self Rule)
            if (string.IsNullOrEmpty(this.key)) return "";
            return this.key + " : "+ this.rvalue + (this.IsImportant ? " !important" : "") + ";";
        }

        public void init(string rule) {
            //(self Rule)
            var parts  =  rule.SplitNth(":", 1);
            var rtype  =  RuleType.standard;

            if (parts.Length < 2 ) {
                parts = rule.SplitNth(" ", 1);
            }
            if (parts[0].StartsWith("@") ) {
                switch(parts[0].ToLower().Trim()) {
                    case "@apply":
                        rtype = RuleType.apply;
                        break;
                    case "@local":
                        rtype = RuleType.local;
                        break;
                    case "@global":
                        rtype = RuleType.global;
                        break;
                    case "@importance":
                        rtype = RuleType.importance;
                        break;
                    default:
                        rtype = RuleType.standard;
                        break;
                }
            }

            this.key = parts[0].Trim();
            this.type = rtype;
            if (parts.Length > 1 ) {
                this.rvalue = parts[1].Trim().Replace(";", "");
            }
        }
    }  // END CLASS
}  // END NAMESPACE
