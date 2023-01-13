using System.Text.RegularExpressions;

namespace Templification.Utils {

    public static class SUtil {

        public static string[] SplitAt(this string input, int index) {
            var len = input.Length;
            if (index <= 0)   return new string[]{input, ""};
            if (index >= len) return new string[]{"", input};

            var split_str = new string[2];
            split_str[0] = input.Substring(0, index);
            split_str[1] = input.Substring(index);
            return split_str;
        }

        public static string FindBetween(this string input, string start, string end) {
            var indexStart = input.IndexOf(start);
            if (indexStart < 0) return input;
            var indexEnd   = input.IndexOf(end, indexStart+1);
            if (indexEnd < 0) return input.Substring(indexStart +1);
            return input.Substring(indexStart+1, indexEnd-1);
        }

        public static int IndexOfNth(this string input, string search, int start, int nth) {
            if (nth < 1) return -1;
            if (nth == 1) return input.IndexOf(search, start);
            var index = input.IndexOf(search, start);
            if (index == -1) return -1;
            return input.IndexOfNth(search, index + 1, --nth);
        }

        public static string[] SplitNth(this string input, string search, int nth) {
            if (nth == 0) {
                return input.Split(search);
            }
            var index = input.IndexOfNth(search, 0, nth);
            if (index < 0) return new string[] {input};
            var firstpart = input.Substring(0, index);
            var lastpart  = input.Substring(index+1);
            var parts = firstpart.Split(search);
            var all = new string[parts.Length + 1];
            parts.CopyTo(all, 0);
            all[all.Length-1] = lastpart;
            return all;
        }

        public static string AllBefore(this string input, string search) {
            var index = input.IndexOf(search);
            if (index < 0) return input;
            return input.Substring(0, index);
        }

        public static string AllAfter(this string input, string search) {
            var index = input.IndexOf(search);
            if (index < 0) return input;
            return input.Substring(index+1);
        }

    }



    public static class Utils {

        // Attempt to open a file for writing and create path if necessary
        // based on user response or CLI arguments
        public static int open_file_with_hand_holding(string path_to_file, out FileStream? opened_file, CmdLineOptions options, string throwMessage = "") {
            FileAttributes fattribs = new FileAttributes();
            var path_exists = File.Exists(path_to_file);
            var dir_path    = Path.GetDirectoryName(path_to_file);

            if (path_exists) fattribs = File.GetAttributes(path_to_file);

            // Not a file... bork!
            if (path_exists && !fattribs.HasFlag(FileAttributes.Normal)) {
                opened_file = null;
                return -1;
            } else if (!path_exists) {
                try {
                    var make_dir = options.auto_make_dirs;

                    if (!options.auto_make_dirs && !Directory.Exists(dir_path)) {
                        Console.WriteLine("Create output directory? (y/n): " + dir_path);
                        var response = Console.ReadLine();
                        make_dir = (response == "y" || response == "Y");
                    }
                    if (make_dir) {
                        Directory.CreateDirectory((string.IsNullOrEmpty(dir_path) ? "" : dir_path));
                    }
                } catch {
                    opened_file = null;
                    if (throwMessage != "") {
                        throw new Exception(throwMessage);
                    }
                    return -2;
                }

            }
            opened_file = File.Open(path_to_file, FileMode.Create);
            return 0;
        }

        // return the file stem of a file name or path to a file.
        // I.E. `/home/user/file.txt` would become `file`<br/>
        // and `just_a_file.txt` would become `just_a_file`
        public static string file_stem(string str) {
            var dot_index =  -1;
            var basename  =  Path.GetFileName(str);
            dot_index = basename.LastIndexOf(".");
            return basename.Substring(0, dot_index);
        }

        // Returns the parent of the file path
        // i.e. `/home/user/file.txt` would become `/home/user`<br/>
        // and `file.txt` would become `.`
        public static string file_parent(string str) {
            var fname  =  Path.GetFileName(str);
            if (fname.Length > 0 ) {
                return new DirectoryInfo(str).Parent.ToString();
            } else {
                return str;
            }
        }

