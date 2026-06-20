# Publishing Santase to Google Play

The MAUI app (`Santase.UI`) builds a Play-ready `.aab`. What remains before a first upload
is **signing** and the **Play Console listing** — neither lives in the app code.

- **Package name:** `com.nksolutions.santase` — permanent after the first upload. Do not change it.
- **Privacy policy URL:** https://nksolutions.com/santase/privacy-policy.html
- **Versioning:** bump `<ApplicationVersion>` (the integer Android versionCode) in
  `Santase.UI.csproj` on **every** upload; Play rejects a reused versionCode.
  `<ApplicationDisplayVersion>` is the human-facing "1.0".

## 1. Create your upload keystore (once)

Google Play uses **Play App Signing**: Google holds the real *app signing key*; you sign
uploads with an *upload key* that you generate and keep. Play Console does **not** make the
upload key for you — you create it locally with `keytool` (ships with the JDK / .NET Android):

```powershell
keytool -genkeypair -v -keystore santase-upload.keystore -alias santase `
    -keyalg RSA -keysize 2048 -validity 10000
```

The keystore currently lives at `src/UI/Santase.UI/santase-upload.keystore` (alias `santase`).
It is **git-ignored** (`*.keystore`), so it never enters version control — but that also means
it is **not** backed up by the repo: keep an **off-machine copy** of the file and its passwords.
If you lose the file you can reset the upload key via Play Console support, but never lose the
*passwords* you set when creating it.

## 2. Build a signed AAB

The script auto-detects `santase-upload.keystore` next to it and defaults the alias to
`santase`, so you only need to provide the two passwords (read by the build via `env:` — they
never touch the command line, the repo, or git):

```powershell
$env:SANTASE_KEYSTORE_PASS = '***'   # keystore (store) password
$env:SANTASE_KEY_PASS      = '***'   # key (alias) password
.\publish-android.ps1
```

Override the location/alias only if they differ: set `SANTASE_KEYSTORE` and/or
`SANTASE_KEY_ALIAS`.

Output: `bin\Release\net10.0-android\com.nksolutions.santase-Signed.aab` — the file you upload.

> A plain `dotnet publish -f net10.0-android -c Release` (no signing props) produces an AAB
> signed with the shared **Android debug key** (`CN=Android Debug`). Play **rejects** that —
> always go through the keystore path above for store builds.

## 3. Play Console (one-time, outside this repo)

Create the app in https://play.google.com/console, then complete:

- **App signing:** opt in to Play App Signing (default), upload the AAB from step 2.
- **Store listing:** title, short + full description, app icon (512×512 PNG — export from
  `Resources/AppIcon`), feature graphic (1024×500), ≥2 phone screenshots (+ tablet if offered).
- **Privacy policy:** paste the URL above.
- **Data safety:** the current build collects **no** data and makes no network calls — declare
  "No data collected / shared". (Revisit when the online mode ships; update the privacy policy too.)
- **Content rating** questionnaire, **target audience**, **app category** (Games › Card),
  and **countries/pricing** (free).

## Technical readiness (already handled in code)

- target SDK = API 36 (.NET 10 Android) — above Play's current minimum target.
- min SDK = API 21 (Android 5.0).
- Custom app icon + splash (`Resources/AppIcon`, `Resources/Splash`); no template assets.
- Permissions: `INTERNET` + `ACCESS_NETWORK_STATE`, reserved for the upcoming online mode.
