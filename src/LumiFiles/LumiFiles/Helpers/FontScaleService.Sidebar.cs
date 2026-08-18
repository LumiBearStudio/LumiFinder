using System;
using Microsoft.UI.Xaml;

namespace LumiFiles.Helpers
{
    /// <summary>
    /// FontScaleService 중 WinUI 타입(Thickness)에 의존하는 파생 속성만 분리한 partial.
    ///
    /// LumiFiles.Tests 는 WinUI 를 참조하지 않고 소스를 직접 컴파일하는 구조라
    /// (LumiFiles.Tests.csproj 의 Compile Include 목록 참고) Thickness 를 해석하지 못한다.
    /// 본체(FontScaleService.cs)는 순수 C# 로 유지해 테스트에서 컴파일되게 하고,
    /// XAML 바인딩 전용인 아래 속성만 이 파일에 둔다 — 테스트 프로젝트는 이 파일을 포함하지 않는다.
    ///
    /// 변경 시 주의: 본체 Density setter 가 두 속성명을 문자열 리터럴로 Raise 하므로
    /// 속성명을 바꾸면 그쪽도 함께 수정해야 한다.
    /// </summary>
    public sealed partial class FontScaleService
    {
        /// <summary>
        /// 런타임 전용: App.xaml 의 &lt;helpers:FontScaleService x:Key="FontScale"/&gt; 로
        /// XAML 파서가 만든 인스턴스를 Application.Resources 에서 찾아 돌려준다.
        /// 실패하면 result 를 건드리지 않아 호출측이 fallback 싱글톤을 쓰게 한다.
        /// </summary>
        static partial void TryResolveXamlInstance(ref FontScaleService? result)
        {
            try
            {
                if (Application.Current is { } app
                    && app.Resources.TryGetValue("FontScale", out var val)
                    && val is FontScaleService svc)
                {
                    result = svc;
                }
            }
            catch
            {
                // Application.Current.Resources 접근 실패 → 호출측 fallback 사용
            }
        }

        /// <summary>
        /// LumiSidebar 항목 Grid Padding. baseline (10,5) at density=3.
        /// vertical = 2 + density, horizontal = 10 고정.
        /// 0→(10,2) / 1→(10,3) / 2→(10,4) / 3→(10,5) / 4→(10,6) / 5→(10,7)
        /// </summary>
        public Thickness SidebarItemPadding => new Thickness(10, 2 + _density, 10, 2 + _density);

        /// <summary>
        /// LumiSidebar 섹션 헤더 Grid Padding. baseline (4,10,4,3).
        /// 헤더는 항목 간격에 비해 약하게만 영향 — vertical top 만 density-1 로 살짝 변동.
        /// </summary>
        public Thickness SidebarHeaderPadding => new Thickness(4, Math.Max(6, 7 + _density), 4, 3);
    }
}
