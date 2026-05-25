# grok-writer

<p align="center">
  <strong>Push-to-talk voice input for Windows powered by xAI Grok Speech-to-Text.</strong><br/>
  Hold a hotkey, speak — your words are transcribed and typed into the active window.
</p>

<p align="center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" />
  <img alt="F#" src="https://img.shields.io/badge/F%23-FsToolkit-378BBA?logo=fsharp&logoColor=white" />
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white" />
  <img alt="STT" src="https://img.shields.io/badge/STT-xAI%20Grok-000000" />
  <img alt="Status" src="https://img.shields.io/badge/status-working-success" />
</p>

---

## What it does

```
F8 ↓   → NAudio starts capturing mic (16 kHz mono WAV)
F8 ↑   → recording stops
         → audio sent to xAI Grok /v1/stt
         → InputSimulatorPlus types the transcript into the focused window
```

Works in any text field — browser, VS Code, Slack, Telegram, anywhere a regular keyboard would.

## Features

- **Global hotkey** — hold `F8` from anywhere, no need to focus any window
- **Low-latency recording** — 16 kHz mono, written straight to a temp WAV
- **Multilingual STT** — Russian, English, Ukrainian and 50+ more via Grok `/v1/stt`
- **Formatted output** — `format=true` returns text with proper punctuation and casing
- **Unicode typing** — Cyrillic, emoji, special chars all work via `SendInput` (Unicode mode)
- **Audio cues** — short `start.wav` / `stop.wav` beeps so you know when you're being recorded
- **Structured logging** — Serilog writes to console + rolling daily file in `logs/`
- **Railway-oriented error handling** — every external call returns `Result`, no unhandled exceptions reach the UI loop

## Tech stack

| Concern         | Library                                 |
|-----------------|------------------------------------------|
| Hotkey          | [H.Hooks](https://github.com/HavenDV/H.Hooks) (LowLevelKeyboardHook) |
| Microphone      | [NAudio](https://github.com/naudio/NAudio) `WaveInEvent` |
| STT             | [xAI Grok](https://docs.x.ai/) `POST /v1/stt` via `HttpClient` |
| Text injection  | [InputSimulatorPlus](https://www.nuget.org/packages/InputSimulatorPlus) (`SendInput` Unicode) |
| Result/error    | [FsToolkit.ErrorHandling](https://github.com/demystifyfp/FsToolkit.ErrorHandling) (`taskResult {}`) |
| Logging         | [Serilog](https://serilog.net/) + Console & File sinks |
| Config          | [dotenv.net](https://github.com/bolorundurowb/dotenv.net) |
| Sounds          | NAudio `WaveOutEvent` + WAVs from [Handy](https://handy.computer/) |

## Quick start

### Prerequisites

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A microphone
- An xAI API key — get one at [console.x.ai](https://console.x.ai)

### Setup

```bash
git clone git@github.com:BashkaMen/grok-writer.git
cd grok-writer
cp .env.example .env
# edit .env and paste your XAI_API_KEY
dotnet run
```

You should see:

```
🎤 Grok Writer — Push-to-Talk (F#)
   Hold [F8] to record, release to stop.
   Grok API key loaded
   Detecting microphone devices:
     [0] Microphone (Razer Seiren V3)
   Listening for hotkeys… (Ctrl+C to exit)
```

Hold `F8`, say something, release. The transcript appears wherever your cursor is.

## Configuration

All config lives in `.env`:

```env
XAI_API_KEY=xai-...
```

Hotkey, sample rate, STT URL and language are constants in `Config.fs` — change them there if needed.

To swap the sounds, drop your own `start.wav` / `stop.wav` into `sounds/`.

## Project layout

```
grok-writer/
├── Types.fs          ← AppState, AppError DUs
├── Config.fs         ← .env loading, hotkey & STT URL
├── Logging.fs        ← Serilog setup (console + logs/grok-writer-YYYYMMDD.log)
├── Sounds.fs         ← Play start.wav / stop.wav via NAudio
├── Stt.fs            ← xAI /v1/stt HTTP client
├── Recorder.fs       ← Mic capture → WAV via NAudio
├── Keyboard.fs       ← InputSimulatorPlus typing wrapper
├── Pipeline.fs       ← stop → transcribe → type → cleanup
├── Program.fs        ← Hook setup + state machine
├── sounds/           ← start.wav, stop.wav
└── logs/             ← daily rolling Serilog files
```

## Architecture

A tiny state machine drives everything:

```
       F8 ↓
Idle ─────────► Recording
                    │
                    │ F8 ↑
                    ▼
                Processing
                    │
                    │ pipeline done / error
                    ▼
                   Idle
```

The hook handler never blocks — `processRecording` runs as a fire-and-forget `Task` so subsequent keystrokes stay responsive.

Every step in the pipeline returns `TaskResult<'a, AppError>`, composed via the `taskResult {}` computation expression from FsToolkit. Errors bubble up to a single sink in `Program.fs` that logs them and resets state to `Idle`.

## Known limits

- **Windows-only** — `H.Hooks` and `InputSimulatorPlus` both wrap Win32 APIs
- **English-only `format` heuristics** — `format=true` works for any language but punctuation quality is best for English
- **No streaming** — `/v1/stt` is a batch endpoint, text appears only after `F8 ↑`
- **No LLM correction step (yet)** — raw STT output is typed as-is; semantic correction would require a second Chat API call

## Roadmap ideas

- [ ] LLM post-correction step (cleaner punctuation, homophone disambiguation)
- [ ] Configurable hotkey via `.env`
- [ ] Tray icon + visual recording indicator
- [ ] Streaming variant if/when xAI exposes a streaming STT endpoint

## Acknowledgements

Start/stop chime sounds are from [Handy](https://handy.computer/) — an excellent open-source voice input app. Used under their respective license.

---

<p align="center">
  Built with F# • Powered by xAI Grok
</p>
