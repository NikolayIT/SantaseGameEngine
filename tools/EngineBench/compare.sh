#!/usr/bin/env bash
# Paired/alternating A-vs-B comparison to cancel thermal drift and run-order bias.
# Usage: compare.sh <baselineDir> <variantDir> [games] [pairs] [repeatsPerProc]
set -euo pipefail

BASE_DLL="$1/EngineBench.dll"
VAR_DLL="$2/EngineBench.dll"
GAMES="${3:-80000}"
PAIRS="${4:-8}"
REPEATS="${5:-6}"

label="$(basename "$2")"
echo "=== compare baseline vs ${label}  (games=$GAMES, pairs=$PAIRS, repeats=$REPEATS, alternating order) ==="

extract() { # $1=dll -> echoes "bestcpu bygame"
  dotnet "$1" "$GAMES" "$REPEATS" 2>/dev/null \
    | awk '/^RESULT/{for(i=1;i<=NF;i++){split($i,a,"=");if(a[1]=="bestcpu")c=a[2];if(a[1]=="bygame")b=a[2]}print c,b}'
}

rr=""; bb=""; vb=""
for ((p=1; p<=PAIRS; p++)); do
  if (( p % 2 == 1 )); then
    read -r b_cpu b_bg < <(extract "$BASE_DLL")
    read -r v_cpu v_bg < <(extract "$VAR_DLL")
  else
    read -r v_cpu v_bg < <(extract "$VAR_DLL")
    read -r b_cpu b_bg < <(extract "$BASE_DLL")
  fi
  ratio="$(awk "BEGIN{print $v_cpu/$b_cpu}")"
  printf "  pair %d: base %8.0f g/cpu-s  %7.0f B   |  var %8.0f g/cpu-s  %7.0f B   (ratio %.3f)\n" \
    "$p" "$b_cpu" "$b_bg" "$v_cpu" "$v_bg" "$ratio"
  rr="$rr $ratio"; bb="$bb $b_bg"; vb="$vb $v_bg"
done

med() { tr ' ' '\n' <<<"$1" | grep -v '^$' | sort -n | awk '{a[NR]=$1} END{print (NR%2)?a[(NR+1)/2]:(a[NR/2]+a[NR/2+1])/2}'; }
# Median of per-pair ratios: each ratio is measured back-to-back so thermal/turbo
# drift cancels inside the pair; the median over pairs is the robust speedup estimate.
rm_=$(med "$rr"); bbm=$(med "$bb"); vbm=$(med "$vb")
awk -v rm="$rm_" -v bbm="$bbm" -v vbm="$vbm" 'BEGIN{
  printf "  --- SPEEDUP (median of per-pair ratios): %+.2f%%\n", (rm-1)*100;
  printf "  --- alloc: base %.0f  var %.0f   -> %+.2f%% (%+.0f B/game)\n", bbm, vbm, (vbm/bbm-1)*100, vbm-bbm;
}'
