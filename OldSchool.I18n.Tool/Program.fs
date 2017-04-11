module OldSchool.I18n.Tool.Program

open OldSchool.I18n.Lib
open FSharp.Data
open System.IO

type ExtractableCollector(i18Class,i18Method) =
    let fileExtensions = [".cs"; ".fs"]
    let extractOne (filePath:string) = 
        if filePath.ToLower().EndsWith(".fs") 
        then (Extractor (fun _ -> ())).ExtractFs i18Class i18Method filePath
        else (Extractor (fun _ -> ())).ExtractCs i18Class i18Method filePath

    let rec extract dir =
        let subdirs = Directory.GetDirectories(dir) |> Seq.map extract |> Seq.concat

        let result = 
            Directory.GetFiles(dir) 
            |> Seq.filter(fun file -> fileExtensions |> List.exists (fun ext -> file.ToLower().EndsWith(ext)))
            |> Seq.map(fun filePath -> filePath |> File.ReadAllText |> extractOne filePath ) 
            |> Seq.concat

        [result; subdirs] |> Seq.concat

    member __.Extract(searchDirs) =
        searchDirs |> List.map extract |> Seq.concat

type FoldedItem = {
    Message : string
    Translated : string
    FoundAt : string[]
}

type TranslationRecord = JsonProvider<"""{
    "items":[{ 
        "m":"original english message to be translated", 
        "t":"translated message / localized", 
        "at":["filepath:lineNo", "filepath:lineNo"]
    }]
    } """,RootName="Coll">
    
type Config = {
    I18nClassName : string
    I18nMethodName : string
    OutputPath : string
    SearchDirs : string list
    IncludeAt : bool
}

type RawConfig = {
    I18nClassName : string option
    I18nMethodName : string option
    OutputPath : string option
    SearchDirs : string list
    IncludeAt : bool
    Quit : bool
}
with
    static member Empty = 
        {RawConfig.I18nClassName = None; I18nMethodName = None; OutputPath = None; SearchDirs = List.Empty; Quit = false; IncludeAt = true}
    member self.ToConfig () =
        match self.I18nClassName, self.I18nMethodName, self.OutputPath, self.SearchDirs with
        |None, _, _, _ ->  failwith "i18n classname not specified via -c parameter"
        |_, None, _, _ ->  failwith "i18n methodname not specified via -m parameter"
        |_, _, None, _ ->  failwith "no output path specified via -o parameter"
        |_, _, _, [] ->  failwith "no dirs to scan specified via -d parameter"
        |_, _, _, x when x |> List.exists (fun x -> Directory.Exists(x) |> not) -> 
            failwith "some dirs to scan don't exist (specified via -d parameter)"
        |Some clazz, Some mthd, Some outp, dirs -> 
            {
                Config.I18nClassName = clazz
                I18nMethodName = mthd
                OutputPath = outp
                SearchDirs = dirs |> List.map(fun x -> Path.GetFullPath(x))
                IncludeAt = self.IncludeAt
            }

let rec parseArgs (state:RawConfig) args =
    match args with
    |"-q"::rest -> 
        parseArgs {state with IncludeAt = false} rest
    |"-c"::rest -> 
        match rest with
        |clazz::rest -> parseArgs {state with I18nClassName = Some clazz} rest
        |_ -> failwith "expected to have classname after -c"
    |"-m"::rest -> 
        match rest with
        |mthd::rest -> parseArgs {state with I18nMethodName = Some mthd} rest
        |_ -> failwith "expected to have methodname after -m"
    |"-d"::rest -> 
        match rest with
        |dir::rest -> parseArgs {state with SearchDirs = dir::state.SearchDirs} rest
        |_ -> failwith "expected to have directory after -d"
    |"-o"::rest -> 
        match rest with
        |out::rest -> parseArgs {state with OutputPath = Some out} rest 
        |_ -> failwith "expected to have outputpath after -o"
    |"-h"::rest|"/?"::rest|"--help"::rest -> 
        printf """
            -h 
            /?
            --help
                Shows this help message

            -d zzzz
                Add directory zzz to scan list

            -o zzzz
                Load old translation from file zzzz and write updated translation into file zzzz

            -m zzzz 
                Use zzzz as expected I18n method name

            -c zzzz
                Use zzzz as expected I18n class name

            -q 
                Don't include content of "at" in JSON - don't add references where message is present in sources
        """
        {state with Quit = true}
    | [] -> state
    |_ -> failwithf "unexpected parameters: %A" args

[<EntryPoint>]
let main argv =
    let cfg = 
        if argv.Length <= 0 then ["-h"] else (argv |> List.ofSeq)
        |> parseArgs RawConfig.Empty

    do if cfg.Quit then System.Environment.Exit(0)

    let cfg = cfg.ToConfig ()
    
    let outExists = File.Exists cfg.OutputPath

    printfn "will scan dirs:"
    cfg.SearchDirs |> List.iter(fun x -> printfn "   %s" (Path.GetFullPath(x)))
    
    printfn "%s translation at: %s" (if outExists then "will update " else "will create") cfg.OutputPath

    let i18Class = "I18n"
    let i18Method = "Translate"

    let oldTransl = 
        if outExists 
        then
            TranslationRecord.Parse(File.ReadAllText cfg.OutputPath).Items
            |> Seq.ofArray
            |> Seq.map (fun x -> x.M, x.T)
            |> Map.ofSeq
        else Map.empty
      
    let withoutBaseDir (inp:string) =
        //assumes case sensitive paths
        let found = 
            cfg.SearchDirs 
            |> List.tryPick(fun searchDir -> if inp.StartsWith searchDir then inp.Substring(searchDir.Length) |> Some else None)
        match found with
        |Some x -> x.TrimStart(System.IO.Path.DirectorySeparatorChar)
        |_ -> inp

    let res = 
        ExtractableCollector(i18Class,i18Method).Extract cfg.SearchDirs 
        |> Seq.groupBy (fun x -> x.Msg)
        |> Seq.sortBy (fun (x,_) -> x)
        |> Seq.map (fun (msg,all) -> 
            {
                FoldedItem.Message = msg
                Translated = defaultArg (oldTransl.TryFind(msg)) "" 
                FoundAt = 
                    if cfg.IncludeAt 
                    then all |> Seq.map (fun x -> sprintf "%s:%i" (withoutBaseDir x.File) x.Row) |> Array.ofSeq
                    else Array.empty
            } )
                
    let content = 
        let items = res |> Seq.map(fun x -> TranslationRecord.Item(x.Message, x.Translated, x.FoundAt)) |> Array.ofSeq
        TranslationRecord.Coll(items).JsonValue.ToString()

    File.WriteAllText(cfg.OutputPath, content)

    0
