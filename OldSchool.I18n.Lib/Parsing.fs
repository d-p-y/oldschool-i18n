module OldSchool.I18n.Parsing

type Item = {
    Msg : string
    File : string

    ///0 based index
    Row : int 
   }

type CsMatchState = 
|Undefined
|MatchedClass
|MatchedDotAfterClass
|MatchedMethod
|MatchedOpenBracketAfterMethod
|MatchedVerbatimMessageStart
|MatchedNonVerbatimMessageStart //more readable than "regular string"

type FsMatchState = 
|Undefined
|MatchedClass
|MatchedDotAfterClass
|MatchedMethod
|MatchedOpenBracketAfterMethod //optional state
|MatchedVerbatimMessageStart
|MatchedNonVerbatimMessageStart //more readable than "regular string"

type ParserState<'T> = {
    LineNo : int
    Parsing : 'T
    WhitespaceRelevant : bool
    InSingleLineComment : bool
    InMultiLineComment : bool}

let (|ThenChar|_|) needed input =
    match input with
    |ch::rest when ch = needed -> Some(rest)
    |_ -> None

let rec beginsWithSoFar (fullMatch:_ list) (partialInput: _ list) =
    if fullMatch.Length < partialInput.Length then false //not expected to be called this way
    else
        if fullMatch.Length = partialInput.Length then partialInput = fullMatch
        else 
            match (fullMatch, partialInput) with
            | _, [] -> true
            | [], _ -> false
            |ch1::expected, ch2::fact when ch1 = ch2 -> beginsWithSoFar expected fact
            |_, _ -> false

let rec thenPhrase notFollowedBy (needed:_ list) input fact =
    match input with
    | [] -> None
    | ch::inputRemain ->
        let fact = ch::fact
        
        if fact.Length = needed.Length then
            if needed = (List.rev fact) 
            then
                match notFollowedBy, inputRemain with
                |Some notFollowedBy, ch::_ when ch = notFollowedBy -> None
                |_ -> Some(fact, inputRemain)
            else None
        else 
            if fact.Length > needed.Length then None
            else
                if beginsWithSoFar needed (List.rev fact) then thenPhrase notFollowedBy needed inputRemain fact
                else None

let (|ThenPhrase|_|) needed input = thenPhrase None needed input List.empty
let (|ThenPhraseNotFollowedBy|_|) needed notFollowedBy input = thenPhrase (Some notFollowedBy) needed input List.empty
    
let rec collectTillFwd terminator notFollowedBy input former collected =
    match input,former with
    |[], _ -> Some(collected, [])
    |ch::rest, None when ch = terminator -> 
        collectTillFwd terminator notFollowedBy rest (Some ch) collected
    |ch::rest, Some _ when ch <> notFollowedBy -> Some(collected, rest)
    |ch::rest, Some former -> collectTillFwd terminator notFollowedBy rest None (ch::former::collected)
    |ch::rest, _ -> collectTillFwd terminator notFollowedBy rest None (ch::collected)
    |_ -> None

let (|TillCharNotFollowedBy|_|) terminator notFollowedBy input = collectTillFwd terminator notFollowedBy input None List.empty

let rec startsWith (content:char list) (neededPhrase:char list) = 
    match content, neededPhrase with
    |_, [] -> true
    |r::_, p::_ when r <> p -> false
    |r::rrem, p::prem -> startsWith rrem prem
    |[], _ -> true

let rec collectTillPhrase terminator followedBy input collected =
    match input with
    |[] -> Some(collected, [])
    |ch::rest when ch = terminator -> 
        if startsWith rest followedBy 
        then Some(collected, rest)
        else collectTillPhrase terminator followedBy rest (ch::collected)
    |ch::rest -> collectTillPhrase terminator followedBy rest (ch::collected)

let (|TillCharNotFollowedByPhrase|_|) terminator notFollowedBy input = 
    collectTillPhrase terminator notFollowedBy input List.empty

let rec collectTillBck terminator notPreceededBy input former collected =
    match input,former with
    |ch::rest, None -> 
        collectTillBck terminator notPreceededBy rest (Some ch) collected
    |ch::rest, Some former when ch = terminator && former <> notPreceededBy -> 
        Some(former::collected, rest)
    |ch::rest, Some former -> 
        collectTillBck terminator notPreceededBy rest (Some ch) (former::collected)    
    |[], Some former -> Some(former::collected, [])
    |[], None -> Some(collected, [])
        
