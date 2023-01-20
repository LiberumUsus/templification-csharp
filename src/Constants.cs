

namespace Templification {

        // Tags type enum, for <start> </end> and <single/> style tags
        public enum TagType {
            start,
            end,
            single,
            root,
            text,
            empty,
        }

        public enum TagSubType {
            command,
            variable,
            style,
            script,
            void_exact,
            cshtml,
            comment,
            template,
            filedetails,
            empty,
        }

    public static class APP {
        public const string TAG_SPACE     = "wspace";
        public const string TAG_NAME      = "name";
        public const string TAG_ATTRIBS   = "attribs";
        public const string TEXT_NAME     = "text";
        public const string ROOT_NAME     = "root";
        public const string TEXT_NEWLINE  = "\n";


        public const string SUB_TYPE_SCRIPT      = "script";
        public const string SUB_TYPE_STYLE       = "style";
        public const string SUB_TYPE_VOIDEX      = "void_exact";
        public const string SUB_TYPE_RAZOR       = "cshtml";
        public const string SUB_TYPE_COMMENTS    = "comments";
        public const string SUB_TYPE_TEMPLATE    = "template";
        public const string SUB_TYPE_FILEDETAILS = "!templification";

        public const string PREFIX_TEMPLATE        = "__";
        public const string PREFIX_INTERNAL_ATTRIB = "__";


        public const string ATTRIB_FLAG_OVERRIDE = "o";
        public const string ATTRIB_FLAG_DELETE   = "d";
        public const string ATTRIB_FLAG_SUBST    = "t";

    }

}
