# Taleem Android

Kotlin + Jetpack Compose client. Consumes the shared Taleem API only — never parses PDFs or hardcodes books.

## Run

1. Start backend at `http://localhost:5080` (emulator uses `http://10.0.2.2:5080/`).
2. Open `android/` in Android Studio.
3. Sync Gradle, run `developmentDebug` on an emulator/device.

## Flavors

| Flavor | API base |
|--------|----------|
| development | `http://10.0.2.2:5080/` |
| staging | `https://staging-api.example.com/` |
| production | `https://api.example.com/` |

## Architecture

UI (Compose) → ViewModel → Repository → Retrofit API

Local DataStore: last book/unit only. Online-first.
