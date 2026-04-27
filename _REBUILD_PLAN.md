# LumiFiles — Rebuild Plan

**전제**: `_MIGRATION_TODO.md` Step 1~3 완료 (Span fresh import + 리브랜딩 + 빌드 성공). 이 시점에는 아직 SPAN UI 모양.

**원칙**: 토큰 표(`_DESIGN_TOKENS.md`) 기준으로만 작업. 추측 금지. 단계마다 빌드 + 스크린샷 + 사용자 OK.

**참고 자료**:
- `_ui_mockup_v3_glass.html` — 토큰/DOM 정밀 spec
- `_DESIGN_TOKENS.md` — 토큰 추출표 (이 문서가 일차 참조)
- `D:\11.AI\LumiFiles\V3.png` — mockup 렌더링 캡처 (HTML 분량 때문에 시각 비교용)

---

## Stage 0 — 사용자 사전 결정 (코드 시작 전)

- [ ] Inter 폰트 번들 vs Segoe UI Variable 사용 (Inter 권장 — mockup 일치)
- [ ] WinUI 3 SystemBackdrop 한계로 vivid wallpaper는 윈도우 안쪽 레이어로 합성 — OK?
- [ ] 캡션 버튼은 WinUI 표준 `TitleBar` 대신 커스텀 42x30 — OK?

---

## Stage 1 — 디자인 토큰 ResourceDictionary

**파일**: `App.xaml`, `Themes/LumiTheme.xaml` (신규)

- [ ] `_DESIGN_TOKENS.md` 1~3장의 모든 색상/지오메트리 값을 `<Color>`, `<SolidColorBrush>`, `<LinearGradientBrush>`, `<x:Double>` 리소스로 정의
- [ ] 카테고리 그라디언트 8종 (amber/coral/gold/green/blue/purple/pink/gray)
- [ ] 글래스 레이어 4종 (window/sidebar/content/pill)

**검증**: 빌드 성공 + 디자이너 미리보기에서 리소스 보임. UI 변화는 아직 없음.

---

## Stage 2 — 윈도우 외형 (24px round + wallpaper)

**파일**: `MainWindow.xaml`, `MainWindow.xaml.cs`

- [ ] 윈도우 코너 24px (Win11 `WindowCornerPreference` + 내부 `Border` CornerRadius 24)
- [ ] 외부 padding `6px 0 6px 6px` (sidebar floating용 여백)
- [ ] Wallpaper 합성: vivid purple radial gradient (mockup 4장의 radial + base linear)
  - 윈도우 내부 최하위 레이어 `<Grid>`에 `LinearGradientBrush` + 위에 `RadialGradientBrush` 5겹
  - WinUI 3는 RadialGradientBrush 지원 (Microsoft.UI.Xaml.Media)
- [ ] 윈도우 그림자 (mockup spec 그대로) — DropShadowPanel 또는 Composition shadow

**검증 스크린샷**: 윈도우 둥근 모서리 + purple wallpaper 외곽 노출. 내부는 아직 SPAN UI 그대로 OK.

---

## Stage 3 — Floating Glass Sidebar

**파일**: `Views/Sidebar.xaml` (또는 기존 사이드바 View)

- [ ] 사이드바 컨테이너: 228px width, 18px CornerRadius (4면), 위치 `Margin="0,0,6,0"` (content 와 6px 간격)
- [ ] 배경: gradient `rgba(30,28,40,0.72) → rgba(22,20,32,0.78)` + Backdrop blur (Mica/Acrylic 또는 Composition Backdrop)
- [ ] 4면 inset highlight + 외곽 그림자 (mockup spec)
- [ ] 상/하 light streak (sidebar 위/아래 1px gradient line, 12px 좌우 마진)

**검증 스크린샷**: 사이드바가 콘텐츠 위로 떠 있고, 4면 둥글고, blur로 wallpaper 색이 살짝 비치는지.

---

## Stage 4 — Sidebar 내용 (Favorites/Locations/Tags)

- [ ] 사이드바 top: 14x14 amber gradient logo + "Lumi Files" 11px secondary
- [ ] sb-item 스타일 (padding 5px 10px, radius 8px, hover/active)
- [ ] **stroke 아이콘** (Phosphor 또는 Tabler — 1.5px stroke, 16x16 SVG/Path)
  - 현재 프로젝트의 `icons-phosphor.json`/`icons-tabler.json` 활용 가능
