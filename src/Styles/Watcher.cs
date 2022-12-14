using Templification.Utils;

namespace Templification.Styles {

    public enum WatchStage {
        zero,
        first,
        first_confirm,
        second,
    }

    public class WatchSegment {

        public string value = "";
        public int    index;
        public int    offset;
        public int    matches;
        public int    chrmatched;
        public bool   indexouter;


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


    public class Watcher {

        public WatchSegment begin_chr = new WatchSegment();
        public WatchSegment end_chr   = new WatchSegment();
        public WatchSegment escape    = new WatchSegment();
        public string      ignore = "";
        public List<Bounds> matches = new List<Bounds>();
        public WatchStage stage = WatchStage.first;
        public bool suspended = false;
        public bool active    = true;
        public List<Bounds> suspends = new List<Bounds>();
        public List<Bounds> mlengths = new List<Bounds>();
        public bool can_nest     {get; set;} = false;
        public bool must_confirm {get; set;} = false;
        public bool insensitive  {get; set;} = false;
        public bool reporting    {get; set;} = true;
        public bool debug        {get; set;} = false;
        public string name = "";


        public List<Utils.Bounds> consume(char chr, int index) {
            //(self Watcher)
            var match_point = new Utils.Bounds {
                b = -1,
                d = -1
            };
            var match_lengths = new Utils.Bounds {
                b = -1,
                d = -1
            };

            // IF MATCHING SUSPENDED OR WATCHER INACTIVE, RETURN NO MATCH
            if (this.suspended || !this.active ) {
                return new List<Bounds>{match_point, match_lengths};
            }

            var record_index =  -1;
            var matched_to   =  WatchStage.zero;

            if (this.begin_chr.consume(chr, this.insensitive) ) {
                record_index = index + this.begin_chr.offset;

                if (this.begin_chr.indexouter ) {
                    record_index -= this.begin_chr.chrmatched;
                }
                matched_to = WatchStage.first;
                this.end_chr.index = 0;
            } else if (this.end_chr.consume(chr, this.insensitive) ) {
                record_index = index + this.end_chr.offset;
                if (!this.begin_chr.indexouter ) {
                    record_index -= this.begin_chr.chrmatched;
                }
                matched_to = WatchStage.second;
            }

            switch(matched_to) {
                case WatchStage.first: {
                    switch(this.stage) {
                        case WatchStage.first: {
                            if (!this.must_confirm ) {
                                this.matches.Add(new Bounds{
                                        b = record_index,
                                    });
                                this.mlengths.Add(new Bounds{
                                        b = this.begin_chr.chrmatched,
                                    });
                                if (!this.can_nest ) {
                                    this.stage = WatchStage.second;
                                }
                                match_point.b = this.matches.Last().b;
                            }
                            break;
                        }
                        case WatchStage.first_confirm: {
                            if (this.has_first() ) {
                                this.stage = WatchStage.second;
                            }
                            break;
                        }
                        case WatchStage.second: {
                            // RESET START IF CANT NEST AND FOUND SECOND STARTER
                            if (!this.can_nest && this.has_first() ) {
                                if (this.begin_chr.value != this.end_chr.value ) {
                                    this.matches.Last().b = record_index;
                                    this.mlengths.Last().b = this.begin_chr.chrmatched;
                                }
                            }
                            break;
                        }
                        default: {break;
                        }
                    }
                    break;
                }
                case WatchStage.second: {
                    if (this.has_first() && (this.stage == WatchStage.second || (this.can_nest && !this.must_confirm)) ) {
                        this.stage = WatchStage.first;
                        if (this.matches.Last().d < 0 ) {
                            this.matches.Last().d = record_index + 1;
                            this.mlengths.Last().d = this.end_chr.chrmatched;
                        } else {
                            // SEARCH BACKWARDS TO FILL EMBEDDED MATCH
                            var i = 0;
                            this.matches.Reverse();
                            foreach (var mm in  this.matches) {
                                if (mm.d < 0 ) {
                                    this.matches[i].d = record_index + 1;
                                    if (this.mlengths.Count > i && i > 0 ) {
                                        this.mlengths[i].d = this.end_chr.chrmatched;
                                    }
                                    break;
                                }
                                i++;
                            }
                            this.matches.Reverse();
                        }

                        // RECORD THE LAST MATCH TO match_point
                        match_point = this.matches.Last();
                        match_lengths = this.mlengths.Last();
                    } else if (this.stage != WatchStage.second ) {
                        // RESET, NEVER FOUND FIRST
                        this.reset(false);
                    }
                    break;
                }
                default: {break;}
            }


            return new List<Bounds>{match_point, match_lengths};
        }

