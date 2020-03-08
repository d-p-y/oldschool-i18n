using OldSchool.I18n.Lib.Tests.CsFiles;

public static class StringExtension {
    public static string MessageFormat(this string self, params object[] args) => string.Format(self, args);
}

class csharp__complicated_locations {
    string SomeProp => I18n.Translate("test1 {0} and {1}").MessageFormat(1, 2);

    string A() {
#pragma warning disable 8321
        string LocFun()
#pragma warning restore 8321
        {
            return I18n.Translate("test2 {0} and {1}");
        }

        var x = "".Length % 5 == 0 ? "" : null; //something appearing as non const for compiler
        return x ?? string.Format(I18n.Translate("test3 {0} and {1}"), 1, 2);
    }
}