let (|TillCharNotPreceededBy|_|) terminator notPreceededBy input = collectTillBck terminator notPreceededBy input None List.empty

let isWhitespace ch = System.Char.IsWhiteSpace(ch)

let fsGetNonEscaped (matched:char list) = 
    let sb = 
        matched 
        |> List.rev 
        |> List.fold 
            (fun acc i -> (acc:System.Text.StringBuilder).Append(i)) 
            (new System.Text.StringBuilder()) 
    sb.ToString()
    
let escapeVerbatim (matched:char list) = 
    let sb = 
        matched 
        |> List.rev 
        |> List.fold 
            (fun acc i -> (acc:System.Text.StringBuilder).Append(i)) 
            (new System.Text.StringBuilder()) 
    sb.ToString().Replace("\"\"","\"")
    
//from https://msdn.microsoft.com/en-us/library/h21280bw.aspx    
let rec doEscape former result (input:char list) =
    match input, former with
    |ch::rest, None -> doEscape (Some ch) result rest
    |ch::rest, Some former when former = '\\' -> 
        match ch with 
        |'\''|'\"'|'\\' -> doEscape None (ch::result) rest
        |'n' -> doEscape None ('\n'::result) rest 
        |'r' -> doEscape None ('\r'::result) rest
        |'t' -> doEscape None ('\t'::result) rest
        |'v' -> doEscape None ('\v'::result) rest
        |'f' -> doEscape None ('\f'::result) rest
        |'b' -> doEscape None ('\b'::result) rest
        |'a' -> doEscape None ('\a'::result) rest
        | _ ->  doEscape None result rest //skipping unsupported or invalid sequence
    |ch::rest, Some former -> doEscape (Some ch) (former::result) rest
    |[], Some former -> (former::result)
    |[], None -> result

let strToMatchChrList i = i |> List.ofSeq

let csEscapeNonVerbatim (matched:char list) = 
    let sb = 
        matched 
        |> List.rev 
        |> doEscape None List.empty
        |> List.rev 
        |> List.fold 
            (fun acc i -> (acc:System.Text.StringBuilder).Append(i)) 
            (new System.Text.StringBuilder()) 
    sb.ToString()

let fsEscapeNonVerbatim = csEscapeNonVerbatim

