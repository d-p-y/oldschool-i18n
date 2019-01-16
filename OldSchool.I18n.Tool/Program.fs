module OldSchool.I18n.Tool.Program

open OldSchool.I18n.StateProcessing
open OldSchool.I18n.Configuration

[<EntryPoint>]
let main argv =
    let log x = System.Console.WriteLine(x:string)
    let cfg = 
        if argv.Length <= 0 then ["-h"] else (argv |> List.ofSeq)
        |> parseArgs log RawConfig.Empty
           
    let fs = System.IO.Abstractions.FileSystem()
    
    do 
        if not <| cfg.Quit 
        then mainProcess log (cfg.ToConfig fs) fs
        
    0