- [ ] **colored container 아이콘** (Pictures/Music/OneDrive — 20x20 radius 5.5px gradient + inset highlight + drop shadow)
- [ ] **tag-dot** (10x10 circle)
- [ ] 영어 텍스트 (Recent/Desktop/Documents/Downloads/Projects/Pictures/Music/Local Disk (C:)/Data (D:)/OneDrive/Important/Work/Personal/Urgent)
- [ ] 활성 항목 amber gradient 배경

**검증 스크린샷**: 목업 사이드바와 1:1 비교. 아이콘 크기/색/배경 정확히 일치.

---

## Stage 5 — 타이틀바 + 탭

**파일**: `MainWindow.xaml` (titlebar 영역)

- [ ] WinUI 3 `Window.SetTitleBar` 또는 `AppTitleBar` 영역 36px
- [ ] 탭 컨트롤: 직접 만든 ItemsControl + DataTemplate
  - tab radius 8px, 12px font, max 170/min 100
  - 컬러 닷 8x8
  - active gradient + inset border
  - close × hover만 보임
- [ ] tab-add 24x24
- [ ] 캡션 42x30 (− ☐ ×, close hover #E81123)
  - WinUI 3 표준 캡션 사용 시 색만 매칭하거나, drag region만 잡고 직접 그림

**검증 스크린샷**: 탭 3개 + 컬러 닷, 활성 탭 amber 글로우. 캡션 버튼 사이즈 정확.

---

## Stage 6 — 툴바 (Pill 그룹)

**파일**: `MainWindow.xaml` (toolbar 영역, 44px)

- [ ] Back/Forward pill (28px button, divider, disabled state)
- [ ] addr-folder (folder-icon amber + "LumiFiles" 13px weight 500)
- [ ] spacer
- [ ] View pill (Icons / List / **Columns active** / Gallery)
- [ ] Sort pill (↕ Name)
- [ ] Tab-add / Share / Download / More 단일 버튼 pill들
- [ ] Search compact 32x28

**기존 SPAN 툴바 완전 폐기** — 큰 주소바 + 우측 검색 구조는 사라짐. `MainWindow.xaml`의 toolbar 영역은 거의 새로 작성.

**검증 스크린샷**: pill 그룹들 간격 + 활성 view 버튼 amber + addr-folder 아이콘 amber.

---

## Stage 7 — 미러 컬럼

**파일**: `Views/ColumnView.xaml` (또는 해당 View)

- [ ] col-header (이름 + 카운트, 11px tertiary, padding 8px 14px 6px)
- [ ] file-row (padding 4px 10px, radius 7px, gap 8px, 13px)
- [ ] hover/selected/focused 상태 (mockup gradient 스펙 그대로)
- [ ] 폴더 아이콘 #6AA8E8 (Tahoe 표준 블루) + 카테고리 변형
- [ ] 확장자 ext 11.5px tertiary
- [ ] chev 10px tertiary

**검증 스크린샷**: 컬럼 4개 가로 스크롤 + 헤더 + 선택 amber gradient.

---

## Stage 8 — Path bar

- [ ] 30px height, bg rgba(0,0,0,0.20)
- [ ] 세그먼트 (radius 5px, hover bg, current weight 500)
- [ ] 우측 status (10.5px tertiary, border-left)

---

## Stage 9 — 폰트 적용

- [ ] Inter 폰트 파일 Assets에 추가 (또는 Stage 0 결정에 따라)
- [ ] App.xaml `FontFamily` 글로벌 적용

---

## Stage 10 — 정리

- [ ] 사용하지 않는 SPAN-specific 리소스/스타일 제거
- [ ] `bkit-templates` 또는 `code-review` 스킬로 코드 리뷰
- [ ] 최종 스크린샷 vs 목업 1:1 비교 (`_DESIGN_TOKENS.md` 14장 체크리스트)
- [ ] 사용자 최종 OK

---

## 단계별 완료 기준 (Definition of Done)

각 Stage는 다음 셋이 모두 OK 일 때만 다음 단계로:
1. **빌드 성공** (에러 0)
2. **스크린샷** (해당 영역 캡처)
3. **사용자 시각 검증** ("OK" 또는 "수정")

수정이 필요한 경우 **그 단계 안에서 fix** — 다음 단계로 누적되지 않게.

---

## 비실패 가드

- 매 Stage 시작 시 `_DESIGN_TOKENS.md` 해당 섹션 다시 읽기
- "비슷하게" 금지. 값이 mockup과 일치하지 않으면 작업 중단하고 사용자에게 보고
- 토큰 표에 없는 값은 임의로 채우지 않음 — 사용자에게 확인
