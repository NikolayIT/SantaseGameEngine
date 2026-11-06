# App Store listing and release – Santase (iPhone)

App Store Connect app **Santase 66: Offline Card Game** · Apple ID `6831495222` · bundle `com.nksolutions.santase` · SKU
`santase-ios` · seller NK Solutions EOOD, developer name NK Games Studio. The pipeline follows the
Gods & Magic one (its `store/APP_STORE.md`), with the differences a public repository and a MAUI app
need, the same way as Belot's. The Google Play counterpart is `src/UI/Santase.UI/PLAYSTORE.md`.

## Files

- `<locale>.json` – each locale's `name`, `subtitle`, `keywords`, `promotionalText` and
  `description`, plus `whatsNew` from the second version on (Apple refuses it on the first).
- `settings.json` – the app-wide answers: Apple ID, category, age rating, content rights, copyright,
  URLs, review notes, each locale's screenshot folder and order, and the excluded territories.
- `../../tools/AppStoreRelease/` – `listing.mjs` (checks the texts; `--apply` writes them and the
  settings), `screenshots.mjs` (uploads a set), `availability.mjs` (price and territories, set
  once), `release.mjs` (the version; `--submit` sends it to App Review), `setup-signing.mjs` (bundle
  ID, profile, GitHub environment and secrets; run it again once a year, before the profile
  expires), and the scripts the workflow runs on the Mac.
- `../../.github/workflows/ios.yml` – the simulator pass and the signed upload.

