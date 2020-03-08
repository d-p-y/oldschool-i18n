using OldSchool.I18n.Lib.Tests.CsFiles;
class csharp_line__ignore_multi_line_comments {
    void Mth() {
        /* 
        I18n.Translate("ignore") 
        */
        I18n.Translate("text to be matched {0} {1}");
    }
}
