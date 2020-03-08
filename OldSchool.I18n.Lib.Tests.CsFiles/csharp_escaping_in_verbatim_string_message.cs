using OldSchool.I18n.Lib.Tests.CsFiles;
class csharp_escaping_in_verbatim_string_message {
    void A() {
        I18n.Translate(@"a\n""b""""c\nde\nfg h\"); //only quoting needed is that '""' means '"'
    }
}
