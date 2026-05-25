module GrokWriter.Main

open System
open System.Threading
open H.Hooks
open Serilog
open GrokWriter.Types
open GrokWriter.Config
open GrokWriter.Recorder
open GrokWriter.Sounds
open GrokWriter.Pipeline

// ── Initialize logging ──────────────────────────────────────────────────
Logging.init ()

Log.Information("🎤 Grok Writer — Push-to-Talk (F#)")
Log.Information("   Hold [{HotKey}] to record, release to stop.", hotKeyDisplay)

// ── Load .env ──────────────────────────────────────────────────────────
Config.load ()

// ── Validate API key ────────────────────────────────────────────────────
match xaiApiKey () with
| None ->
    Log.Error("XAI_API_KEY is not set. Add it to your .env file.")
    exit 1
| Some _ ->
    Log.Information("Grok API key loaded")

// ── List mic devices ────────────────────────────────────────────────────
Log.Information("Detecting microphone devices:")
for (idx, name) in MicRecorder.ListDevices() do
    Log.Information("  [{Index}] {Name}", idx, name)

// ── Mutable state ──────────────────────────────────────────────────────
let mutable state = Idle
let mutable activeRecorder: MicRecorder option = None

// ── Global keyboard hook ──────────────────────────────────────────────
let keyboardHook = new LowLevelKeyboardHook()

keyboardHook.Down.Add(fun args ->
    match state, args.Keys with
    | Idle, keys when keys.Are(Key.F8) ->
        state <- Recording
        let micRec = new MicRecorder()
        match micRec.Start().Result with
        | Ok _path ->
            activeRecorder <- Some micRec
            Sounds.play "start.wav" |> Async.AwaitTask |> Async.StartAsTask |> ignore
            Log.Information("⏺  Recording started…")
        | Error err ->
            Log.Error("Failed to start recorder: {Error}", err)
            state <- Idle
            micRec.Dispose()
    | _ -> ()
)

keyboardHook.Up.Add(fun args ->
    match state, args.Keys with
    | Recording, keys when keys.Are(Key.F8) ->
        state <- Processing
        Sounds.play "stop.wav" |> Async.AwaitTask |> Async.StartAsTask |> ignore
        Log.Information("⏹  Recording stopped.")

        match activeRecorder with
        | Some micRec ->
            activeRecorder <- None
            task {
                let! result = Pipeline.processRecording micRec
                match result with
                | Ok _ -> ()
                | Error err -> Log.Error("{Error}", err)
                state <- Idle
                Log.Information("=== ready ===")
                micRec.Dispose()
            } |> ignore
        | None ->
            state <- Idle
    | _ -> ()
)

keyboardHook.Start()
Log.Information("Listening for hotkeys… (Ctrl+C to exit)")

// ── Keep the process alive ─────────────────────────────────────────────
let exitEvent = new ManualResetEvent(false)

Console.CancelKeyPress.Add(fun args ->
    Log.Information("Shutting down…")
    args.Cancel <- true
    keyboardHook.Dispose()
    exitEvent.Set() |> ignore
)

exitEvent.WaitOne() |> ignore
keyboardHook.Dispose()
Log.CloseAndFlush()