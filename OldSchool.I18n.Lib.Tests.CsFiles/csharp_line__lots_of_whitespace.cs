using OldSchool.I18n.Lib.Tests.CsFiles;
class csharp_line__lots_of_whitespace {
    void Mth() {
        string.Format(
            I18n
                .
                Translate  /*
             sdgfsdf*/
                ( //sdfsfsdf
                    /* dfgdg */   //
                    "text to be matched {0} {1}"
                ), 'a', 123);
    }
}