        // Replace str content with new content given the replacement range
        // Adjust an offset to indicate how positions after the replacement have been offset
        public static string replace_string_adjust_offset(string str, string content, int start, int end, ref int offset) {
            var csize  =  content.Length;
            var ssize  =  str.Length;

            // BAIL IF INDEXES DONT LINE UP
            if (end < start || start < 0 || end > ssize ) {
                return str;
            }

            var bsize       =  end - start;
            var new_offset  =  offset + (csize - bsize);
            var new_string  =  str[..start] + content + str[end..];

            offset = new_offset;

            return new_string;
        }

        // Convert an array of locations to an array of groups
        public static List<Re_group> make_location_groups(List<Match> locations, int fill_index) {
            var groups  = new List<Re_group>();
            var findex  =  0;

            foreach (var location in locations) {
                var start = location.Index;
                var end   = location.Length + location.Index;
                if (fill_index > 0 ) {
                    if (findex < start ) {
                        groups.Add(new Re_group {
                              start = findex,
                              end = start,
                            });
                    }
                    findex = end;
                }
                groups.Add(new Re_group{
                        start = start,
                        end = end,
                    });
            }

            if (fill_index > 0 && findex > 0 ) {
                groups.Add(new Re_group{
                        start = findex,
                        end = fill_index,
                    });
            }

            return groups;
        }

        //    return the negative group of the provided group
        public static List<Re_group> make_negative_groups(List<Re_group> groups, int length) {
            var new_groups  = new List<Re_group>();
            var findex  =  0;

            if (groups.Count > 0 ) {
                foreach (var group in groups ) {
                    new_groups.Add(new Re_group{
                            start = findex,
                                end = group.start,
                                });
                    findex = group.end;
                }
                if (new_groups.Last().end < length ) {
                    new_groups.Add(new Re_group{
                            start = new_groups.Last().end,
                            end = length,
                        });
                }
            }
            return new_groups;
        }

        //    Merge groups based on pairs and provide the reduced groups
        public static List<Re_group> merge_pair_groups(List<Re_group> groups, int length) {
            var new_groups  = new List<Re_group>();

            for (var i = 0; i < (groups.Count / 2); i++ ) {
                new_groups.Add(new Re_group{
                        start = groups[i * 2].start,
                        end = groups[(i * 2) + 1].end
                    });
            }

            return new_groups;
        }

        // return string thes of the locations matched in a string source
        public static List<string> match_strings_from_location_ints(List<Match> locations, string source, bool fill_gaps) {
            var string_matches  =  new List<string>();
            var fill_index = (fill_gaps ) ?  source.Length  :  0 ;
            var groups  =  make_location_groups(locations, fill_index);

            foreach (var group in groups ) {
                if (group.start < 0 || group.end <= group.start || group.end > source.Length ) {
                    continue;
                }
                string_matches.Add(source[group.start..group.end]);
            }

            return string_matches;
        }

        // Ensure a map has an entry, this is mostly used for hashmaps of arrays so that
        // the array value is initialized.
        public static void ensure_map_has_entry<T>(string key, T value, Dictionary<string,T> mapping ) {
            if (!mapping.ContainsKey(key) ) {
                mapping[key] = value;
            }
        }



        // Is value or values in bounds string of.
        // This funtion will check any number int ofegers and ALL must be
        // within the bounds of string the for the result to be true.
        public static bool in_bounds(string source, params int[] n) {
            var is_bounded  =  false;

            foreach (var x in n ) {
                is_bounded = x <= source.Length && x > -1;
                if (!is_bounded ) {
                    break;
                }
            }

            return is_bounded;
        }

        // This function will split the `source` string into an array
        // based on any of the deliminators provided.
        // i.e `source` = `"Testing my:function"` `delims` = `[" ",":"]`.
        // Result would be: `["Testing","my","function"]`
        public static List<string> split_any(string source, List<string> delims) {
            if (delims.Count <= 0 ) {
                return new List<string>();
            }
            var new_source = source;
            var fvalue     =  delims[0];
            var pastfirst  = false;
            // REPLACE ALL  OTHERS WITH THE FIRST VALUE
            foreach (var istr in delims ) {
                if (pastfirst) {
                    new_source = new_source.Replace(istr, fvalue);
                }
                pastfirst = true;
            }
            return new_source.Split(fvalue).ToList();
        }

