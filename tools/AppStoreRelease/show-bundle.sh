#!/usr/bin/env bash
# Prints what App Review and the App Store read from an app bundle, and fails when the settings
# they depend on are missing.
set -euo pipefail

app="$1"
plist="$app/Info.plist"
buddy() { /usr/libexec/PlistBuddy -c "Print :$1" "$plist" 2>/dev/null || echo "(missing)"; }

du -sh "$app"
for key in CFBundleIdentifier CFBundleName CFBundleDisplayName CFBundleShortVersionString CFBundleVersion \
  MinimumOSVersion UIDeviceFamily UISupportedInterfaceOrientations UIUserInterfaceStyle UIStatusBarStyle \
  ITSAppUsesNonExemptEncryption CFBundleLocalizations UILaunchStoryboardName CFBundleIconName; do
  echo "$key: $(buddy "$key" | tr '\n' ' ')"
done

echo "CFBundleIcons: $(buddy CFBundleIcons | tr -s ' 
' ' ')"
echo "Largest files (KB):"
find "$app" -type f -exec du -k {} + | sort -rn | awk 'NR <= 8' | sed "s|$app/||"

ls -d "$app"/*.lproj
ls "$app" | grep -i -E 'PrivacyInfo|AppIcon|Assets.car' || true

test -f "$app/PrivacyInfo.xcprivacy"
test -f "$app/bg.lproj/InfoPlist.strings"
test "$(buddy CFBundleIdentifier)" = "com.nksolutions.santase"
test "$(buddy ITSAppUsesNonExemptEncryption)" = "false"
test "$(buddy CFBundleIconName)" = "appicon"
echo "Bundle checks pass."
