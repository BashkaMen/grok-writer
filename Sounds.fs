module GrokWriter.Sounds

open System
open System.IO
open NAudio.Wave
open Serilog

/// Path to sounds directory (next to the executable)
let private soundsDir =
    Path.Combine(AppContext.BaseDirectory, "sounds")

/// Play a WAV file asynchronously — non-blocking, errors are swallowed.
let play (fileName: string) =
    task {
        let filePath = Path.Combine(soundsDir, fileName)
        if not (File.Exists filePath) then
            Log.Warning("Sound file not found: {FilePath}", filePath)
        else
            try
                use reader = new WaveFileReader(filePath)
                use output = new WaveOutEvent()
                output.Init(reader)
                output.Play()
                Log.Debug("Playing sound: {FileName}", fileName)
                let timeout = DateTime.Now.AddSeconds(1.0)
                while output.PlaybackState = PlaybackState.Playing
                      && DateTime.Now < timeout do
                    do! System.Threading.Tasks.Task.Delay(50)
                output.Stop()
            with ex ->
                Log.Error(ex, "Error playing sound {FileName}", fileName)
    }