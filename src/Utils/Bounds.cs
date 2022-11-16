
namespace Templification.Utils {
    // Bounds object, basic tuple int for positions and bounds.
    // The letters b and d are used instead of x and y; `b` represents the start `d` represents the end &#030;
    // These letters were chosen because of the visual bounds of `bd`.
    public class Bounds {
        public int b = -1;
        public int d = -1;

        // Override of `str()` function used for printing objects in `V`.
        // Prints the `bd` values as `(b, d)`.
        // i.e where `b=1` & `d=2` returns the string `(1,2)`
        public string str() {
            return "($this.b, $this.d)";
        }

        // Indicates if the provided `x` and `y` values are within the
        // bounds of the `bd` values. Logical true for `inclusive` causes
        // the funtion to return true if `x` = `b` and/or `y` = `d`
        public bool inbounds(int x, int y, bool inclusive) {
            if (inclusive) {
                return this.b >= x && this.d <= y;
            } else {
                return this.b > x && this.d < y;
            }
        }
    }  // END CLASS
}  // END NAMESPACE
