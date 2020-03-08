using OldSchool.I18n.Lib.Tests.CsFiles;
class csharp__escaping_in_raw_string {
    void A() {
        I18n.Translate("a\tb\"c\n"); //use https://msdn.microsoft.com/en-us/library/h21280bw.aspx
    }
}
