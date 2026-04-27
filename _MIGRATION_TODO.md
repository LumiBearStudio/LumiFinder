# LumiFiles — Rebuild Checklist (v2)

**Status**: 신규 프로젝트로 간주. Span 최신 src를 fresh import 후 `_ui_mockup_v3_glass.html` 디자인을 1:1로 구현.
**핵심 원칙**: 지금 SPAN UI(Windows 표준 다크)와 목표 UI(Tahoe Liquid Glass)는 **완전히 다른 느낌**이다. "비슷하게"가 아니라 **목업 그대로** 만든다.

---

## 0. 컨텍스트

**시작점 (캡처 1)** — 현재 SPAN Finder
- Windows 표준 다크 테마, 직사각 윈도우, 시스템 블루 액센트
- 한국어 시스템 폴더 사이드바, 큰 주소바 + 우측 검색

**목표 (캡처 2 = `_ui_mockup_v3_glass.html`)** — Tahoe Liquid Glass
- 24px 둥근 윈도우 + vivid purple gradient wallpaper 노출
- Floating glass sidebar (18px radius, 4면 모두 둥글, blur 80px)
- Amber 액센트 #FFB86B, Inter 폰트
- Favorites / Locations / Tags 영어 3섹션
- 컴팩트 pill 툴바, 컬러 닷 탭, 카운트 헤더 미러 컬럼

**이전 시도가 실패한 이유**: 토큰을 추측으로 적용. 디자인 검증 단계 없이 누적 — 이번엔 토큰 표(`_DESIGN_TOKENS.md`)를 먼저 만들고 단계별 빌드+스크린샷으로 검증한다.

---

## 1. Fresh import (Span src → LumiFiles)

```powershell
# Visual Studio 완전 종료
robocopy "D:\11.AI\Span\src" "D:\11.AI\LumiFiles\.claude\worktrees\adoring-margulis-de0aae\src" /E /R:2 /W:2 `
  /XD "bin" "obj" /XF "*.user" "*.suo" "*.pfx"
```

확인:
- `src/Span/Span/` (Span 원본 폴더 구조)이 들어왔는지
- `bin/`, `obj/`, `.pfx`, `.user` 없는지

---

## 2. 리브랜딩 (Span → LumiFiles)

폴더/파일:
- [ ] `src/Span/` → `src/LumiFiles/`
- [ ] `src/LumiFiles/Span/` → `src/LumiFiles/LumiFiles/`
- [ ] `Span.csproj` → `LumiFiles.csproj`
- [ ] `Span.sln/.slnx`, `Span.Thumbs`, `Span.Tests`, `Span.UITests`, `Span.ScreenshotAnalyzer` → `LumiFiles.*`

내용 일괄 치환:
- [ ] `namespace Span` → `namespace LumiFiles` (코드 + XAML `x:Class`, `xmlns`)
- [ ] `using Span.` → `using LumiFiles.`
- [ ] `.sln`, `.csproj` 내부 경로/이름

`Package.appxmanifest`:
- [ ] `<Identity Name="LumiBearStudio.SPANFinder">` → `LumiBearStudio.LumiFiles`
- [ ] `<DisplayName>Span Finder</DisplayName>` → `Lumi Files`
- [ ] AppExecutionAlias `spanfinder.exe` → `lumifiles.exe`
- [ ] `mp:PhoneIdentity PhoneProductId` — **새 GUID 발급** (`[guid]::NewGuid()`)
- [ ] Visual Elements / Description 새 브랜드

서명 인증서:
- [ ] 기존 `*.pfx` 삭제 후 VS에서 `LumiFiles_Dev.pfx` 새로 생성

데이터 경로:
- [ ] DebugLogger `Span/Logs` → `LumiFiles/Logs`
- [ ] LocalSettings는 Package Identity별로 자동 분리 → 키 이름 변경 불필요

온보딩:
- [ ] `Assets/Onboarding/index.html`: "SPAN Finder" → "Lumi Files"
- [ ] `Assets/Onboarding/script.js`: 9개 언어 텍스트 일괄
- [ ] `Views/OnboardingWindow.xaml.cs`: Title + virtual host

DefaultFileManagerService:
- [ ] AliasPath `spanfinder.exe` → `lumifiles.exe`

---

## 3. 빌드 검증 (디자인 작업 들어가기 전 필수)

```bash
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -p:Platform=x64
```
- 에러 0 + F5 실행 → 타이틀바 "Lumi Files" 확인
- **이 시점은 아직 SPAN UI 모양 그대로 (캡처 1)** — 정상

---

## 4. Glass 디자인 변환 (`_REBUILD_PLAN.md` 참조)

토큰 표 (`_DESIGN_TOKENS.md`)와 단계별 절차(`_REBUILD_PLAN.md`)에 따라 진행. 매 단계마다:
1. 토큰 표에서 해당 영역 값 확인
2. XAML/리소스에 적용
3. 빌드 + 스크린샷
4. 목업과 시각 비교 → 사용자 OK 후 다음 단계

---

## 5. 에셋 교체 (디자인 변환 후)
- [ ] Assets/Square*Logo*, StoreLogo, Wide310x150Logo*, SplashScreen*, LockScreenLogo* — Lumi 로고
- [ ] Assets/app.ico
- [ ] Assets/Onboarding/app-icon.png
- [ ] Assets/icon_source.svg

---

## 6. 원본 Span 변경 동기화 (장기)

- 참고: https://github.com/LumiBearStudio/SpanFinder.git (reference-only, git remote 추가 안 함)
- 로컬: `D:\11.AI\Span\src`
- 방향: 단방향 Span → LumiFiles. 네임스페이스 차이로 자동 머지 불가, 수동 diff 비교
- LumiFiles의 glass 변환은 Span에 역반영 안 함
