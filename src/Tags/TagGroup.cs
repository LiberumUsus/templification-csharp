namespace Templification.Tags {

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
        empty,
    }

    public class TagGroup {
        public int start;
        public int end;

        public TagType    type;
        public TagSubType sub_type;

    }  // END CLASS
}  // END NAMESPACE
