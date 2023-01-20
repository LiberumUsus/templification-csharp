
namespace Templification.Styles {
    public class WatchSegment {

        public string value = "";
        public int    index;
        public int    offset;
        public int    matches;
        public int    chrmatched;
        public bool   indexouter = false;


        public bool consume(char chr, bool insensitive) {
            //(self WatchSegment)
            if (this.value.Length == 0 ) {
                return false;
            }
            // Clear matches if starting fresh
            if (this.index == -1 ) {
                this.index = 0;
                this.chrmatched = 0;
            }

            var vindex  = this.special_char_check();
            var matched = false;

            if (this.value[vindex] == chr
                || (insensitive && this.value.ToLower()[vindex] == chr)
                || (insensitive && this.value.ToUpper()[vindex] == chr) ) {
                this.chrmatched += 1;
                if (vindex > this.index ) {
                    this.index += 1;
                }
                this.index += 1;
                matched = (this.index == this.value.Length);
                if (matched ) {
                    this.matches += 1;
                }
            } else if (this.index < vindex ) {
                this.chrmatched += 1;
            } else {
                // Reset index anytime a match fails
                // matches must be continuous
                this.index = -1;
            }
            if (this.index >= this.value.Length ) {
                this.index = -1;
            }
            return matched;
        }

        int special_char_check() {
            //(self WatchSegment)
            var return_index  =  this.index;

            if (this.value[this.index] == '\\' ) {
                if (this.index + 1 < this.value.Length && this.value[this.index + 1] == '*' ) {
                    this.index += 1;
                    return_index = this.index;
                }
            }
            if ((this.value[this.index] == '*' && this.index == 0)
                || (this.value[this.index] == '*' && this.value[this.index - 1] != '\\') ) {
                if ((this.index - 1 >= 0 && this.value[this.index - 1] != '\\') || this.index - 1 == -1 ) {
                    return_index = this.index + 1;
                }
            }
            return return_index;
        }
    }
} // END NAMESPACE
