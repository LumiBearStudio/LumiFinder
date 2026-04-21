# LumiFiles — Migration & Rebrand Checklist

**Status**: Span에서 클린 복사 직후 작업 목록. 새 세션에서 이 파일을 읽고 시작.

---

## 0. 복사 전략 (사용자가 직접 진행)

**Span 소스에서 LumiFiles로 복사할 대상**:
- ✅ `src/` (모든 소스 코드)
- ✅ `Span.sln` (솔루션 — 이름 변경 예정)
- ✅ `.gitignore`
- ✅ `global.json` (.NET SDK 버전)
- ✅ `build-msix.bat` (경로 수정 예정)
- ✅ 표준 OSS 문서: `LICENSE`, `LICENSE.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, `CONTRIBUTING.md`, `PRIVACY.md`, `OpenSourceLicenses.md`
- ✅ `.github/ISSUE_TEMPLATE/`, `.github/PULL_REQUEST_TEMPLATE.md` (템플릿 — 이름 수정 예정)

**복사하지 말 것** (혹시 섞였으면 삭제):
- ❌ `.git/`, `.vs/`, `.claude/` — 새 git init 예정, VS 로컬 캐시, Claude 세션 상태
- ❌ `bin/`, `obj/` (모든 하위 포함) — 빌드 아티팩트, ~930MB 차지
- ❌ `AppPackages/`, `Logs/` — 릴리즈 출력물, 런타임 로그
- ❌ `*.pfx` — 개발용 서명 인증서 (새로 생성 필요)
- ❌ `*.user`, `*.suo` — VS 사용자 설정
- ❌ Span 특화 로컬 문서/자산: `MS Store/`, `MS Store README/`, `github-docs/`, `design/`, `docs/`
- ❌ 임시/테스트: `2.png`, `3.png`, `image.png`, `pri_dump.xml`, `README` (확장자 없는 파일), `_test_nfd_creator/`, `test-sentry/`
- ❌ Span 특화 기능 문서 (필요 시 재작성): `FEATURES.md`, `ROADMAP.md`, `AGENTS.md`, `README.md`

**확실한 복사 명령 (PowerShell, 관리자 권한 불필요)**:
```powershell
# Visual Studio 먼저 완전 종료 (.vs 파일 잠금 해제)
robocopy "D:\11.AI\Span" "D:\11.AI\LumiFiles" /E /R:2 /W:2 `
  /XD ".git" ".vs" ".claude" "AppPackages" "Logs" "bin" "obj" `
      "MS Store" "MS Store README" "github-docs" "design" "docs" `
      "_test_nfd_creator" "test-sentry" `
  /XF "2.png" "3.png" "image.png" "pri_dump.xml" "README" `
      "*.pfx" "*.user" "*.suo" "AGENTS.md" "FEATURES.md" "ROADMAP.md" "README.md"
```
`/R:2 /W:2` = 재시도 2회/2초 대기만 — 파일 잠김 있어도 금방 포기.

---

## 1. 복사 완료 후 정리 (새 세션에서 검증)

```powershell
# 확인 명령 — 전부 "False"여야 정상
$exclude = @('.git', '.vs', '.claude', 'bin', 'obj', 'AppPackages', 'Logs')
foreach ($e in $exclude) {
  Test-Path "D:\11.AI\LumiFiles\$e" | ForEach-Object { "$e : $_" }
}
```

또 src 내부 bin/obj 잔존 확인:
```powershell
Get-ChildItem "D:\11.AI\LumiFiles\src" -Recurse -Directory -Include "bin","obj" |
  Select-Object FullName
```
결과 있으면 삭제.

---

## 2. 리브랜딩 체크리스트

### 2-1. 파일/폴더 이름
- [ ] `Span.sln` → `LumiFiles.sln`
- [ ] `src/Span/` → `src/LumiFiles/`
- [ ] `src/LumiFiles/Span/Span.csproj` → `src/LumiFiles/LumiFiles/LumiFiles.csproj`
- [ ] `Span.slnx`, `Span.Thumbs`, `Span.Tests`, `Span.UITests`, `Span.ScreenshotAnalyzer` — 모두 `LumiFiles.*`로 개명
- [ ] `SpanFinder_Dev.pfx` 삭제 (새로 생성)

### 2-2. 솔루션/프로젝트 파일 내용
- [ ] `LumiFiles.sln`에서 프로젝트 경로/이름 `Span` → `LumiFiles` 치환
- [ ] 각 `.csproj`:
  - `<RootNamespace>Span</RootNamespace>` → `<RootNamespace>LumiFiles</RootNamespace>`
  - `<AssemblyName>` 있으면 변경
  - ProjectReference 경로 갱신
- [ ] `global.json` — 변경 없음 (SDK 버전만)

### 2-3. Package.appxmanifest
파일: `src/LumiFiles/LumiFiles/Package.appxmanifest`
- [ ] `<Identity Name="LumiBearStudio.SPANFinder"` → `LumiBearStudio.LumiFiles`
- [ ] `<DisplayName>Span Finder</DisplayName>` → `Lumi Files`
- [ ] `<PublisherDisplayName>` — 그대로 `LumiBear Studio`
- [ ] AppExecutionAlias `spanfinder.exe` → `lumifiles.exe`
- [ ] `mp:PhoneIdentity` PhoneProductId — 새 GUID 생성 (절대 기존 것 재사용 금지, 기존 Span Store 앱과 충돌)
- [ ] Visual Elements, Description 등 모두 새 브랜드로

