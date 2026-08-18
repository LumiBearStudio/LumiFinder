#!/usr/bin/env python3
"""
LumiFiles 로컬라이제이션 정합성 검사

Span 업스트림에서 기능을 포팅할 때마다 번역이 덜 채워진 채 딸려오는 일이 있어
(예: Span #43/#45 는 한국어만 번역하고 나머지 7개 언어에 영어를 그대로 넣어 배포)
포팅 후 이 스크립트를 돌려 확인한다.

검사 항목
  1. 구조   — 모든 항목이 (키 + 9개 언어) = 10개 문자열을 갖는가
  2. 공백   — 빈 문자열이 들어간 슬롯이 있는가
  3. 미번역 — 영어 원문이 그대로 복사된 슬롯 (자리표시자로 추정되는 것만)
  4. 폴란드 — LocalizationPolish.cs 가 기본 사전의 키를 모두 덮는가

사용법
    python tools/check-localization.py
    → 문제가 있으면 종료 코드 1

번역 슬롯 순서는 LocalizationData.cs 튜플 순서와 일치해야 한다:
    (key, en, ko, ja, zh-Hans, zh-Hant, de, es, fr, pt-BR)
"""

import io
import os
import sys

# Windows 기본 콘솔(cp949)에서 한글·em-dash 출력이 깨지지 않도록 UTF-8 로 고정
try:
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

LANGS = ["en", "ko", "ja", "zh-Hans", "zh-Hant", "de", "es", "fr", "pt-BR"]

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, "src", "LumiFiles", "LumiFiles", "Services", "LocalizationData.cs")
POLISH = os.path.join(ROOT, "src", "LumiFiles", "LumiFiles", "Services", "LocalizationPolish.cs")

# 영어와 동일한 것이 '정상'인 키 — 고유명사·브랜드명·UI 개념명.
# 여기 없는 키가 여러 언어에서 영어와 같으면 미번역 자리표시자로 의심한다.
INTENTIONALLY_SAME = {
    "MillerColumns",            # UI 개념 고유명
    "Help_MillerColumns",
    "Settings_ShelfEnabled",    # 제품 기능명 (File Shelf)
    "Settings_BuyMeCoffee",     # 서비스 브랜드명
    "Settings_SupportStoreTitle",
    "Settings_PlanFree",
    "Preview_Git",
    "ColumnGit",
    "Preview_Hash",             # SHA256
    "Shortcuts_Shelf",
}

# 자리표시자 판정 기준: 영어가 이 단어 수 이상이고, 이 개수 이상의 언어가 영어와 동일
MIN_WORDS = 2
MIN_LANGS = 3


def parse_entries(text):
    """('key', 'en', ...) 튜플을 줄바꿈에 상관없이 추출. (entries, malformed) 반환."""
    entries, malformed = [], []
    i, n = 0, len(text)
    while i < n:
        j = text.find('("', i)
        if j < 0:
            break
        if j > 0 and text[j - 1] not in " \t\n\r":
            i = j + 2
            continue
        vals, k, closed = [], j + 1, False
        while k < n:
            c = text[k]
            if c == '"':
                k += 1
                buf = []
                while k < n:
                    ch = text[k]
                    if ch == "\\" and k + 1 < n:
                        buf.append(text[k:k + 2])
                        k += 2
                        continue
                    if ch == '"':
                        break
                    buf.append(ch)
                    k += 1
                vals.append("".join(buf))
                k += 1
                continue
            if c == ")":
                closed = True
                break
            if c in " \t\r\n,":
                k += 1
                continue
            break  # 튜플이 아닌 다른 구문
        if closed and vals:
            if len(vals) == len(LANGS) + 1:
                entries.append(vals)
            elif len(vals) > 1:
                malformed.append((vals[0], len(vals)))
            i = k
        else:
            i = j + 2
    return entries, malformed


def polish_keys(text):
    """LocalizationPolish.cs 의 ["key"] = "..." 항목에서 키만 추출."""
    keys, i = set(), 0
    while True:
        a = text.find('["', i)
        if a < 0:
            break
        b = text.find('"]', a + 2)
        if b < 0:
            break
        key = text[a + 2:b]
        if key and all(ch.isalnum() or ch == "_" for ch in key):
            keys.add(key)
        i = b + 2
    return keys


def main():
    problems = 0

    src = io.open(DATA, encoding="utf-8-sig").read()
    entries, malformed = parse_entries(src)
    print("기본 사전: %d개 항목 (%s)" % (len(entries), os.path.basename(DATA)))

    # 1. 구조
    if malformed:
        problems += len(malformed)
        print("\n[구조 오류] 값이 %d개가 아닌 항목 %d건:" % (len(LANGS) + 1, len(malformed)))
        for key, cnt in malformed:
            print("   - %s (값 %d개)" % (key, cnt))

    # 2. 빈 문자열
    empties = []
    for r in entries:
        for idx, lang in enumerate(LANGS, start=1):
            if r[idx].strip() == "":
                empties.append((r[0], lang))
    if empties:
        problems += len(empties)
        print("\n[빈 문자열] %d건:" % len(empties))
        for key, lang in empties[:20]:
            print("   - %s (%s)" % (key, lang))

    # 3. 미번역 자리표시자
    placeholders = []
    for r in entries:
        if r[0] in INTENTIONALLY_SAME:
            continue
        en = r[1]
        same = [LANGS[i - 1] for i in range(2, len(LANGS) + 1) if r[i] == en]
        if len(en.split()) >= MIN_WORDS and len(same) >= MIN_LANGS:
            placeholders.append((r[0], en, same))
    if placeholders:
        problems += len(placeholders)
        print("\n[미번역 의심] %d건 — 영어 원문이 그대로 복사됨:" % len(placeholders))
        for key, en, same in placeholders:
            print("   - %s" % key)
            print("       en='%s'" % (en if len(en) <= 60 else en[:57] + "..."))
            print("       미번역 %d개 언어: %s" % (len(same), ", ".join(same)))
        print("\n   * 고유명사라 의도된 것이면 이 스크립트의 INTENTIONALLY_SAME 에 키를 추가하세요.")

    # 4. 폴란드어 커버리지 (별도 파일 구조)
    if os.path.exists(POLISH):
        pol = polish_keys(io.open(POLISH, encoding="utf-8-sig").read())
        base_keys = [r[0] for r in entries]
        missing = [k for k in base_keys if k not in pol]
        orphan = [k for k in pol if k not in set(base_keys)]
        print("\n폴란드어: %d개 키 / 기본 %d개" % (len(pol), len(base_keys)))
        if missing:
            problems += len(missing)
            print("[폴란드어 누락] %d건 (영어로 폴백됨):" % len(missing))
            for k in missing[:20]:
                print("   - %s" % k)
        if orphan:
            # 기본에 없는 키 = Span 전용 기능의 잔여 번역. 동작에는 무해하므로 경고만.
            print("   (참고) 기본 사전에 없는 폴란드어 키 %d건 — 미사용, 무해" % len(orphan))

    print("")
    if problems:
        print("결과: 문제 %d건" % problems)
        return 1
    print("결과: 이상 없음 — %d개 항목 × %d개 언어 + 폴란드어 전량 커버" % (len(entries), len(LANGS)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
