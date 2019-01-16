﻿module OldSchool.I18n.Configuration

type Config = {
    I18nClassName : string
    I18nMethodName : string list
    OutputPath : string
    SearchDirs : string list
    IncludeAt : bool
    AdditionalTranslationFiles : string list
}

type RawConfig = {
    I18nClassName : string option
    I18nMethodName : string list
    OutputPath : string option
    SearchDirs : string list
    IncludeAt : bool
    Quit : bool
    IncludeTranslationFile : string list
}
with
    static member Empty = 
        {RawConfig.I18nClassName = None; I18nMethodName = []; OutputPath = None; SearchDirs = List.Empty; Quit = false; IncludeAt = false; IncludeTranslationFile = []}
    member self.ToConfig (fs:System.IO.Abstractions.IFileSystem) =
        match self.I18nClassName, self.I18nMethodName, self.OutputPath, self.SearchDirs, self.IncludeTranslationFile with
        |None, _, _, _, _ ->  failwith "i18n classname not specified via -c parameter"
        |_, [], _, _, _ ->  failwith "i18n methodname not specified via -m parameter"
        |_, _, None, _, _ ->  failwith "no output path specified via -o parameter"
        |_, _, _, [], _ ->  failwith "no dirs to scan specified via -d parameter"
        |_, _, _, x, _ when x |> List.exists (fun x -> fs.Directory.Exists(x) |> not) -> 
            failwith "some dirs to scan don't exist (specified via -d parameter)"
        |_, _, _, _, x when x |> List.exists (fun x -> fs.File.Exists(x) |> not) -> 
            failwith "some 'translation file to include' don't exist (specified via -i parameter)"
        |Some clazz, mthds, Some outp, dirs, includeFiles -> 
            {
                Config.I18nClassName = clazz
                I18nMethodName = mthds
                OutputPath = outp
                SearchDirs = dirs |> List.map(fun x -> fs.Path.GetFullPath(x))
                IncludeAt = self.IncludeAt
                AdditionalTranslationFiles = includeFiles |> List.map(fun x -> fs.Path.GetFullPath(x))
            }

let rec parseArgs log (state:RawConfig) args =
    let parseArgs = parseArgs log
    match args with
    |"-q"::rest -> 
        parseArgs {state with IncludeAt = true} rest
    |"-c"::rest -> 
        match rest with
        |clazz::rest -> parseArgs {state with I18nClassName = Some clazz} rest
        |_ -> failwith "expected to have classname after -c"
    |"-m"::rest -> 
        match rest with
        |mthd::rest -> parseArgs {state with I18nMethodName = mthd::state.I18nMethodName} rest
        |_ -> failwith "expected to have methodname after -m"
    |"-d"::rest -> 
        match rest with
        |dir::rest -> parseArgs {state with SearchDirs = dir::state.SearchDirs} rest
        |_ -> failwith "expected to have directory after -d"
    |"-i"::rest ->
        match rest with
        |filePath::rest -> parseArgs {state with IncludeTranslationFile = filePath::state.IncludeTranslationFile} rest
        |_ -> failwith "expected to have filepath after -i"
    |"-o"::rest -> 
        match rest with
        |out::rest -> parseArgs {state with OutputPath = Some out} rest 
        |_ -> failwith "expected to have outputpath after -o"
    |"-h"::_|"/?"::_|"--help"::_ -> 
        """
            -h 
            /?
            --help
                Shows this help message

            -d zzzz
                Add directory zzz to scan list

            -i zzzz
                Import additional translation from file - read only. Parameter may be used several times

            -o zzzz
                Load old translation from file zzzz and write updated translation into file zzzz

            -m zzzz 
                Use zzzz as expected I18n method name

            -c zzzz
                Use zzzz as expected I18n class name

            -q 
                Include content of "at" in JSON - add references where message is present in sources
        """ |> log
        {state with Quit = true}
    | [] -> state
    |_ -> failwithf "unexpected parameters: %A" args
