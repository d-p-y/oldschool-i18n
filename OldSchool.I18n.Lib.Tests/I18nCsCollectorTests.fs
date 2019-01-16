namespace OldSchool.I18n.Lib.Tests

open Xunit
open OldSchool.I18n.Parsing
open Xunit.Abstractions

type CsExtractorTests(log:ITestOutputHelper) = 
    let i18Class = "I18n"
    let i18Method = "Translate"
        
    let logFunc = (fun x -> (x ()) |> log.WriteLine)
    
    [<Fact>]
    let ``csharp line - ignore single line comments`` () =
        let sut = Extractor logFunc
        let found = sut.ExtractCs i18Class i18Method """c:\smthelse""" """
            // I18n.Translate("ignore") */
            I18n.Translate("text to be matched {0} {1}")"""
        Assert.Equal<Item list>([{Item.Msg = "text to be matched {0} {1}"; Item.Row = 2; File = """c:\smthelse"""}], found)

    [<Fact>]
    let ``csharp line - ignore multi line comments`` () =
        let sut = Extractor logFunc
        let found = sut.ExtractCs i18Class i18Method """c:\smthelse""" """
            /* 
            I18n.Translate("ignore") 
            */
            I18n.Translate("text to be matched {0} {1}")"""
        Assert.Equal<Item list>([{Item.Msg = "text to be matched {0} {1}"; Item.Row = 4; File = """c:\smthelse"""}], found)

    [<Fact>]
    let ``csharp line - lots of whitespace`` () =
        let sut = Extractor logFunc
        let found = sut.ExtractCs i18Class i18Method """c:\smthelse""" """
                string.Format(
                I18n
                .
                Translate  /*
             sdgfsdf*/ 
               ( //sdfsfsdf
              /* dfgdg */   //
                 "text to be matched {0} {1}"
                ), 'a', 123); """

        Assert.Equal<Item list>([{Item.Msg = "text to be matched {0} {1}"; Item.Row = 8; File = """c:\smthelse"""}], found)

    [<Fact>]
    let ``csharp line - embedded without whitespace`` () =
        let sut = Extractor logFunc
        let found = sut.ExtractCs i18Class i18Method """c:\smthelse""" """
            public void Aaa() {
                string.Format(I18n.Translate("text to be matched {0} {1}"), 'a', 123);
            } """

        Assert.Equal<Item list>([{Item.Msg = "text to be matched {0} {1}"; Item.Row = 2; File = """c:\smthelse"""}], found)

    [<Fact>]
    let ``csharp - simple raw string message`` () =
        let sut = Extractor logFunc
        let found = sut.ExtractCs i18Class i18Method """c:\smth""" """I18n.Translate("foo")"""

        Assert.Equal<Item list>([{Item.Msg = "foo"; Item.Row = 0; File = """c:\smth"""}], found)
         
    [<Fact>]
    let ``csharp - simple verbatim string message`` () =
        let sut = Extractor logFunc
        let found = sut.ExtractCs i18Class i18Method """c:\smth""" """I18n.Translate(@"foo")"""

        //take values from https://msdn.microsoft.com/en-us/library/h21280bw.aspx
        Assert.Equal<Item list>([{Item.Msg = """foo"""; Item.Row = 0; File = """c:\smth"""}], found)
         
    [<Fact>]
    let ``csharp - escaping in verbatim string message`` () =
        let sut = Extractor logFunc
        let src = "\nI18n.Translate(@\"a\n\"\"b\"\"\"\"c\nde\nfg h\")"
        let fact = sut.ExtractCs i18Class i18Method "c:\smth" src
        let exp = "a\n\"b\"\"c\nde\nfg h"

        //only quoting needed is that '""' means '"'
        Assert.Equal<Item list>([{Item.Msg = exp; Item.Row = 1; File = """c:\smth"""}], fact)

    [<Fact>]
    let ``csharp - escaping in raw string`` () =
        let sut = Extractor logFunc
        let src = "I18n.Translate(\"a\\tb\\\"c\\n\")"
        let fact = sut.ExtractCs i18Class i18Method """c:\smth""" src
        let exp = "a\tb\"c\n"

        //use https://msdn.microsoft.com/en-us/library/h21280bw.aspx
        Assert.Equal<Item list>([{Item.Msg = exp; Item.Row = 0; File = """c:\smth"""}], fact)
