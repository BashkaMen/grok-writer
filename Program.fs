module GrokWriter.Main

open System
open System.Threading
open H.Hooks
open Serilog
open GrokWriter.Config
open GrokWriter.Recorder
open GrokWriter.Agent

Logging.init ()

Log.Information("🎤 Grok Writer — Push-to-Talk (F#)")
Log.Information("   Hold [{HotKey}] to record, release to stop.", hotKeyDisplay)

Config.load ()

match xaiApiKey () with
| None ->
    Log.Error("XAI_API_KEY is not set. Add it to your .env file.")
    exit 1
| Some _ ->
    Log.Information("Grok API key loaded")

Log.Information("Detecting microphone devices:")
for (idx, name) in MicRecorder.ListDevices() do
    Log.Information("  [{Index}] {Name}", idx, name)

let agent = Agent.create ()

let keyboardHook = new LowLevelKeyboardHook()

keyboardHook.Down.Add(fun args ->
    if args.Keys.Are(Key.F8) then agent.Post HotkeyDown)

keyboardHook.Up.Add(fun args ->
    if args.Keys.Are(Key.F8) then agent.Post HotkeyUp)

keyboardHook.Start()
Log.Information("Listening for hotkeys… (Ctrl+C to exit)")

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