///FS has optional brackets before message, CS has mandatory brackets before message
///valid FS&CS: i18n.Translate("azzz")
///valid FS only: i18n.Translate "azzz"
type Extractor(log) =
    
    let fsVerbatimStart = "\"\"\"" |> strToMatchChrList
    let fsSlLineComment = "//" |> strToMatchChrList
    let fsMlLineCommentStart = "(*" |> strToMatchChrList
    let fsMlLineCommentEnd = "*)" |> strToMatchChrList
    
    let rec extractFs clazz mthd fnd content (state:ParserState<FsMatchState>) =
        log (fun () -> sprintf "state=%A content=%s found=%A" state (content |> Array.ofList |> System.String) fnd)
        
        match state, content with
        | _, [] -> fnd //end of file
        | {WhitespaceRelevant = false}, ch::content when isWhitespace ch  ->
            extractFs clazz mthd fnd content (if ch <> '\n' then state else {state with LineNo = state.LineNo + 1})
        | {InSingleLineComment = false}, ThenPhrase fsSlLineComment (_, content) -> 
            extractFs clazz mthd fnd content {state with InSingleLineComment = true; WhitespaceRelevant = true}
        | {InSingleLineComment = true}, ThenChar '\n' content -> 
            extractFs clazz mthd fnd content {state with InSingleLineComment = false; LineNo = state.LineNo + 1; WhitespaceRelevant = false}
        | {InSingleLineComment = true}, _ -> 
            extractFs clazz mthd fnd content.Tail state
        | {InMultiLineComment = false}, ThenPhraseNotFollowedBy fsMlLineCommentStart ')' (_,content) -> 
            extractFs clazz mthd fnd content {state with InMultiLineComment = true}
        | {InMultiLineComment = true}, ThenPhrase fsMlLineCommentEnd (_,content) -> 
           extractFs clazz mthd fnd content {state with InMultiLineComment = false}
        | {InMultiLineComment = true}, _ -> 
           extractFs clazz mthd fnd content.Tail state
        | {Parsing = FsMatchState.Undefined}, ThenPhrase clazz (_,content) ->
            extractFs clazz mthd fnd content {state with Parsing = FsMatchState.MatchedClass}
        | {Parsing = FsMatchState.Undefined}, _::rest -> 
            extractFs clazz mthd fnd rest state
        | {Parsing = FsMatchState.MatchedClass}, ThenChar '.' content ->
            extractFs clazz mthd fnd content {state with Parsing = FsMatchState.MatchedDotAfterClass; LineNo = state.LineNo}
        | {Parsing = FsMatchState.MatchedDotAfterClass}, ThenPhrase mthd (_,content) -> 
            extractFs clazz mthd fnd content {state with Parsing = FsMatchState.MatchedMethod; LineNo = state.LineNo}
        | {Parsing = FsMatchState.MatchedMethod}, ThenChar '(' content ->
            extractFs clazz mthd fnd content {state with Parsing = FsMatchState.MatchedOpenBracketAfterMethod; LineNo = state.LineNo}
        | {Parsing = FsMatchState.MatchedOpenBracketAfterMethod}, ThenPhrase fsVerbatimStart (_,content) ->
            extractFs clazz mthd fnd content {state with Parsing = FsMatchState.MatchedVerbatimMessageStart; WhitespaceRelevant = true; LineNo = state.LineNo}
        | {Parsing = FsMatchState.MatchedOpenBracketAfterMethod}, ThenChar '"' content ->
            extractFs clazz mthd fnd content {state with Parsing = FsMatchState.MatchedNonVerbatimMessageStart; WhitespaceRelevant = true; LineNo = state.LineNo}
        | {Parsing = FsMatchState.MatchedMethod}, ThenPhrase fsVerbatimStart (_,content) ->
            extractFs clazz mthd fnd content {state with Parsing = FsMatchState.MatchedVerbatimMessageStart; WhitespaceRelevant = true; LineNo = state.LineNo}
        | {Parsing = FsMatchState.MatchedMethod}, ThenChar '"' content ->
            extractFs clazz mthd fnd content {state with Parsing = FsMatchState.MatchedNonVerbatimMessageStart; WhitespaceRelevant = true; LineNo = state.LineNo}
        | {Parsing = FsMatchState.MatchedNonVerbatimMessageStart}, TillCharNotPreceededBy '"' '\\' (matched, content) ->
            let newItm = fsEscapeNonVerbatim matched, state.LineNo
            extractFs clazz mthd (newItm::fnd) content {state with Parsing = FsMatchState.Undefined; WhitespaceRelevant = false}
        | {Parsing = FsMatchState.MatchedVerbatimMessageStart}, TillCharNotFollowedByPhrase '"' ['"'; '"'] (matched, content) ->
            let newItm = fsGetNonEscaped matched, state.LineNo
            extractFs clazz mthd (newItm::fnd) content {state with Parsing = FsMatchState.Undefined; WhitespaceRelevant = false}
        | _, _ ->
            extractFs clazz mthd fnd content.Tail {state with Parsing = FsMatchState.Undefined}

    let csVerbatimStart = "@\"" |> strToMatchChrList
    let csSlLineComment = "//" |> strToMatchChrList
    let csMlLineCommentStart = "/*" |> strToMatchChrList
    let csMlLineCommentEnd = "*/" |> strToMatchChrList
    
    let rec extractCs clazz mthd fnd content (state:ParserState<CsMatchState>) =
        log (fun () -> sprintf "state=%A content=%s found=%A" state (content |> Array.ofList |> System.String) fnd)
        
        match state, content with
        | _, [] -> fnd //end of file
        | {WhitespaceRelevant = false}, ch::content when isWhitespace ch  ->
            extractCs clazz mthd fnd content (if ch <> '\n' then state else {state with LineNo = state.LineNo + 1})
        | {InSingleLineComment = false}, ThenPhrase csSlLineComment (_, content) -> 
            extractCs clazz mthd fnd content {state with InSingleLineComment = true; WhitespaceRelevant = true}
        | {InSingleLineComment = true}, ThenChar '\n' content -> 
            extractCs clazz mthd fnd content {state with InSingleLineComment = false; LineNo = state.LineNo + 1; WhitespaceRelevant = false}
        | {InSingleLineComment = true}, _ -> 
            extractCs clazz mthd fnd content.Tail state
        | {InMultiLineComment = false}, ThenPhrase csMlLineCommentStart (_,content) -> 
            extractCs clazz mthd fnd content {state with InMultiLineComment = true}
        | {InMultiLineComment = true}, ThenPhrase csMlLineCommentEnd (_,content) -> 
           extractCs clazz mthd fnd content {state with InMultiLineComment = false}
        | {InMultiLineComment = true}, _ -> 
           extractCs clazz mthd fnd content.Tail state
        | {Parsing = CsMatchState.Undefined}, ThenPhrase clazz (_,content) ->
            extractCs clazz mthd fnd content {state with Parsing = CsMatchState.MatchedClass}
        | {Parsing = CsMatchState.Undefined}, _::rest -> 
            extractCs clazz mthd fnd rest state
        | {Parsing = CsMatchState.MatchedClass}, ThenChar '.' content ->
            extractCs clazz mthd fnd content {state with Parsing = CsMatchState.MatchedDotAfterClass; LineNo = state.LineNo}
        | {Parsing = CsMatchState.MatchedDotAfterClass}, ThenPhrase mthd (_,content) -> 
            extractCs clazz mthd fnd content {state with Parsing = CsMatchState.MatchedMethod; LineNo = state.LineNo}
        | {Parsing = CsMatchState.MatchedMethod}, ThenChar '(' content ->
            extractCs clazz mthd fnd content {state with Parsing = CsMatchState.MatchedOpenBracketAfterMethod; LineNo = state.LineNo}
        | {Parsing = CsMatchState.MatchedOpenBracketAfterMethod}, ThenChar '"' content ->
            extractCs clazz mthd fnd content {state with Parsing = CsMatchState.MatchedNonVerbatimMessageStart; WhitespaceRelevant = true; LineNo = state.LineNo}
        | {Parsing = CsMatchState.MatchedOpenBracketAfterMethod}, ThenPhrase csVerbatimStart (_,content) ->
            extractCs clazz mthd fnd content {state with Parsing = CsMatchState.MatchedVerbatimMessageStart; WhitespaceRelevant = true; LineNo = state.LineNo}
        | {Parsing = CsMatchState.MatchedNonVerbatimMessageStart}, TillCharNotPreceededBy '"' '\\' (matched, content) ->
            let newItm = csEscapeNonVerbatim matched, state.LineNo
            extractCs clazz mthd (newItm::fnd) content {state with Parsing = CsMatchState.Undefined; WhitespaceRelevant = false}
        | {Parsing = CsMatchState.MatchedVerbatimMessageStart}, TillCharNotFollowedBy '"' '"' (matched, content) ->
            let newItm = escapeVerbatim matched, state.LineNo
            extractCs clazz mthd (newItm::fnd) content {state with Parsing = CsMatchState.Undefined; WhitespaceRelevant = false}
        | _, _ ->
            extractCs clazz mthd fnd content.Tail {state with Parsing = CsMatchState.Undefined}

    let extractCsStrings (i18Class:string) (i18Method:string) filePath (content:string) = 
        {ParserState.Parsing = CsMatchState.Undefined; WhitespaceRelevant = false; InSingleLineComment = false; InMultiLineComment = false; LineNo = 0}
        |> extractCs (i18Class |> strToMatchChrList) (i18Method |> strToMatchChrList) List.Empty (content.ToCharArray() |> List.ofArray) 
        |> List.map (fun (msg, line) -> {Item.File = filePath; Msg = msg; Row = line})

    let extractFsStrings (i18Class:string) (i18Method:string) filePath (content:string) = 
        {ParserState.Parsing = FsMatchState.Undefined; WhitespaceRelevant = false; InSingleLineComment = false; InMultiLineComment = false; LineNo = 0}
        |> extractFs (i18Class |> strToMatchChrList) (i18Method |> strToMatchChrList) List.Empty (content.ToCharArray() |> List.ofArray) 
        |> List.map (fun (msg, line) -> {Item.File = filePath; Msg = msg; Row = line})
        
    member __.ExtractCs = extractCsStrings
    member __.ExtractFs = extractFsStrings
