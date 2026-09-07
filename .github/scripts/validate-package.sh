#!/usr/bin/env bash
#
# UPM 패키지 정합성 검사. Unity 라이선스도 에디터도 필요 없다.
# CI 에서도 로컬에서도 같은 것을 돈다:  .github/scripts/validate-package.sh
#
set -uo pipefail

PKG_ROOT="Assets/UISystem"
PKG="$PKG_ROOT/package.json"
CHANGELOG="$PKG_ROOT/CHANGELOG.md"

fails=0
fail() { printf '  \033[31mFAIL\033[0m %s\n' "$1"; fails=$((fails + 1)); }
pass() { printf '  \033[32mok\033[0m   %s\n' "$1"; }
section() { printf '\n\033[1m%s\033[0m\n' "$1"; }

cd "$(git rev-parse --show-toplevel)"

# 최상위 2칸 들여쓰기 필드만 뽑는다. author.name 같은 중첩 필드에 걸리지 않게.
pkg_field() { sed -n "s/^  \"$1\"[[:space:]]*:[[:space:]]*\"\([^\"]*\)\".*/\1/p" "$PKG" | head -1; }

# ---------------------------------------------------------------- package.json
section "package.json"

if [ ! -f "$PKG" ]; then
  fail "$PKG 이 없다. 패키지 루트가 맞는지 확인할 것"
  exit 1
fi

if command -v jq >/dev/null 2>&1; then
  if jq empty "$PKG" 2>/dev/null; then pass "JSON 문법"; else fail "JSON 문법이 깨졌다"; fi
fi

PKG_NAME=$(pkg_field name)
PKG_VERSION=$(pkg_field version)

for f in name version displayName unity; do
  if [ -n "$(pkg_field "$f")" ]; then pass "$f = $(pkg_field "$f")"; else fail "$f 필드가 비었다"; fi
done

if printf '%s' "$PKG_NAME" | grep -qE '^[a-z0-9]+(\.[a-z0-9-]+)+$'; then
  pass "name 형식"
else
  fail "name 은 com.company.package 형식이어야 한다: $PKG_NAME"
fi

if printf '%s' "$PKG_VERSION" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+'; then
  pass "version 형식"
else
  fail "version 이 유의적 버전이 아니다: $PKG_VERSION"
fi

# ------------------------------------------------------------------ CHANGELOG
section "CHANGELOG"

if [ ! -f "$CHANGELOG" ]; then
  fail "$CHANGELOG 이 없다"
elif grep -q "^## \[$PKG_VERSION\]" "$CHANGELOG"; then
  pass "$PKG_VERSION 항목이 있다"
else
  fail "CHANGELOG 에 '## [$PKG_VERSION]' 항목이 없다"
fi

# -------------------------------------------------------------- 패키지 경계
# git URL 패키지는 ?path= 폴더를 통째로 복사한다. 파일 단위 제외가 불가능하므로
# 경계 안에 나가면 안 되는 것이 섞이지 않았는지가 이 저장소의 핵심 불변식이다.
section "패키지 경계 ($PKG_ROOT)"

leaked=$(git ls-files "$PKG_ROOT" | grep -E '\.unity$|/Samples/|/Resources/|\.prefab$' || true)
if [ -z "$leaked" ]; then
  pass "씬 · 프리팹 · 샘플 · Resources 가 경계 안에 없다"
else
  fail "경계 안에 있으면 안 되는 파일:"
  printf '         %s\n' $leaked
fi

for outside in Assets/Samples Assets/Resources docs; do
  if [ -z "$(git ls-files "$PKG_ROOT/$(basename "$outside")")" ]; then
    pass "$outside 는 경계 밖"
  else
    fail "$outside 가 경계 안으로 들어왔다"
  fi
done

# ---------------------------------------------------------------- meta 정합성
section "meta 파일"

missing=0
orphan=0

while IFS= read -r f; do
  [ -e "$f.meta" ] || { fail "meta 없음: $f"; missing=$((missing + 1)); }
done < <(git ls-files Assets | grep -v '\.meta$')

# 공백이 든 경로(TextMesh Pro 등)가 있으므로 xargs 로 쪼개지 않는다.
while IFS= read -r d; do
  [ "$d" = "Assets" ] && continue
  [ -e "$d.meta" ] || { fail "폴더 meta 없음: $d"; missing=$((missing + 1)); }
done < <(while IFS= read -r f; do dirname "$f"; done < <(git ls-files Assets) | sort -u)

while IFS= read -r m; do
  [ -e "${m%.meta}" ] || { fail "고아 meta: $m"; orphan=$((orphan + 1)); }
done < <(git ls-files Assets | grep '\.meta$')

[ "$missing" -eq 0 ] && pass "모든 에셋과 폴더에 meta 가 있다"
[ "$orphan" -eq 0 ] && pass "고아 meta 없음"

# -------------------------------------------------------------------- asmdef
section "asmdef"

while IFS= read -r a; do
  if command -v jq >/dev/null 2>&1 && ! jq empty "$a" 2>/dev/null; then
    fail "JSON 문법이 깨졌다: $a"
  else
    pass "$(basename "$a")"
  fi
done < <(git ls-files '*.asmdef')

# --------------------------------------------------------------------- 결과
section "결과"
if [ "$fails" -eq 0 ]; then
  printf '  \033[32m통과\033[0m — %s %s 를 배포할 수 있다\n\n' "$PKG_NAME" "$PKG_VERSION"
  exit 0
fi

printf '  \033[31m%d건 실패\033[0m\n\n' "$fails"
exit 1
