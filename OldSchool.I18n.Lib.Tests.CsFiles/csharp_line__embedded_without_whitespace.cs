using OldSchool.I18n.Lib.Tests.CsFiles;
class csharp_line__embedded_without_whitespace {
    public void Aaa() {
        string.Format(I18n.Translate("text to be matched {0} {1}"), 'a', 123);
    }
}
