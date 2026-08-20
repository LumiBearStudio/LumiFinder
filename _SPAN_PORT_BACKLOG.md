# Span → LumiFiles 포팅 백로그 (전수 검증판)

> 업스트림: https://github.com/LumiBearStudio/SpanFinder (reference-only, git remote 로 추가하지 않음)
>
> **분기 기준점**: `a36a67a` (2026-04-27) = *fresh import from Span **v1.4.13***
> **Span 현재**: v1.7.0 (2026-08-18) — 기준점 이후 실질 커밋 **38건**
>
> **검증 방법**: 각 Span 커밋의 실제 추가 코드에서 고유 식별자를 뽑아 LumiFiles 소스에 대조.
> 비율만으로 판정되지 않는 항목(이동된 코드가 우연히 일치)은 개별 식별자로 재확인함.
> 생성물(obj/bin)은 제외.

---

## ✅ 이미 반영됨 (포팅 불필요 — 검증 완료)

| Span 커밋 | 내용 | 근거 |
|---|---|---|
| `0d73660` | Miller Columns 가로 스크롤 흔들림 | LumiFiles `f3c1caf` 명시 포팅 |
| `d8b150e` `eeb95fd` `3ac7827` | 온보딩 무한 재출현 (Discussion #29/#30) | LumiFiles `417a36c` |
| `a933430` | AddressBar PlaceholderText 다국어 | 6/6 일치 |
| `3882cb0` | 단축키 설정 다국어 | 신규 키 11/11 존재 |
| `a729c2e` | #28 휴지통 모드 툴팁 다국어 | LumiFiles 자체 i18n 패스(S-3.40)로 커버 |
| `0c27aa5` `e3db295` `0e262dc` `d7193bc` `ba26438` | #61 복사/삭제 진행률·취소 + 후속 | **2026-08-18 포팅 완료** |
| `6adecf9` | #62 (1)(2) 탐색기 클립보드 상호운용 | **2026-08-18 포팅 완료** |
| `3bc2aad` | 테스트 프로젝트 호환 | LumiFiles 는 별도 방식으로 복구(809 통과) |

---

## P1 — 안정성 / 데이터 무결성  ✅ 전건 완료 (2026-08-18)

- [x] **셸 컨텍스트 메뉴 timeout 경로의 STA cross-apartment release** — Span `ab0569f`
  - timeout 시 STA 루프를 먼저 닫고 ThreadPool 에서 `result?.Dispose()` → COM 객체를
    STA 아닌 어퍼트먼트에서 release (E_INVALIDARG / E_UNEXPECTED 크래시)
  - `Session.DisposeOnSta()` 추가 + timeout 분기는 `creationDone` 대기 후 `CompleteAdding`
- [x] **v1.4.15 메인 크래시 차단 + 언어 전환 드라이브 표시명** — Span `c4be182`
  - `UnobservedTaskException` 에 취소 전용 예외 필터(`isCancellationOnly`) 추가
  - `Helpers/TaskExtensions.cs`(FireAndForget) 신규 + WorkerProcess/FileSystemWatcher/
    GitStatusBar/DragDrop 타이머 콜백 throw 봉인
  - 언어 변경 시 `RefreshDrives()` — 일본어→한국어 전환 후 이전 언어 표시명이 남던 캐시 버그
- [x] **#62 (3)(4) 사이드바 클릭 피드백** — Span `d7a20d0`
  - hover 를 고정 흰색 5% → 테마 브러시 조회로 변경(라이트 테마에서 안 보이던 문제)
  - `OnSidebarItemPointerPressed/Released` 신규 — 눌림 피드백
  - **LumiFiles 고유 대응**: Span 은 구 사이드바에 적용했으나 LumiFiles 의 실제 화면은
    `LumiSidebar`(V3) 이고 **포인터 피드백이 아예 없었음** → 즐겨찾기/로컬·클라우드·네트워크
    드라이브/휴지통 5개 항목에 연결하고 브러시도 Lumi 토큰(`LumiHoverBrush`,
    `LumiPillHoverBrush`)으로 맞춤. (Settings 항목은 `Border` 라 대상 아님)
  - #62(3) RecycleBin 뷰모드 가드는 `6adecf9` 포팅에 포함되어 이미 반영됨

## P2 — 탐색 / 조작 버그  (9/11 완료, 2026-08-18)

- [x] **#49 Column View 에서 Ctrl+V 가 선택 폴더가 아닌 상위 폴더에 붙여넣음** — `3b669bd`
- [x] **#59 이름변경 중 빈 공간 좌클릭 시 저장되지 않고 취소됨** — `a2839be` (`CommitAnyActiveRename`)
- [x] **#37 새 폴더/파일 생성 후 인라인 rename 자동 진입 race** — `376a1c8`
- [x] **#53 새 컬럼 열 때 스크롤 한 컬럼 부족** — `ca9afba` (누락된 호출 지점 보강)
- [x] **#47 컬럼 가로 스크롤에 애니메이션 비활성 설정 미반영** — `6084bf7`
- [x] **#51 숨김 파일 표시 시 System 속성 항목이 숨겨지던 문제** — `20d8faf`
- [x] **Split 우측 페인 휠 가로 스크롤 안 됨** — `f9cdde7`
- [x] **#48 새 탭이 즉시 middle-click 에 반응하지 않음** — `2db2240`
- [x] **#60 PDF 미리보기 중 해당 PDF 삭제 불가** — `1b356ff`

### ⚠️ 보류 — 아키텍처 분기로 그대로 포팅 불가

- [ ] **#57 이전 컬럼 재선택 시 뷰가 우측 끝으로 점프** — Span `3d34f79`
- [ ] **#57 후속: 미리보기 패널 토글 시 재앵커 점프** — Span `2a55306`

  **증상은 LumiFiles 에 실재함**: `OnLeftAfterReplaceLastColumn`(MainWindow.xaml.cs:2042) /
  `OnRightAfterReplaceLastColumn`(:2074) 가 교체 후 **무조건** `ScrollToLastColumn` +
  `ScrollToLastColumnSync` 를 호출한다. Span 이 #57 에서 조건부로 바꾼 바로 그 코드다.

  **포팅이 막힌 이유** — 같은 문제를 두 프로젝트가 다른 방식으로 풀었다:
  | | ExtentWidth 보존 방식 |
  |---|---|
  | Span | spacer Border 를 제거 컬럼 수만큼 **확장** + ViewChanged 실시간 트리밍 |
  | LumiFiles | S-3.33 `ColumnSwapStarting/Completed` + `ItemsControl.MinWidth` **잠금** |

  Span 의 #57 은 spacer 기구 위에 세워져 있어(`GetMillerSpacer`,
  `OnMillerViewChangedTrimSpacer`, `AdjustSpacerAndScrollAfterReplace`, 동적 탭 패널의
  StackPanel+spacer 구조) 그대로 얹으면 LumiFiles 의 MinWidth 잠금과 **두 기구가 경합**한다.
  실제로 패치 시도 시 5개 헝크가 실패했고, 실패 지점이 전부 분기 지점이다.
  후속 `2a55306` 은 `GetMillerSpacer` 를 직접 호출해 단독 적용 시 컴파일 불가.

  **선택지**
  1. *(작음/권장)* LumiFiles 네이티브 최소 수정 — 재앵커 전에 "새 마지막 컬럼이 이미
     완전히 보이는가" 판정을 추가해 보이면 `ChangeView` 를 건너뜀. 기존 헬퍼
     (`GetTotalColumnsActualWidth`, `ScrollToLastColumnSync`)만 사용, spacer 기구 불필요.
     → **테스트로 검증 불가한 스크롤 동작이라 앱에서 직접 확인 필요**
  2. *(큼)* Span 의 spacer 기구를 통째로 이식하고 S-3.33 MinWidth 잠금을 제거 — 일관성은
     높지만 V3 작업물(S-3.33)을 되돌리는 셈이고 회귀 범위가 큼

