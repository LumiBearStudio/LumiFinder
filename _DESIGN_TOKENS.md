# LumiFiles — Design Tokens (from `_ui_mockup_v3_glass.html`)

**목적**: 코드 작업 시작 전 사용자 검증용. 추측 금지 — 모든 값은 mockup HTML에서 추출.
**적용 대상**: WinUI 3 XAML (App.xaml ResourceDictionary, MainWindow.xaml, 컴포넌트 스타일)

**참고 자료 (Source of Truth)**:
- 토큰/구조 정밀: `_ui_mockup_v3_glass.html` (1014줄, CSS 변수 + DOM 구조)
- 시각 레이아웃: `D:\11.AI\LumiFiles\V3.png` (mockup 렌더링 캡처 — HTML 분량이 커서 한눈에 보기 어려울 때 사용)
- 사용자 캡처 1: 시작점 (현재 SPAN UI)
- 사용자 캡처 2: 도착점 (V3 mockup 렌더링)

---

## 1. 색상 (Color)

### Lumi 액센트
| Token | Value | 용도 |
|---|---|---|
| `--lumi-amber` | `#FFB86B` | 메인 액센트 (선택, 활성 탭, 폴더 아이콘) |
| `--lumi-amber-soft` | `#FFD27A` | 활성 텍스트 |
| `--lumi-amber-deep` | `#D48028` | 그라디언트 끝 |

### 다크 표면
| Token | Value |
|---|---|
| `--bg-0` | `#0A0A0C` |
| `--bg-1` | `#16161A` |
| `--bg-2` | `#1E1E22` |
| `--bg-3` | `#26262B` |

### 텍스트
| Token | Value |
|---|---|
| `--text-primary` | `#EEEEF2` |
| `--text-secondary` | `#92929A` |
| `--text-tertiary` | `#60606A` |

### 카테고리 아이콘 그라디언트 (135deg)
| Class | Stops |
|---|---|
| `amber` | `#FFB86B → #D48028` |
| `coral` | `#FF9A6B → #E85A3C` |
| `gold` | `#F0CE5A → #C29A28` |
| `green` | `#7EC785 → #4BA35E` |
| `blue` | `#6AA8E8 → #3E7FC2` |
| `purple` | `#B08FE8 → #7A52C2` |
| `pink` | `#F08FBF → #C25A8F` |
| `gray` | `#7A7368 → #504A40` |

---

## 2. 글래스 / 레이어

| 영역 | 배경 | Backdrop filter |
|---|---|---|
| **Window frame** | `linear-gradient(180deg, rgba(18,18,24,0.88), rgba(12,12,16,0.94))` | `blur(60px) saturate(1.4)` |
| **Sidebar** (Layer 2, floating) | `linear-gradient(180deg, rgba(30,28,40,0.72), rgba(22,20,32,0.78))` | `blur(80px) saturate(2.0) brightness(1.05)` |
| **Content** (Layer 1, flush) | `rgba(0,0,0,0.12)` overlay | — |
| **Pill** (toolbar) | `rgba(255,255,255,0.025)` / hover `rgba(255,255,255,0.055)` | — |

### 윈도우 그림자
```
0 40px 100px rgba(0,0,0,0.70),
0 16px 40px rgba(0,0,0,0.45),
0 0 0 0.5px rgba(255,255,255,0.08)
```

### 사이드바 엣지 (모든 4면 inset highlight)
```
inset 0 0.5px 0 rgba(255,255,255,0.18),    /* 위 */
inset 0 -0.5px 0 rgba(255,255,255,0.04),   /* 아래 */
inset 0.5px 0 0 rgba(255,255,255,0.06),    /* 왼 */
inset -0.5px 0 0 rgba(255,255,255,0.06),   /* 오 */
0 0 0 0.5px rgba(255,255,255,0.08),
0 2px 12px rgba(0,0,0,0.25)
```

### 사이드바 상/하 light streak (`::before`/`::after`)
```
top:    1px / linear-gradient(90deg, transparent, rgba(255,255,255,0.22) 30%-70%, transparent)
bottom: 1px / linear-gradient(90deg, transparent, rgba(255,255,255,0.06) 30%-70%, transparent)
양쪽 12px 마진
```

---

## 3. 지오메트리 (Geometry)

