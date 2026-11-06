#!/usr/bin/env bash
# Runs the store-shots build (Platforms/iOS/StoreShotsDriver.cs) on a 6.9" iPhone simulator and
# takes the screenshots it asks for: English and Bulgarian, plus a check of the language a
# Bulgarian iPhone gets on its own, and the tallest screens on the smallest iPhone iOS 15+ runs on.
#
#   capture-shots.sh <path to the .app> <output folder>
set -euo pipefail

app="$1"
out="$2"
bundle="com.nksolutions.santase"
mkdir -p "$out"

best="$(xcrun simctl list devices available -j | python3 -c '
import json, re, sys
devices = json.load(sys.stdin)["devices"]
best = None
for runtime, items in devices.items():
    if "iOS" not in runtime:
        continue
    version = [int(x) for x in re.findall(r"\d+", runtime.split("iOS")[-1])]
    for item in items:
        if "Pro Max" in item["name"]:
            key = (version, item["name"])
            if best is None or key > best[0]:
                best = (key, item["udid"], item["name"], runtime)
print(best[1])
print(best[3])
print(best[2], best[3], file=sys.stderr)
')"
udid="$(sed -n 1p <<< "$best")"
runtime="$(sed -n 2p <<< "$best")"

# A clean status bar, as on Apple's own product pages.
boot() {
  echo "Simulator: $udid"
  xcrun simctl boot "$udid" || true
  xcrun simctl bootstatus "$udid" -b
  xcrun simctl status_bar "$udid" override --time "9:41" --dataNetwork wifi --wifiMode active --wifiBars 3 \
    --cellularMode active --cellularBars 4 --batteryState charged --batteryLevel 100
}

# Runs one pass of the driver: run <name> <language> <games> <scenes> [launch arguments...]
run() {
  local name="$1" language="$2" games="$3" scenes="$4"
  shift 4
  local folder="$out/$name"
  mkdir -p "$folder"

  xcrun simctl uninstall "$udid" "$bundle" || true
  xcrun simctl install "$udid" "$app"
  local data
  data="$(xcrun simctl get_app_container "$udid" "$bundle" data)/Documents"
  mkdir -p "$data"

  echo "== $name: language '$language', $games game(s) per opponent"
  SIMCTL_CHILD_SANTASE_SHOT_LANGUAGE="$language" \
  SIMCTL_CHILD_SANTASE_SHOT_GAMES="$games" \
  SIMCTL_CHILD_SANTASE_SHOT_SCENES="$scenes" \
    xcrun simctl launch "$udid" "$bundle" "$@"

  local deadline=$((SECONDS + 2400)) failed=0 polls=0
  while [ ! -f "$data/shots-done.txt" ]; do
    if [ -f "$data/shot-request.txt" ]; then
      local shot
      shot="$(cat "$data/shot-request.txt")"
      xcrun simctl io "$udid" screenshot --type=png "$folder/$shot.png" 2> /dev/null
      rm -f "$data/shot-request.txt"
      echo "  took $shot"
    fi
    if [ "$SECONDS" -gt "$deadline" ]; then
      echo "::error::The $name pass never finished."
      failed=1
      break
    fi
    # Now and then, as a spawn is slow and a stalled one holds up the screenshots. Not grep -q:
    # closing the pipe early kills launchctl, which pipefail reports as a failure.
    polls=$((polls + 1))
    if [ $((polls % 50)) = 0 ] && ! xcrun simctl spawn "$udid" launchctl list 2> /dev/null | grep "UIKitApplication:$bundle" > /dev/null; then
      sleep 2
      if [ ! -f "$data/shots-done.txt" ]; then
        sleep 20 # A crash report takes a moment to appear.
        echo "::error::The app stopped during the $name pass."
        failed=1
        break
      fi
    fi
    sleep 0.2
  done

  cp "$data/shots-log.txt" "$folder/log.txt" 2> /dev/null || true
  cat "$folder/log.txt" 2> /dev/null || true
  if [ -f "$data/shots-error.txt" ]; then
    cp "$data/shots-error.txt" "$folder/error.txt"
    echo "::error::The $name pass failed: $(head -3 "$folder/error.txt")"
    failed=1
  fi
  if [ "$failed" = 1 ]; then
    mkdir -p "$out/crashes"
    cp -R "$HOME/Library/Logs/DiagnosticReports/." "$out/crashes/" 2> /dev/null || true
    xcrun simctl spawn "$udid" log show --last 10m --style compact --predicate "process CONTAINS 'Santase' OR eventMessage CONTAINS '$bundle'" > "$out/crashes/$name-log.txt" 2> /dev/null || true
    exit 1
  fi
  xcrun simctl terminate "$udid" "$bundle" || true
}

boot

store_scenes="01-gameplay,02-marriage,03-hint,04-closed,05-result,06-victory,07-opponents,08-statistics,09-rules,10-settings"
run en en "${SANTASE_SHOT_GAMES:-1}" "$store_scenes"
run bg bg "${SANTASE_SHOT_GAMES:-1}" "$store_scenes"

# No language chosen in the app, a Bulgarian iPhone: the app must start in Bulgarian.
run device-bg "" 0 "" -AppleLanguages "(bg)" -AppleLocale "bg_BG"
grep -q 'Language without a saved choice: bg' "$out/device-bg/log.txt" || { echo "::error::A Bulgarian iPhone did not get Bulgarian."; exit 1; }

# App Store Connect refuses images of another size. Only the two store sets are held to it.
python3 - "$out" <<'EOF'
import pathlib, struct, sys
problems = 0
root = pathlib.Path(sys.argv[1])
for folder in ("en", "bg"):
    for path in sorted((root / folder).glob("*.png")):
        data = path.read_bytes()
        width, height, depth, color = struct.unpack(">IIBB", data[16:26])
        print(path.relative_to(root), width, height, "bit depth", depth, "color type", color)
        if (width, height) != (1320, 2868):
            problems += 1
sys.exit(1 if problems else 0)
EOF

# The longer language on the smallest screen: for looking at, the overlays have no scrolling.
small_type="iPhone SE (3rd generation)"
if small="$(xcrun simctl create "store-shots-small" "$small_type" "$runtime")"; then
  xcrun simctl shutdown "$udid" || true
  udid="$small"
  boot
  run small bg 0 "01-gameplay,05-result,06-gameover,07-opponents,10-settings"

  # And with the system's text a good deal bigger: nothing may end up out of reach.
  xcrun simctl ui "$udid" content_size accessibility-medium
  run small-big-text bg 0 "01-gameplay,05-result,06-gameover,07-opponents"
else
  echo "::warning::No $small_type simulator with $runtime: the small screen was not checked."
fi
