namespace OldSchool.I18n.Lib.Tests

open Xunit
open OldSchool.I18n.Parsing
open Xunit.Abstractions
open System.IO

type CsExtractorTests(log:ITestOutputHelper) = 
    let i18Class = "I18n"
    let i18Method = "Translate"
        
    let logFunc = (fun x -> (x ()) |> log.WriteLine)
    let fileName = """c:\smthelse"""

    let doTest csFileName =
        let filePath = Path.Combine("CsFiles", csFileName)
        CsExtractor.ExtractCs i18Class [i18Method] fileName (File.ReadAllText filePath)

    [<Fact>]
    let ``csharp line - ignore single line comments`` () =
        let found = doTest "csharp_line__ignore_single_line_comments.cs"
        Assert.Equal<Item list>([{Item.Msg = "text to be matched {0} {1}"; Item.Row = 4; File = fileName}], found)

    [<Fact>]
    let ``csharp line - ignore multi line comments`` () =
        let found = doTest "csharp_line__ignore_multi_line_comments.cs"
        Assert.Equal<Item list>([{Item.Msg = "text to be matched {0} {1}"; Item.Row = 6; File = fileName}], found)

    [<Fact>]
    let ``csharp line - lots of whitespace`` () =
        let found = doTest "csharp_line__lots_of_whitespace.cs"
        Assert.Equal<Item list>([{Item.Msg = "text to be matched {0} {1}"; Item.Row = 4; File = fileName}], found)

    [<Fact>]
    let ``csharp line - embedded without whitespace`` () =
        let found = doTest "csharp_line__embedded_without_whitespace.cs"
        Assert.Equal<Item list>([{Item.Msg = "text to be matched {0} {1}"; Item.Row = 3; File = fileName}], found)

    [<Fact>]
    let ``csharp - simple raw string message`` () =
        let found = doTest "csharp__simple_raw_string_message.cs"
        Assert.Equal<Item list>([{Item.Msg = "foo"; Item.Row = 3; File = fileName}], found)
         
    [<Fact>]
    let ``csharp - simple verbatim string message`` () =
        let found = doTest "csharp__simple_verbatim_string_message.cs"
        Assert.Equal<Item list>([{Item.Msg = """foo"""; Item.Row = 3; File = fileName}], found)
         
    [<Fact>]
    let ``csharp - escaping in verbatim string message`` () =
        let found = doTest "csharp_escaping_in_verbatim_string_message.cs"
        let exp = "a\\n\"b\"\"c\\nde\\nfg h\\"
                
        Assert.Equal<Item list>([{Item.Msg = exp; Item.Row = 3; File = fileName}], found)

    [<Fact>]
    let ``csharp - escaping in raw string`` () =
        let found = doTest "csharp__escaping_in_raw_string.cs"
        let exp = "a\tb\"c\n"
        Assert.Equal<Item list>([{Item.Msg = exp; Item.Row = 3; File = fileName}], found)

    [<Fact>]
    let ``csharp - complicated locations`` () =
        let found = doTest "csharp__complicated_locations.cs"
        Assert.Equal<Item list>(
            [
                {Item.Msg = "test1 {0} and {1}"; Item.Row = 7; File = fileName}
                {Item.Msg = "test2 {0} and {1}"; Item.Row = 14; File = fileName} 
                {Item.Msg = "test3 {0} and {1}"; Item.Row = 18; File = fileName} 
            ], 
            found)