### 2-4. 네임스페이스 일괄 치환
- [ ] 전체 프로젝트에서 `namespace Span` → `namespace LumiFiles` (sed / VS 솔루션 Find & Replace)
- [ ] `using Span.` → `using LumiFiles.`
- [ ] XAML `xmlns:x="clr-namespace:Span..."` 모두 치환
- [ ] `x:Class="Span..."` 모두 치환
- [ ] 리소스 키 `Span*Brush`, `Span*Style` 등은 **그대로 유지 가능** (추후 테마 리브랜드 시 함께)
- **주의**: DebugLogger 경로 "Span/Logs" 같은 **사용자 데이터 경로**는 **그대로 유지** 하지 않음 — `LumiFiles/Logs`로 변경 (사용자 데이터 분리)
- **주의**: SettingsService가 쓰는 ApplicationData LocalSettings는 **Package Identity별로 자동 분리**되므로 내부 키 이름은 건드릴 필요 없음

### 2-5. 서명 인증서
- [ ] 기존 `SpanFinder_Dev.pfx` 삭제
- [ ] 새 dev cert 생성: Visual Studio → Package.appxmanifest → Packaging 탭 → "Choose Certificate..." → Create test certificate
- [ ] 파일명 `LumiFiles_Dev.pfx`

### 2-6. 에셋 교체 (UI PoC 이후 본격)
- [ ] `Assets/Square*Logo*.png`, `StoreLogo.png`, `Wide310x150Logo*.png`, `SplashScreen*.png`, `LockScreenLogo*.png` — 신규 로고로 교체
- [ ] `Assets/app.ico` — 교체
- [ ] `Assets/Onboarding/app-icon.png` — 교체
- [ ] `Assets/icon_source.svg` — 벡터 원본 교체

### 2-7. 온보딩 텍스트
- [ ] `src/LumiFiles/LumiFiles/Assets/Onboarding/index.html`
  - `<h1 class="welcome-title">SPAN Finder</h1>` → `Lumi Files`
  - `alt="SPAN Finder"` → `alt="Lumi Files"`
- [ ] `src/LumiFiles/LumiFiles/Assets/Onboarding/script.js` I18N 9개 언어
  - `welcome_subtitle` 새 슬로건
  - `dfm_title: 'SPAN Finder를 기본 파일 탐색기로 사용'` → `Lumi Files를...` (9개 언어 전부)
  - `mc_span: 'Span'` → `mc_lumi: 'Lumi'` (또는 그대로, 상징적 유지)
  - shortcuts labels — 필요 시 조정
- [ ] `src/LumiFiles/LumiFiles/Views/OnboardingWindow.xaml.cs`
  - `Title = "SPAN Finder"` → `"Lumi Files"`
  - virtual host `onboarding.span.local` → `onboarding.lumifiles.local`

### 2-8. DefaultFileManagerService
- [ ] `AliasPath` — `spanfinder.exe` → `lumifiles.exe` (Package.appxmanifest 변경과 연계)
- [ ] reg 파일 내용 자동 반영됨 (AliasPath 변수 기반)

---

## 3. Git 초기화

```bash
cd D:/11.AI/LumiFiles
git init
git branch -M main
git add .
git commit -m "Initial import from Span (rebranded to Lumi Files)"
# GitHub에 새 repo 생성: LumiBearStudio/LumiFiles (Private)
git remote add origin https://github.com/LumiBearStudio/LumiFiles.git
git push -u origin main
```

---

## 4. 빌드 검증

```bash
cd D:/11.AI/LumiFiles
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -p:Platform=x64
```
- 에러 0 — 리브랜딩 성공
- 빌드 성공 후 F5로 실행 → 타이틀바/온보딩 등에서 "Lumi Files" 확인

---

## 5. UI PoC (이 체크리스트 이후 별도 작업)

다음 세션에서 `/pdca plan lumi-files-ui` 또는 직접:
- TitleBar 좌측 트래픽라이트 (red/yellow/green)
- Sidebar 톤 (Favorites/Locations/Tags 섹션)
- 기본 뷰 Column View (Miller) 유지, 톤 조정
- 폰트 Inter 또는 Cantarell
- 액센트 컬러 조정

---

## 6. 기타 준비

- [ ] 도메인 `lumifiles.com` 또는 `lumi.files` 확보
- [ ] MS Store Partner Center에서 "Lumi Files" 앱 이름 예약
- [ ] 새 PhoneProductId GUID 생성 (`[guid]::NewGuid()`)
- [ ] 새 개발 인증서
- [ ] 스토어 아이콘 세트 (로고 디자인 결정 후)

---

## 원본 Span 변경 대응 (장기)

Span과 LumiFiles는 같은 엔진. Span에서 버그 픽스나 기능 추가 시:
1. 해당 커밋 해시 기록
2. LumiFiles 쪽에 cherry-pick 또는 수동 반영
3. 네임스페이스 차이 때문에 자동 패치는 어려우니 수동 diff 비교 유지

장기적으로 `Span.Core` 같은 공용 라이브러리 추출 검토 (현재는 리팩토링 비용 > 이득, 6개월 후 재평가).
