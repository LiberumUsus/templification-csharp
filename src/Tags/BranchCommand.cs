using System.Text.RegularExpressions;

namespace Templification.Tags {


    public class BranchCommand {

        public string name    = "";
        public string attribs = "";

        public int index      =  0;
        public int last_block = -1;
        public int next_block = -1;


        public BranchCommand clone() {
            var cloned = new BranchCommand();
            cloned.name       = this.name;
            cloned.attribs    = this.attribs;
            cloned.index      = this.index;
            cloned.last_block = this.last_block;
            cloned.next_block = this.next_block;
            return cloned;
        }

        public void init(string text, int index) {
            //(self BranchCommand)
            var pat_command  = @"{[#:/](?<name>\w*)\s*(?<attribs>.*)}";
            var command_ex  = new Regex(pat_command); // or  panic(err)
            var trimmed_text  =  text.Trim();
            var matches = command_ex.Match(trimmed_text);
            if (matches.Success) {
                this.name = matches.Groups["name"].Value;
                this.attribs = matches.Groups["attribs"].Value;
                this.index = index;
            }
        }

        public string get_name() {
            //(self BranchCommand)
            return this.name;
        }

        public void process_command(TagBranch usage) {
            //(self BranchCommand)
            // switch(this.name) {
            //  "if" {}
            //  "else" {}
            //  "endif" {}
            //}
        }

    }  // END CLASS
}  // END NAMESPACE
