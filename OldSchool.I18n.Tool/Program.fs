module OldSchool.I18n.Tool.Program

open OldSchool.I18n.StateProcessing
open OldSchool.I18n.Configuration

[<EntryPoint>]
let main argv =
    let cfg = 
        if argv.Length <= 0 then ["-h"] else (argv |> List.ofSeq)
        |> parseArgs RawConfig.Empty
           
    let fs = System.IO.Abstractions.FileSystem()

    if not <| cfg.Quit
    then
        let cfg = cfg.ToConfig fs
    
        printfn "will scan dirs:"
        cfg.SearchDirs |> List.iter(fun x -> printfn "   %s" (fs.Path.GetFullPath(x)))
    
        printfn "%s translation at: %s" 
            (if fs.File.Exists cfg.OutputPath then "will update " else "will create") 
            cfg.OutputPath

        let translationAsText = 
            cfg |> importTransl fs |> collectItems fs cfg |> serializeItemsIntoTextJson

        fs.File.WriteAllText(cfg.OutputPath, translationAsText)

    0