        // Determine if string the is just blank space with a newline terminator
        public static bool empty_with_newline(string source) {
            var just_empty_with_newline  =  false;

            var parts  =  source.Split(new[]{ Environment.NewLine }, StringSplitOptions.None);
            if (parts.Length > 1 ) {
                just_empty_with_newline = (parts[0].Trim() + parts[1].Trim()).Length <= 0;
            }

            return just_empty_with_newline;
        }

        // Returns string a with the contents between the `start` and `end` deliminators
        // cleared of content.
        // i.e. Given the start value `{` and the end value `}` the input `source` value <i>"This {example} makes it clear {to you} right?"</i>
        // will become <i>"This makes it clear right?"</i>.
        public static string clear_between(string source, string start, string end) {
            return clear_between_with_regex(source, start, end, ".*");
        }


        // Returns string a with the contents between the `start` and `end` deliminators
        // cleared of content.
        // i.e. Given the start value `{` and the end value `}` the input `source` value <i>"This {example} makes it clear {to you} right?"</i>
        // will become <i>"This makes it clear right?"</i>.
        public static string clear_between_with_regex(string source, string start, string end, string inner_regex) {
            var rgex  = new  Regex(start + inner_regex + end) ; // or   new regex.RE();
            var new_value  =  "";

            if (!(source.Contains(start) && source.Contains(end)) ) {
                return source;
            }
            var tmatchs = rgex.Matches(source);

            var group_matches  =  make_location_groups(tmatchs.ToList(), 0);
            var b  =  0;
            var d  =  0;
            foreach (var group in group_matches ) {
                d = group.start;
                if (group.end > group.start ) {
                    new_value += source[b..d];
                } else {
                    new_value = source;
                }
                b = group.end;
            }
            if (group_matches.Count > 0 && b < source.Length ) {
                new_value += source[b..];
            } else if (group_matches.Count <= 0 ) {
                new_value = source;
            }

            return new_value;
        }

        // Pretty <b>`:hacky:`</b> function. Exists explictly to strip the
        // string value `-ss` and all content afterwords off of
        // string a objects value.
        public static string strip_id(string attrib) {
            var end_index  = attrib.LastIndexOf("-ss") ; // or  attrib.len
            return attrib[0..end_index];
        }

        public static string replace_class_with_prefix(string current, string addition) {
            var out_string  =  "";
            foreach (var item in current.Split(" ") ) {
                var matched_one  =  false;
                foreach (var new_item in addition.Split(" ") ) {
                    var prefix  =  new_item.AllAfter("-ss");
                    if (item.StartsWith(prefix) ) {
                        matched_one = true;
                    }
                }
                if (!matched_one ) {
                    out_string += item + " ";
                }
            }

            return out_string + addition;
        }


        // Print a formatted table and add new lines
        public static void print_tableln<T>(T item, List<int> widths, string delim) {
            print_table(item + "\n",widths,delim);
        }



        // A fairly `:hacky:` function that can be used to print a
        // width formatted group of text.
        // No example right now, its a pretty rough function and
        // probably won"t work as expected
        public static void print_table(string item, List<int> widths, string delim) {
            if (item == null) return;
            var str_parts  =  item.Split(delim);
            var width  =  0;
            var pstr  =  "";

            var i = 0;
            foreach (var istr in str_parts ) {
                var padding  =  "";
                pstr = istr;
                if (i < widths.Count ) {
                    width = widths[i];
                }
                if (width > istr.Length ) {
                    padding = new string(' ',width - istr.Length);
                } else if (width > 0 ) {
                    pstr = istr[0..width] + "";
                }
                if (i < (str_parts.Length - 1)) {
                    Console.Write(pstr + delim + padding);
                } else {
                    Console.Write(pstr + padding);
                }
                i++;
            }
        }

    }  // END CLASS
}  // END NAMESPACE