| 영역 | 값 |
|---|---|
| Window 코너 | **24px** |
| Sidebar 코너 | **18px** (4면 모두) |
| Content 코너 | **0px** (window border-radius + overflow:hidden 으로 자동 클립) |
| Window padding | `6px 0 6px 6px` (오른쪽은 0 — content가 윈도우 엣지에 flush) |
| Window 내부 gap | `6px` (sidebar ↔ content) |
| Sidebar width | `228px` |
| Content grid rows | `36px (titlebar) / 44px (toolbar) / 1fr (columns) / 30px (pathbar)` |
| Column width | `260px` |
| Preview column width | `300px` |

### 내부 패널 radius
- `--panel-radius-inner`: `16px`

---

## 4. 배경 (Wallpaper) — 윈도우 외부

```css
background:
  radial-gradient(ellipse at 15% 20%, rgba(100, 60, 220, 0.65), transparent 50%),
  radial-gradient(ellipse at 70% 15%, rgba(200, 60, 140, 0.50), transparent 50%),
  radial-gradient(ellipse at 45% 85%, rgba( 60, 40, 160, 0.60), transparent 55%),
  radial-gradient(ellipse at 85% 65%, rgba( 40,120, 200, 0.40), transparent 50%),
  radial-gradient(ellipse at 30% 60%, rgba( 80, 50, 180, 0.35), transparent 55%),
  linear-gradient(160deg, #1A0E30 0%, #0C0818 100%);
```

**WinUI 3 적용**: 윈도우 자체의 backdrop은 SystemBackdrop으로는 부족. Mica/Acrylic 표준 brush로는 이 vivid 톤이 안 나옴 — 윈도우 안에 레이어 2개를 두거나, BackgroundImage Brush를 LinearGradient + RadialGradientBrush로 합성.

---

## 5. 폰트 (Typography)

| Token | Value |
|---|---|
| Family | `Inter, Pretendard, -apple-system, "Segoe UI", system-ui, sans-serif` |
| Base size | `13px` |
| Sidebar item | `13px` / 활성 weight `500` |
| Sidebar header | `10.5px` / weight `500` / opacity `0.8` |
| Tab | `12px` |
| Toolbar pill | `13px` |
| File row | `13px` |
| Path bar | `11px` |
| File extension | `11.5px` (tertiary) |
| Preview title | `14px` weight `600` |
| Preview row | `12px` |

**WinUI 3 적용**: `Inter` 폰트 파일을 Assets에 포함하거나 Windows 11 기본 `Segoe UI Variable` fallback. 사용자 결정 필요 — Inter 번들 vs Segoe UI Variable.

---

## 6. 컴포넌트 — 사이드바 아이템

```
sb-item:
  padding: 5px 10px
  gap: 9px (icon ↔ text)
  radius: 8px
  font: 13px / weight 400 (active 500)
  hover bg: rgba(255,255,255,0.04)
  active bg: linear-gradient(90deg, rgba(255,184,107,0.28), rgba(255,184,107,0.15))
  active icon color: #FFF3DC

sb-ico-stroke (line icon, default):
  20x20, font 15px, color secondary, opacity 0.85
  Phosphor/Tabler 1.5px stroke

sb-ico (colored container):
  20x20, radius 5.5px
  gradient bg (위 카테고리 표 참조)
  inset 0 1px 0 rgba(255,255,255,0.22), 0 1px 2px rgba(0,0,0,0.28)

tag-dot:
  10x10 원, margin 0 4px 0 5px
```

---

## 7. 컴포넌트 — 탭

```
tab:
  padding: 4px 11px 5px
  radius: 8px
  font: 12px
  max-width: 170px, min-width: 100px
  color: tertiary (idle), secondary (hover), primary (active)

tab.active:
  bg: linear-gradient(180deg, rgba(255,184,107,0.16), rgba(255,184,107,0.06))
  shadow: inset 0 0 0 1px rgba(255,184,107,0.12)

tab-icon:
  8x8 원, opacity 0.8

tab-close:
  14x14, opacity 0 (hover/active 0.7)
  hover bg rgba(255,255,255,0.1)

tab-add:
  24x24, radius 6px
```

---

## 8. 컴포넌트 — 캡션 버튼

```
button:
  42x30, radius 6px
  hover bg: rgba(255,255,255,0.07)
button.close:
  hover bg: #E81123 / color white
```

