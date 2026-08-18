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

## P4 — 업스트림도 미해결 (OPEN) — 우리가 먼저 해도 됨

- [ ] **Span #55 기본 탐색기 설정 후 프로그램 삭제 시 링크 해제 안 되어 모든 폴더가 안 열림** ⚠️ 위험도 최상
- [ ] **Span #58 Finder 스타일 컬러 태그/라벨**
- [ ] **Span #56 미리보기 기능 확장 요청**

---

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
