module GrokWriter.Keyboard

open System.Threading.Tasks
open WindowsInput
open FsToolkit.ErrorHandling
open Serilog
open GrokWriter.Types

/// Type text into the currently focused window via InputSimulatorPlus.
/// Uses SendInput with KEYEVENTF_UNICODE — works with Cyrillic and any language.
let typeText (text: string) = taskResult {
    Log.Information("[keyboard] typing {Length} chars…", text.Length)
    try
        let sim = InputSimulator()
        sim.Keyboard.TextEntry(text) |> ignore
        Log.Information("[keyboard] done")
        return ()
    with ex ->
        return! Error (TypingError ex.Message)
}