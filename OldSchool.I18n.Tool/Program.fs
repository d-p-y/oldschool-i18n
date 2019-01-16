module OldSchool.I18n.Tool.Program

open OldSchool.I18n.StateProcessing
open OldSchool.I18n.Configuration
open System.IO

[<EntryPoint>]
let main argv =
    let cfg = 
        if argv.Length <= 0 then ["-h"] else (argv |> List.ofSeq)
        |> parseArgs RawConfig.Empty
            
    if not <| cfg.Quit
    then
        let cfg = cfg.ToConfig ()
    
        printfn "will scan dirs:"
        cfg.SearchDirs |> List.iter(fun x -> printfn "   %s" (Path.GetFullPath(x)))
    
        printfn "%s translation at: %s" 
            (if File.Exists cfg.OutputPath then "will update " else "will create") 
            cfg.OutputPath

        let translationAsText = 
            cfg |> importTransl |> collectItems cfg |> serializeItemsIntoTextJson

        File.WriteAllText(cfg.OutputPath, translationAsText)

    0
