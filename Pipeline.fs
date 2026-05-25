module GrokWriter.Pipeline

open System
open System.IO
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Serilog
open GrokWriter.Types
open GrokWriter.Stt
open GrokWriter.Recorder
open GrokWriter.Keyboard

/// Full pipeline: stop recording → STT → type text → cleanup
let processRecording (micRec: MicRecorder) =
    (taskResult {
        Log.Information("─── pipeline start ───")

        Log.Information("[1/3] stopping recorder…")
        let! filePath = micRec.Stop()
        Log.Information("      WAV: {FilePath}", filePath)

        Log.Information("[2/3] transcribing…")
        let! text = transcribe filePath "ru"

        Log.Information("[3/3] typing transcript…")
        do! typeText text

        try File.Delete filePath with _ -> ()

        Log.Information("─── pipeline done ───")
        return ()
    })
    |> TaskResult.mapError (fun err ->
        Log.Error("✗ pipeline error: {Error}", err)
        err
    )