        public bool has_first() {
            //(self Watcher)
            return this.matches.Count > 0 && this.matches.Last().b >= 0;
        }

        public bool has_last() {
            //(self Watcher)
            return this.matches.Count > 0 && this.matches.Last().d > 0;
        }

        public bool is_searching() {
            //(self Watcher)
            return this.has_first() && this.matches.Last().d < 0;
        }


        public Bounds pop_match() {
            //(self Watcher)
            if (this.matches.Count > 0 ) {
                var lastItem = this.matches.Last();
                this.mlengths.RemoveAt(this.mlengths.Count-1);
                this.matches.RemoveAt(this.matches.Count-1);
                return lastItem;
            } else {
                return new Utils.Bounds();
            }
        }

        public List<Bounds> pop_suspends() {
            //(self Watcher)
            var suspends  =  new List<Bounds>(this.suspends);
            this.suspends = new List<Bounds>();
            return suspends;
        }

        public void suspend(bool suspended, int index) {
            //(self Watcher)
            if (suspended && !this.suspended ) {
                this.suspended = suspended;
                this.suspends.Add(new Bounds{
                        b = index,
                            });
            } else if (this.suspended && !suspended && this.suspends.Count > 0 ) {
                this.suspended = suspended;
                if (index > this.suspends.Last().b ) {
                    this.suspends.Last().d = index;
                } else {
                    this.suspends.RemoveAt(this.suspends.Count-1);
                }
            }
        }

        public void reset(bool hard) {
            //(self Watcher)
            if (hard) {
                this.matches = new List<Bounds>();
            } else if (this.matches.Count > 0 && this.matches.Last().b >= 0 ) {
                if (this.mlengths.Count > 0) this.mlengths.RemoveAt(this.mlengths.Count-1);
            }
            this.stage = WatchStage.first;
        }

        public Watcher set_start(int value, WatchStage new_stage) {
            //(self Watcher)
            // POPULATE MATCHES IF MISSING
            if (this.matches.Count == 0 ) {
                this.matches.Add(new Bounds());
            }
            if (this.mlengths.Count == 0 ) {
                this.mlengths.Add(new Bounds());
            }
            // SET INITIAL MATCH LOCATION TO "value"
            this.matches.Last().b = value;
            // SKIP MATCHING FIRST TO first_confirm
            if (new_stage != WatchStage.zero ) {
                this.stage = new_stage;
            }
            return this;
        }

        public void offsets(int start_offset, int end_offset) {
            //(self Watcher)
            this.begin_chr.offset = start_offset;
            this.end_chr.offset = end_offset;
        }

        public void useouter(bool use) {
            //(self Watcher)
            this.begin_chr.indexouter = true;
            this.end_chr.indexouter = true;
        }

        public Watcher init(string name, string strings) {
            //(self Watcher)
            this.name = name;
            this.active = true;
            this.matches = new List<Bounds>();
            this.mlengths = new List<Bounds>();
            this.can_nest = false;
            var parts  =  strings.Split(" ");
            if (parts.Length > 0 ) {
                this.begin_chr = new WatchSegment {
                    value = parts[0],
                    index = 0
                };
            }
            if (parts.Length > 1 ) {
                this.end_chr = new WatchSegment {
                    value = parts[1],
                    index = 0
                };
                this.can_nest = this.end_chr.value == this.begin_chr.value;
            }
            if (parts.Length > 2 ) {
                this.escape = new WatchSegment {
                    value = parts[2],
                    index = 0
                };
            }
            if (parts.Length > 3 ) {
                this.ignore = parts[3];
            }
            return this;
        }

    }  // END CLASS
}  // END NAMESPACE
