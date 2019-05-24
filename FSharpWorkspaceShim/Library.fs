namespace FSharpWorkspaceShim

open System
open System.IO
open FSharp.Compiler.SourceCodeServices
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text

module Shim =

    let private checker = FSharpChecker.Create()

    let private getIndex (text: string) (line: int) (column: int) =
        let mutable index = -1
        let mutable currentLine = 0
        let mutable currentColumn = 0
        text.ToCharArray()
        |> Array.iteri (fun i c ->
            if line = currentLine && column = currentColumn then index <- i
            match c with
            | '\n' ->
                currentLine <- currentLine + 1
                currentColumn <- 0
            | _ -> currentColumn <- currentColumn + 1)
        index

    let private newlineProxy = System.String [|char 29|]

    // adapted from https://github.com/dotnet/fsharp/blob/master/src/fsharp/ErrorLogger.fs
    let private normalizeErrorString (text : string) =
        if isNull text then nullArg "text"
        let text = text.Trim()

        let buf = System.Text.StringBuilder()
        let mutable i = 0
        while i < text.Length do
            let delta =
                match text.[i] with
                | '\r' when i + 1 < text.Length && text.[i + 1] = '\n' ->
                    // handle \r\n sequence - replace it with one single space
                    buf.Append newlineProxy |> ignore
                    2
                | '\n' | '\r' ->
                    buf.Append newlineProxy |> ignore
                    1
                | c ->
                    // handle remaining chars: control - replace with space, others - keep unchanged
                    let c = if Char.IsControl c then ' ' else c
                    buf.Append c |> ignore
                    1
            i <- i + delta
        buf.ToString()

    let private newlineifyErrorString (message:string) = message.Replace(newlineProxy, Environment.NewLine)

    // adapted from https://github.com/dotnet/fsharp/blob/master/vsintegration/src/FSharp.Editor/Common/RoslynHelpers.fs
    let private convertError (error: FSharpErrorInfo) (location: Location) =
        // Normalize the error message into the same format that we will receive it from the compiler.
        // This ensures that IntelliSense and Compiler errors in the 'Error List' are de-duplicated.
        // (i.e the same error does not appear twice, where the only difference is the line endings.)
        let normalizedMessage = error.Message |> normalizeErrorString |> newlineifyErrorString

        let id = "FS" + error.ErrorNumber.ToString("0000")
        let emptyString = LocalizableString.op_Implicit("")
        let description = LocalizableString.op_Implicit(normalizedMessage)
        let severity = if error.Severity = FSharpErrorSeverity.Error then DiagnosticSeverity.Error else DiagnosticSeverity.Warning
        let customTags =
            match error.ErrorNumber with
            | 1182 -> WellKnownDiagnosticTags.Unnecessary
            | _ -> null
        let descriptor = new DiagnosticDescriptor(id, emptyString, description, error.Subcategory, severity, true, emptyString, String.Empty, customTags)
        Diagnostic.Create(descriptor, location)

    let GetDiagnostics (projectPath: string) (files: string[]) (pathMapSource: string) (pathMapDest: string) =
        async {
            let projectOptions = {
                ProjectFileName = projectPath
                ProjectId = None
                SourceFiles = files
                OtherOptions = [||]
                ReferencedProjects = [||]
                IsIncompleteTypeCheckEnvironment = false
                UseScriptResolutionRules = false
                LoadTime = DateTime.Now
                UnresolvedReferences = None
                OriginalLoadReferences = []
                ExtraProjectInfo = None
                Stamp = None
            }
            let ensureDirectorySeparator (path: string) =
                if path.EndsWith(Path.DirectorySeparatorChar |> string) |> not then path + (string Path.DirectorySeparatorChar)
                else path
            let pathMapSource = ensureDirectorySeparator pathMapSource
            let pathMapDest = ensureDirectorySeparator pathMapDest
            let! results = checker.ParseAndCheckProject projectOptions
            // adapted from from https://github.com/dotnet/fsharp/blob/master/vsintegration/src/FSharp.Editor/Diagnostics/DocumentDiagnosticAnalyzer.fs
            let diagnostics =
                results.Errors
                |> Seq.choose (fun error ->
                    if error.StartLineAlternate = 0 || error.EndLineAlternate = 0 then
                        // F# error line numbers are one-based. Compiler returns 0 for global errors (reported by ProjectDiagnosticAnalyzer)
                        None
                    else
                        // Roslyn line numbers are zero-based
                        let linePositionSpan = LinePositionSpan(LinePosition(error.StartLineAlternate - 1, error.StartColumn), LinePosition(error.EndLineAlternate - 1, error.EndColumn))
                        let text = File.ReadAllText(error.FileName)
                        let textSpan =
                            TextSpan.FromBounds(
                                getIndex text (error.StartLineAlternate - 1) error.StartColumn,
                                getIndex text (error.EndLineAlternate - 1) error.EndColumn)

                        // F# compiler report errors at end of file if parsing fails. It should be corrected to match Roslyn boundaries
                        let correctedTextSpan =
                            if textSpan.End <= text.Length then
                                textSpan
                            else
                                let start =
                                    min textSpan.Start (text.Length - 1)
                                    |> max 0

                                TextSpan.FromBounds(start, text.Length)

                        let filePath =
                            if error.FileName.StartsWith(pathMapSource) then String.Concat(pathMapDest, error.FileName.Substring(pathMapSource.Length))
                            else error.FileName
                        let location = Location.Create(filePath, correctedTextSpan, linePositionSpan)
                        Some(convertError error location))
                |> Seq.toArray
            return diagnostics
        } |> Async.StartAsTask