**Keys.** This repository is public, so nothing secret enters it. The scripts read the team's key
from the folder `SANTASE_APPLE_KEYS` names, by default `../GodsAndMagic/keys/apple` beside this
repository (`asc.json`, `AuthKey_<id>.p8`, and the team's one Apple Distribution certificate, which
Gods & Magic's `setup-signing.mjs` issues and renews). `.gitignore` refuses `keys/`, `*.p8`, `*.p12`
and `*.mobileprovision`. The workflow gets them from the secrets of the GitHub environment
`app-store`, open only to `master`. The owner is this repository's only writer, so the environment
has no required reviewer; give it one (as Belot's has) if anybody else ever gets write access,
because the key is an Admin key.

## Releasing a version

Bump `ApplicationDisplayVersion` in `Santase.UI.csproj` (`ApplicationVersion` is Android's; iOS takes
the workflow's run number).

```bash
gh workflow run ios.yml --ref master -f task=screenshots   # when the screens changed; artifact app-store-screenshots
gh workflow run ios.yml --ref master -f task=upload
node tools/AppStoreRelease/release.mjs                     # the build shows here within minutes
node tools/AppStoreRelease/release.mjs --create            # the version, released to everyone once approved
# bring whatsNew up to date in every <locale>.json, then:
node tools/AppStoreRelease/listing.mjs --apply
node tools/AppStoreRelease/screenshots.mjs <artifact folder>
node tools/AppStoreRelease/release.mjs --submit            # attach the build and send it to App Review
```

A version's release type is `AFTER_APPROVAL` with no phased release, so everybody gets it as soon
as Apple approves it; `--submit` refuses anything else.

## What is set

- **Locales.** The App Store has no Bulgarian, and Apple's table gives the Bulgarian storefront
  **English (U.K.)** alone. So `en-GB` carries the Bulgarian listing and Bulgarian screenshots, with
  an English paragraph at the end for the other storefronts that read it (the U.K., and the
  countries without a language of their own), and `en-US`, the primary language, the English
  listing and screenshots. The app itself declares both languages (`CFBundleLocalizations` and the
  `.lproj` folders), so the page lists them.
- **Name.** The store has `66 Santase`, `66 Santase - Classic Card Game` and `Santase 66 Online`
  already, hence the longer name. The home screen says Santase or Сантасе, by the phone's language.
- **iPhone only**, portrait, iOS 15 or newer, as Gods & Magic: on an iPad the app runs as an iPhone
  app, and no iPad screenshots are needed.
- **Category:** Games, subcategories Card and Strategy.
- **Age rating:** every answer none or no, as the game has no betting, money, chat or user content
  anyone else sees. Names stay on the device. Links leave for Safari (`Pages/ExternalBrowser.cs`),
  so there is no open web access in the app.
- **Price and availability:** free; every territory except China mainland, which requires a
  government approval number for games; new territories are added automatically.
- **App Privacy:** Data Not Collected. It has no API: App Store Connect → App Privacy. Keep it in
  step with the privacy policy (`nksolutions.com` repository, `public/santase/privacy-policy.html`),
  which Settings opens too, because Apple wants it reachable inside the app.
- **App Review:** the contact is read from the team's other app and is never in this repository;
  the notes come from `settings.json`.

## The iOS build

`Platforms/iOS` holds the app delegate, `Info.plist`, the privacy manifest and the `.lproj` folders
(under `Resources`). On a Mac the project targets `net10.0-ios` alone. On Windows, `SantaseIos=true`
in the environment gives a compile check:

```powershell
$env:SantaseIos='true'
dotnet restore src/UI/Santase.UI/Santase.UI.csproj --force -p:StoreShots=true
dotnet msbuild src/UI/Santase.UI/Santase.UI.csproj -t:Compile -p:TargetFramework=net10.0-ios -p:Configuration=Release -p:StoreShots=true -p:IsMacEnabled=false
$env:SantaseIos=$null
dotnet restore src/UI/Santase.UI/Santase.UI.csproj --force    # back to the normal targets
```

Differences from Android:

- **No Back button.** Every page has its own, but iOS's swipe from the edge pops a page without
  asking, so `AppShell.OnNavigating` refuses it on the table; the Menu button asks first as ever.
  The simulator pass checks that the Shell refuses it.
- **Dark always** (`UIUserInterfaceStyle`): every page is dark felt, so native pickers and the
  keyboard match it, and the status bar stays light.
- **Ahead of time.** iOS has no JIT. Release builds compile everything ahead of time and keep the
  interpreter for what the compiler could not foresee (`MtouchInterpreter=-all`), rather than crash.
  It costs about 7 MB of the IPA (Belot measured 52 against 59 MB). The simulator build leaves it
  out: with it the build takes an hour instead of 8 minutes, and the pass is stricter without it.
- **The workload is pinned** to set `10.0.401` (iOS SDK 26.5, Xcode 26.6) in `select-xcode.sh`:
  newer sets carry iOS SDK 27, which needs Xcode 27, and GitHub's newest image, `macos-26`, stops at
  26.6. Move it when GitHub has `macos-27`.

## Screenshots and the simulator pass

`Platforms/iOS/StoreShotsDriver.cs`, built only with `-p:StoreShots=true` (which also targets the
simulator, so it cannot reach a store build), plays the real app on the newest 6.9" iPhone
simulator on an Apple Silicon runner, where the build runs ahead-of-time compiled like a device's:

1. it checks that the neural player's weights reached the app, since without them that player
   silently falls back to untrained weights;
2. a whole game against each of the five opponents, through the table's own commands, with the
   person playing what the hint says, and a two-player game, at no pace, so the AI's code paths run
   and the statistics hold real games;
3. the store scenes on the real pages, in each language: it writes a scene's name to a file in the
   app's Documents, and `capture-shots.sh` takes it with `simctl io` (1320×2868, the 6.9" iPhone's
   size, status bar set to Apple's usual 9:41), with `10-settings` for checking only;
4. a run with no language chosen on a Bulgarian iPhone, which must start in Bulgarian;
5. the tallest screens, in Bulgarian, on an iPhone SE (`small` in the artifact), for looking at:
   the round and game overlays have no scrolling.

The artifact holds the shots, each pass's `log.txt` and, after a failure, the crash reports.

**This is a simulator, not a device.** Nothing here has run on a real iPhone. App Review's device
is the first; install it from TestFlight on a phone before a release that changes the iOS layer.

## When the app goes live

The first approved version is released automatically. Once its page answers at
https://apps.apple.com/app/id6831495222:

- add the App Store link beside Google Play on the publisher site (`nksolutions.com` repository,
  `public/santase/index.html`);
- add it to the repository's README and to ednaigra.com's Santase page if that links the app.

## Gotchas

- Creating an app has no API (`POST /v1/apps` answers 403): App Store Connect → Apps → New App,
  with the bundle ID `setup-signing.mjs` registered.
- Availability must name every territory, with `available: false` for the excluded ones; a list that
  leaves them out gets a 409.
- A new app's first version already exists as `1.0`; `release.mjs --create` renames it.
- Creating a locale's app info localization also creates its version localization.
- `screenshots.mjs` waits five minutes per locale for Apple to process its images; read
  `assetDeliveryState` before retrying, since a retry deletes the set and starts over.
- App Store text: no emoji, no em dash, and no other platform or store named (`listing.mjs` checks).
- No TestFlight groups: the owner does not want them.