## P3 — 기능 추가  ✅ 전건 완료 (2026-08-18)

- [x] **#41 실행 파일(.exe/.lnk) 자체 리소스 아이콘** — `46545d9`
- [x] **#43 기본 뷰 모드 설정** — `6df3694` (Settings 화면 노출 확인)
- [x] **#45 Column View 컬럼 너비 자동 조정 옵션** — `d697420` (`AutoFitColumnWidth` + 토글 UI)
- [x] **#44 "폴더에서 보기" 시 대상 파일 자동 선택** — `57f2909`
- [x] **#50b 파일을 다른 탭 위로 드래그 시 탭 활성화** — `194637d`
      (보이는 Lumi 탭바 DataTemplate 에 연결 확인)
- [x] **#39b 메뉴 키(Apps) + Shift+F10 컨텍스트 메뉴** — `55a7992`
- [x] **#39a 사이드바/홈 즐겨찾기 desktop.ini 커스텀 아이콘** — `a145f9f`
      **LumiFiles 적응 필요했음**: 패치의 XAML 이 Span 구 사이드바에만 적용되어
      화면상 무효 → 보이는 `LumiSidebar` 즐겨찾기 항목에 `CustomIcon`/`GlyphVisibility`
      바인딩을 직접 연결. (모델·홈 화면은 패치로 정상 반영)
