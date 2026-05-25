module GrokWriter.Types

/// All errors that can occur in the pipeline
type AppError =
    | ApiKeyMissing
    | MicrophoneNotFound
    | RecordingFailed of string
    | SttError of int * string   // statusCode, body
    | SttEmptyResult
    | ChatError of string
    | TypingError of string
    | SoundError of string
    | Unexpected of exn

    override this.ToString() =
        match this with
        | ApiKeyMissing -> "XAI_API_KEY is not set in .env"
        | MicrophoneNotFound -> "No microphone device found"
        | RecordingFailed msg -> $"Recording failed: {msg}"
        | SttError (code, body) -> $"STT error {code}: {body}"
        | SttEmptyResult -> "STT returned empty text"
        | ChatError msg -> $"Chat error: {msg}"
        | TypingError msg -> $"Typing error: {msg}"
        | SoundError msg -> $"Sound error: {msg}"
        | Unexpected ex -> $"Unexpected error: {ex.Message}"

/// STT response from xAI /v1/stt
type SttResponse = {
    text: string
    language: string
    duration: float
    words: SttWord option
}
and SttWord = {
    text: string
    start: float
    ``end``: float
    speaker: int option
}
