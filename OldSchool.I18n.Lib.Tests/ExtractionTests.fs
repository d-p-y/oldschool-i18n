namespace OldSchool.I18n.Lib.Tests

open Xunit
open OldSchool.I18n.Parsing
open OldSchool.I18n.Configuration
open OldSchool.I18n.StateProcessing
open Xunit.Abstractions
open System.IO.Abstractions.TestingHelpers
open System.Collections.Generic

type ExtractionTests(log:ITestOutputHelper) = 
    let i18Class = "I18n"
    let i18Method = "Translate"
       
    let logFunc = (fun x -> (x ()) |> log.WriteLine)  
    let assertEqualString(x, y) = Assert.Equal(x,y, ignoreLineEndingDifferences=true)

    [<Fact>]
    let ``finds single classfile with one message without at`` () =
        let fs =            
            [
                """c:\projects\testing\classes\someclass.cs""", """
                class Foo {
                    void Bar() {
                        return I18n.Translate("Something {0} zzz {1}");
                    }
                }"""
            ]
            |> Seq.map (fun (k,v) -> k, MockFileData v)
            |> Map.ofSeq
            |> MockFileSystem

        let foundmessagesJson =
            let cfg = 
                {
                    Config.I18nClassName = "I18n"
                    I18nMethodName = ["Translate"]
                    OutputPath = """c:\projects\testing\translation.json"""
                    SearchDirs = ["""c:\projects\testing"""]
                    IncludeAt = false
                    AdditionalTranslationFiles = List.empty
                } 
            collectItems fs cfg Map.empty |> serializeItemsIntoTextJson cfg

        assertEqualString("""{
  "items": [
    {
      "m": "Something {0} zzz {1}",
      "t": ""
    }
  ]
}""", foundmessagesJson)

    [<Fact>]
    let ``finds single classfile with one message with at`` () =
        let fs =            
            [
                """c:\projects\testing\classes\someclass.cs""", MockFileData("""
                class Foo {
                    void Bar() {
                        return I18n.Translate("Something {0} zzz {1}");
                    }
                }""")
            ]
            |> Seq.map (fun (k,v) -> k, MockFileData v)
            |> Map.ofSeq
            |> MockFileSystem

        let foundmessagesJson =
            let cfg = 
                {
                    Config.I18nClassName = "I18n"
                    I18nMethodName = ["Translate"]
                    OutputPath = """c:\projects\testing\translation.json"""
                    SearchDirs = ["""c:\projects\testing"""]
                    IncludeAt = true
                    AdditionalTranslationFiles = List.empty
                } 
            collectItems fs cfg Map.empty |> serializeItemsIntoTextJson cfg

        assertEqualString("""{
  "items": [
    {
      "m": "Something {0} zzz {1}",
      "t": "",
      "at": [
        "classes\\someclass.cs:3"
      ]
    }
  ]
}""", foundmessagesJson)

    [<Fact>]
    let ``updates translations`` () =
        let fs =            
            [
                """c:\projects\testing\classes\someclass.cs""", """
                class Foo {
                    void Bar1() {
                        return I18n.Translate("Something old");
                    }

                    void Bar2() {
                        return I18n.Translate("Something new");
                    }
                }"""

                """c:\projects\testing\translation.json""", """{
                    "items": [
                        {
                            "m": "Something old",
                            "t": "some reused translation"
                        }
                        ]
                    }"""
            ]
            |> Seq.map (fun (k,v) -> k, MockFileData v)
            |> Map.ofSeq
            |> MockFileSystem

        let cfg = 
            {
                Config.I18nClassName = "I18n"
                I18nMethodName = ["Translate"]
                OutputPath = """c:\projects\testing\translation.json"""
                SearchDirs = ["""c:\projects\testing"""]
                IncludeAt = false
                AdditionalTranslationFiles = List.empty
            } 
            
        mainProcess (fun x -> log.WriteLine x) cfg fs
            
        assertEqualString("""{
  "items": [
    {
      "m": "Something new",
      "t": ""
    },
    {
      "m": "Something old",
      "t": "some reused translation"
    }
  ]
}""", fs.File.ReadAllText("""c:\projects\testing\translation.json"""))