- [x] **#56 이미지 편집 포맷 썸네일/미리보기 (.clip/.psd/.jfif)** — `e4f5da6`, `b236121`
- [x] **폴란드어 로케일** — `327ce4f` (PR #54)
      - 키명 불일치 교정: Span `Settings_OnboardingReplayBtn` → LumiFiles `..._ReplayButton`
      - LumiFiles 고유 문자열 33개(타이틀바 캡션·정렬 메뉴·툴바·진행률 등)가 영어로
        폴백되던 것을 번역 추가 → **938개 키 전량 커버(누락 0)**
      - 현재 로케일: en, ko, ja, zh-CN, zh-TW, de, es, fr, pt, **pl**
- [x] **`Settings_OnboardingReplayBtn` 키 누락** — LumiFiles 는 `Settings_OnboardingReplayButton`
      이라는 다른 키명을 쓰고 있어 별도 추가 불필요(폴란드어 쪽 키명만 교정)

## P4 — 업스트림 미해결 (OPEN) — 직접 구현 완료 (2026-08-18)

포팅할 패치가 없어 **직접 구현**했다. 업스트림 이슈 스레드의 논의 결론을 설계 근거로 삼았다.

### [x] Span #55 기본 탐색기 설정 후 삭제 시 모든 폴더가 안 열림 ⚠️

MSIX 는 삭제 훅이 없어 `HKCU\...\shell\open\command` 가 사라진 실행 파일
(`%LOCALAPPDATA%\Microsoft\WindowsApps\LumiFiles.exe`)을 계속 가리킨다.
삭제 자체를 가로챌 방법이 없으므로 3중으로 대응:

1. **감지·복구** — 시작 시 세 키를 검사해 대상이 존재하지 않으면 정리.
   HKCU 라 UAC 없이 조용히 처리되고, 복구되면 토스트로 알린다.
   우리(또는 **Span 시절**) 실행 파일을 가리킬 때만 지운다 — 다른 파일 관리자의
   등록은 사용자가 일시적으로 앱을 지운 것일 수 있어 건드리지 않는다.
   → Span 을 기본 탐색기로 두고 삭제해 폴더가 안 열리게 된 사용자도 LumiFiles 설치만으로 복구된다.
2. **사전 경고** — 설정에서 켤 때 "삭제 전 반드시 해제" 다이얼로그. 취소하면 토글이 되돌아간다.
3. **복구 파일** — 켤 때 `문서\LumiFinder\restore-default-file-manager.reg` 를 남긴다.
   MSIX 의 LocalAppData 는 삭제 시 함께 지워지므로 **패키지 밖**에 둬야 의미가 있다.
   경로를 경고 다이얼로그에 함께 표시한다.

`DefaultFileManagerService` / `MainWindow.SplitPreviewManager`(시작 훅) /
`SettingsModeView`(경고) + 로컬라이제이션 5키. 경로 파싱 단위 테스트 11건.

### [x] Span #56 미리보기 확장 — .psd 코덱 의존 제거

제보자가 v1.6.5 이후에도 `.psd` 썸네일이 안 나온다고 했고, 원인은 **Windows 에 PSD
셸 코덱이 있어야** 하는 구조였다(최신 Photoshop 은 코덱을 설치해주지 않는다).

PSD 는 파일 안에 JPEG 썸네일을 갖고 있다 — Image Resources 섹션의 리소스 ID
**1036**(PS5+) / **1033**(PS4). 이를 직접 파싱해 코덱 없이 썸네일을 만든다.
추출 실패 시에는 기존 셸 경로로 폴백하므로 코덱이 있는 환경의 동작은 그대로다.

`LumiFiles.Thumbs/PsdThumbnailExtractor.cs` 신규.
`.clip` 과 디코드·축소·재인코딩 로직이 같아 `GenerateFromEmbeddedImageAsync` 로 공용화.
단위 테스트 15건 — 규격에 맞는 최소 PSD 를 만들어 검증(정상 추출, 이름 패딩 홀/짝,
앞선 리소스 건너뛰기, kRawRGB·SOI 없음·잘린 파일 등 폴백).

**남은 것**: `.ai`(Illustrator) 지원, CSP 5.x 로 저장한 `.clip`(원본 샘플 없이는 검증 불가),
`.psd` 미리보기 패널(현재는 썸네일만 — 미리보기는 인프로세스 코덱이라 크래시 리스크로 제외).

### [x] Span #58 Finder 스타일 컬러 태그 — 폴더 MVP

업스트림 스레드에서 제안자와 합의된 설계를 그대로 따랐다: **폴더 전용 + desktop.ini 저장 +
고정 팔레트**. 파일 태그는 저장 위치가 마땅치 않다(NTFS ADS 는 USB/exFAT/네트워크에서
사라지고, 경로 기반 DB 는 외부에서 이동/이름변경 시 고아가 된다). desktop.ini 는 폴더
**안**에 있어 탐색기로 옮겨도 따라가고, 평범한 파일이라 어떤 파일시스템에서도 살아남는다.

- `Models/FolderTagColor.cs` — 7색 고정 팔레트(빨강/주황/노랑/초록/파랑/보라/회색)
- `Services/FolderTagService.cs` — desktop.ini 읽기/쓰기, 캐시, 변경 이벤트
- 폴더 우클릭 → **태그** 서브메뉴(현재 태그 체크 표시, '태그 없음'으로 해제)
- Miller/Details/List/Icon 4개 뷰에 태그 점 표시

**업스트림이 착수 전에 확인하려던 두 가지에 대한 답**:
- *목록 성능*: Windows 는 폴더에 ReadOnly/System 속성이 있을 때만 desktop.ini 를 해석한다.
  태그를 남길 때 이 속성을 함께 표시하고, 목록 코드는 기존 `FolderItem.MaybeHasCustomIcon`
  게이트를 재사용한다 → **평범한 폴더는 desktop.ini 존재 확인조차 하지 않는다.**
  읽기는 백그라운드에서만 하고 렌더링 경로는 캐시만 조회한다.
- *쓰기 권한*: 읽기 전용 미디어·보호된 폴더는 `SetTag` 가 false 를 반환하고
  토스트로 명확히 알린다(조용히 실패하지 않는다).

단위 테스트 24건 — 특히 **사용자의 기존 desktop.ini 를 훼손하지 않는 것**을 집중 검증
(커스텀 아이콘 설정 보존, 태그만 제거, 설정→변경→해제 왕복 후 원본 복원, 숨김+시스템
속성 파일 재기록, 우리 태그만 있던 파일은 해제 시 삭제).

**남은 것**: 태그로 필터/검색, Details 뷰 태그 컬럼, 사이드바 즐겨찾기 태그 표시.
(모두 이슈의 "Suggested UX" 중 선택 항목이며 저장 계층은 이미 준비돼 있다)

## 포팅 체크리스트

Span 커밋을 포팅한 뒤 아래 3가지를 순서대로 확인한다.

```bash
# 1. 빌드
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -c Debug -p:Platform=x64

# 2. 회귀 테스트 (809개)
dotnet test src/LumiFiles/LumiFiles.Tests/LumiFiles.Tests.csproj -c Debug -p:Platform=x64

# 3. 로컬라이제이션 정합성
python tools/check-localization.py
```

추가로 매번 확인할 것:

- **브랜딩 잔재**: 패치가 `namespace Span.*` / `using Span.*` / `Span.ViewModels.*` 를
  끌고 들어온다. 아래로 훑고 발견되면 `LumiFiles.*` 로 교정한다.
  ```bash
  grep -rnE "(^|[^A-Za-z0-9_])Span\." src/LumiFiles/LumiFiles --include=*.cs --include=*.xaml --exclude-dir=obj --exclude-dir=bin
  ```
  - `Span.Helpers` 만 확인하면 놓친다 — 실제로 `Span.ViewModels.*` 가 새어 들어와
    빌드가 22개 오류로 깨진 적이 있다. 위 패턴처럼 `Span.` 전체를 훑을 것.
  - 앞의 `(^|[^A-Za-z0-9_])` 가 없으면 `TimeSpan.` 이 57건 걸려 진짜 잔재가 파묻힌다.
  - 출력이 없으면 정상.
- **보이는 UI 에 반영됐는지**: Span 패치의 XAML 은 대부분 *구 사이드바 / LegacyUnifiedBar* 를
  대상으로 하는데 LumiFiles 는 이 영역이 **collapsed** 다. 패치가 붙어도 화면에는 아무 변화가
  없을 수 있으므로, 실제로 보이는 `LumiSidebar`(MainWindow.xaml 106~460) / `LumiToolbar`(479~) /
  하단 path bar(2310~) 중 어디에 연결해야 하는지 확인한다.
  (실제 사례: #62(4) 사이드바 피드백, #39a 커스텀 아이콘 — 둘 다 수동 재연결 필요했음)
- **미번역 동반 유입**: 업스트림이 한국어만 번역하고 배포한 항목이 있다
  (#43/#45 가 7개 언어 미번역 상태로 들어왔음). 위 3번 스크립트가 이를 잡는다.

## 회귀 안전망 (복구 완료)

- [x] **테스트 프로젝트 복구** — **809 통과 / 0 실패 / 1 건너뜀 (2026-08-18)**
  - `FontScaleService` 의 WinUI 의존(`Thickness`)을 `FontScaleService.Sidebar.cs` partial 로 분리,
    `Application.Current` 해석은 *구현 없는 partial method 는 호출이 소거된다*는 C# 규칙을 이용한
    `TryResolveXamlInstance(ref ...)` 훅으로 전환 (런타임 동작 불변)
  - `SettingsServiceStub` 에 `FolderCustomIconsEnabled`/`AnimationsEnabled` 구현 추가
  - csproj 에 `AppDataPaths.cs`, `ShellDeleteWithProgress.cs` 등록
  - 낡은 기대값 2건 갱신(로그 prefix 11→16자, PropertyChanged 8→12개)

## LumiFinder 자체 이슈 (별도 트랙)

- [x] **#1-1 주소창 직접 편집** — 하단 path bar 연필 버튼(토글) + 경로 입력/이동
- [x] **#1-2 검색 UI** — 창 크기 반응형 + 검색 Enter 를 실제 파일 재귀검색에 연결
      (기존엔 명령 팔레트가 열렸고, 팔레트가 사이드바 컬럼에 갇혀 좌측에 표시되던 문제도 수정)
- [ ] **#1-3 Miller 컬럼 긴 경로 압축/축약 옵션**
