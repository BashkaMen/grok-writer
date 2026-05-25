module GrokWriter.Agent

open FsToolkit.ErrorHandling
open Serilog
open GrokWriter.Types
open GrokWriter.Recorder
open GrokWriter.Sounds
open GrokWriter.Pipeline

type Msg =
    | HotkeyDown
    | HotkeyUp
    | PipelineFinished

let create () =
    MailboxProcessor<Msg>.Start(fun inbox ->
        let rec idle () = async {
            let! msg = inbox.Receive()
            match msg with
            | HotkeyDown ->
                let micRec = new MicRecorder()
                let! result = micRec.Start() |> Async.AwaitTask
                match result with
                | Ok _ ->
                    Sounds.play "start.wav" |> ignore
                    Log.Information("⏺  Recording started…")
                    return! recording micRec
                | Error err ->
                    Log.Error("Failed to start recorder: {Error}", err)
                    micRec.Dispose()
                    return! idle ()
            | _ ->
                return! idle ()
        }

        and recording (micRec: MicRecorder) = async {
            let! msg = inbox.Receive()
            match msg with
            | HotkeyUp ->
                Sounds.play "stop.wav" |> ignore
                Log.Information("⏹  Recording stopped.")
                task {
                    let! result =
                        Pipeline.processRecording micRec
                        |> TaskResult.catch Unexpected
                    match result with
                    | Ok _ -> ()
                    | Error err -> Log.Error("{Error}", err)
                    micRec.Dispose()
                    inbox.Post PipelineFinished
                } |> ignore
                return! processing ()
            | _ ->
                return! recording micRec
        }

        and processing () = async {
            let! msg = inbox.Receive()
            match msg with
            | PipelineFinished ->
                Log.Information("=== ready ===")
                return! idle ()
            | _ ->
                return! processing ()
        }

        idle ()
    )
