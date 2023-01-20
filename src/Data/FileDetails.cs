using System.Text.RegularExpressions;

namespace Templification.Data {
    public class FileDetails {
        private Regex validFileName = new Regex("[^?%*:|\"<>,;=\\/]+[^?%*:|\"<>,;=\\/.]");
        private string _fileOutName = string.Empty;

        public string FileOutName {
            get { return _fileOutName;}
            set {
                if (validFileName.Match(value).Success) {
                    _fileOutName = value.Trim();
                } else {
                    _fileOutName = string.Empty;
                }
            }
        }


        public FileDetails clone() {
            var cloned = new FileDetails();
            cloned._fileOutName = this._fileOutName;
            return cloned;
        }

    } // END CLASS
} // END NAMESPACE