---

## 9. 컴포넌트 — 툴바 pill

```
pill:
  padding: 2px
  radius: 999px (capsule)
  bg: rgba(255,255,255,0.025)

pill > button:
  height: 28px
  padding: 0 9px
  min-width: 32px
  radius: 999px
  hover bg: rgba(255,255,255,0.055)
  active: linear-gradient(180deg, rgba(255,184,107,0.22), rgba(255,184,107,0.12))
         + color amber-soft

pill-divider:
  width: 1px / height: 14px / bg rgba(255,255,255,0.08)

addr-folder:
  height: 28px / padding 0 12px 0 10px
  font: 13px weight 500
  folder-icon: amber color

search-compact:
  32x28, radius 999px
  bg: pill / hover pill-hover
```

---

## 10. 컴포넌트 — 미러 컬럼

```
column:
  width: 260px
  border-right: 1px rgba(255,255,255,0.035)

col-header:
  padding: 8px 14px 6px
  font: 11px / weight 500 / tertiary
  flex: space-between (이름 ↔ 카운트)

file-row:
  padding: 4px 10px
  radius: 7px
  gap: 8px
  font: 13px

file-row:hover bg: rgba(255,255,255,0.04)
file-row.selected:
  bg: linear-gradient(90deg, rgba(255,184,107,0.22), rgba(255,184,107,0.12))
  shadow: inset 0 0 0 1px rgba(255,184,107,0.22)

file-row.focused:
  bg: linear-gradient(90deg, rgba(255,184,107,0.22), rgba(255,184,107,0.12))

file-icon:
  18x18, font 14px
  ico-folder: #6AA8E8 (default blue, Tahoe 표준)
  ico-folder-amber/coral/gold/green/purple/pink: 카테고리 컬러

ext: 11.5px tertiary
chev: 10px tertiary opacity 0.6
```

---

## 11. 컴포넌트 — Path bar

```
pathbar:
  height: 30px
  padding: 0 14px
  bg: rgba(0,0,0,0.20)
  border-top: 1px rgba(255,255,255,0.035)
  font: 11px secondary

pb-seg:
  padding: 3px 6px
  radius: 5px
  hover: rgba(255,255,255,0.05)
  current: primary / weight 500

pb-sep:
  9px tertiary opacity 0.5

pb-status:
  font: 10.5px tertiary
  border-left: 1px divider
  padding-left: 12px
```

---

## 12. 사이드바 구조 (콘텐츠 — 영어, mockup 그대로)

### Top (drag region, 36px)
```
[14x14 amber gradient logo + glow]  Lumi Files
```

### Body
**Favorites**
- Recent (clock stroke)
- Desktop (monitor stroke)
- Documents (file stroke)
- Downloads (download stroke)
- **Projects (folder stroke) — active**
- Pictures (purple container icon)
- Music (pink container icon)

**Locations**
- Local Disk (C:) (drive stroke)
- Data (D:) (drive stroke)
- OneDrive (blue container icon)

**Tags**
- Important (gold dot #F0CE5A)
- Work (green dot #7EC785)
- Personal (blue dot #6AA8E8)
- Urgent (coral dot #FF7A5C)

### Bottom
- Settings (gear stroke)

---

## 13. 탭 구성 (mockup 예시)

| Tab | Color dot |
|---|---|
| Projects | `var(--cat-amber)` (gold) |
| **LumiFiles** (active) | `var(--cat-blue)` |
| Downloads | `var(--cat-green)` |

---

## 14. 검증 체크리스트 (각 단계 완료 시)

- [ ] 윈도우 24px 코너 보이는가? (외부 padding 으로 wallpaper 노출되는가?)
- [ ] 사이드바가 콘텐츠 위에 떠 있는가? (그림자/엣지 highlight 보이는가?)
- [ ] 사이드바 4면 모두 둥근가?
- [ ] amber 액센트가 시스템 블루 어디에도 안 남았는가?
- [ ] 폰트가 Inter (또는 합의된 fallback)인가?
- [ ] 캡션 버튼이 Windows 표준 시스템 버튼이 아닌 커스텀 42x30인가?
- [ ] 툴바가 큰 주소바가 아닌 컴팩트 pill 그룹인가?
- [ ] 미러 컬럼 헤더에 이름 + 카운트 보이는가?
