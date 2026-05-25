module GrokWriter.Recorder

open System
open System.IO
open NAudio.Wave
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Serilog
open GrokWriter.Types

/// Microphone recorder using NAudio WaveInEvent.
type MicRecorder() =
    let mutable waveSource: IWaveIn = null
    let mutable waveWriter: WaveFileWriter = null
    let mutable filePath: string = ""

    /// List available microphone devices
    static member ListDevices() =
        [ for i in -1 .. WaveInEvent.DeviceCount - 1 do
            let caps = WaveInEvent.GetCapabilities(i)
            yield (i, caps.ProductName) ]

    /// Start recording to a temp WAV file.
    member this.Start() = taskResult {
        let deviceNumber = if WaveInEvent.DeviceCount > 0 then 0 else -1

        let ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        filePath <- Path.Combine(Path.GetTempPath(), sprintf "grok-writer-%d.wav" ts)
        Log.Information("[recorder] output: {FilePath}", filePath)

        let source = new WaveInEvent(DeviceNumber = deviceNumber,
                                     WaveFormat = new WaveFormat(16000, 16, 1))
        waveSource <- source

        waveWriter <- new WaveFileWriter(filePath, source.WaveFormat)

        source.DataAvailable.Add(fun args ->
            if not (obj.ReferenceEquals(waveWriter, null)) then
                waveWriter.Write(args.Buffer, 0, args.BytesRecorded)
                waveWriter.Flush()
        )

        source.StartRecording()
        Log.Information("[recorder] NAudio WaveInEvent started (16kHz mono)")
        return filePath
    }

    /// Stop recording. Returns the WAV file path.
    member this.Stop() = taskResult {
        Log.Information("[recorder] stopping…")

        if not (obj.ReferenceEquals(waveSource, null)) then
            waveSource.StopRecording()
            waveSource.Dispose()
            waveSource <- null

        if not (obj.ReferenceEquals(waveWriter, null)) then
            waveWriter.Dispose()
            waveWriter <- null

        if String.IsNullOrEmpty filePath || not (File.Exists filePath) then
            return! Error (RecordingFailed "WAV file not found after stop")
        else
            let size = int (FileInfo(filePath)).Length
            Log.Information("[recorder] WAV saved: {Size} bytes", size)
            return filePath
    }

    member this.Dispose() =
        if not (obj.ReferenceEquals(waveSource, null)) then
            waveSource.Dispose()
            waveSource <- null
        if not (obj.ReferenceEquals(waveWriter, null)) then
            waveWriter.Dispose()
            waveWriter <- null