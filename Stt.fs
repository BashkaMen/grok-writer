module GrokWriter.Stt

open System
open System.IO
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Serilog
open GrokWriter.Types
open GrokWriter.Config

/// Transcribe an audio file via xAI /v1/stt endpoint.
/// Reads XAI_API_KEY from environment variables automatically.
let transcribe (filePath: string) (language: string) = taskResult {
    let! apiKey = xaiApiKey () |> Result.requireSome ApiKeyMissing

    let fileInfo = FileInfo(filePath)
    Log.Information("[stt] reading {FilePath} ({FileSize} bytes)", filePath, fileInfo.Length)

    let fileName = Path.GetFileName(filePath)
    let fileData = File.ReadAllBytes(filePath)

    use content = new MultipartFormDataContent()
    content.Add(new StringContent(language), "language")
    content.Add(new StringContent("true"), "format")
    content.Add(new ByteArrayContent(fileData, 0, fileData.Length), "file", fileName)

    use client = new HttpClient()
    client.DefaultRequestHeaders.Authorization <-
        Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey)

    Log.Information("[stt] sending to {Url}", sttUrl)
    let! response = client.PostAsync(sttUrl, content)

    let! json =
        if response.IsSuccessStatusCode then
            task { let! body = response.Content.ReadAsStringAsync() in return Ok body }
        else
            task {
                let! body = response.Content.ReadAsStringAsync()
                let msg = if body.Length > 300 then body.[..300] else body
                return Error (SttError (int response.StatusCode, msg))
            }

    Log.Debug("[stt] raw response: {Response}", json.[..min 200 (json.Length - 1)])

    let doc = JsonDocument.Parse(json)
    let root = doc.RootElement

    let text =
        match root.TryGetProperty("text") with
        | true, p -> p.GetString() |> Option.ofObj |> Option.defaultValue ""
        | false, _ -> ""

    let duration =
        match root.TryGetProperty("duration") with
        | true, p -> p.GetDouble()
        | false, _ -> 0.0

    if String.IsNullOrWhiteSpace text then
        return! Error SttEmptyResult
    else
        Log.Information("[stt] transcript ({Duration:F1}s): {Text}", duration, text)
        return text
}
