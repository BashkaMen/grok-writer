module GrokWriter.Config

open dotenv.net
open Serilog

/// Load .env file once at startup
let load () =
    DotEnv.Load()
    Log.Information(".env loaded")

let xaiApiKey () =
    match System.Environment.GetEnvironmentVariable("XAI_API_KEY") with
    | null | "" -> None
    | key -> Some key

let hotKeyName = "F8"
let hotKeyDisplay = "F8"

/// Grok STT endpoint
let sttUrl = "https://api.x.ai/v1/stt"