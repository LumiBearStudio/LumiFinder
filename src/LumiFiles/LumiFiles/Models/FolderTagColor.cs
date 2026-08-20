namespace LumiFiles.Models
{
    /// <summary>
    /// Span #58: Finder 스타일 폴더 컬러 태그.
    ///
    /// 업스트림 논의에서 정리된 설계를 따른다:
    ///  - <b>폴더 전용</b>. 파일 태그는 저장 위치가 마땅치 않다(NTFS ADS 는 USB/exFAT/네트워크에서
    ///    사라지고, 앱 DB 는 외부에서 이동/이름변경하면 고아가 된다). 폴더는 desktop.ini 를
    ///    폴더 안에 두므로 탐색기로 옮기거나 이름을 바꿔도 태그가 따라간다.
    ///  - <b>고정 팔레트</b>. 사용자가 이름을 붙이는 태그 관리 UI 없이 색 자체가 의미를 갖는다
    ///    (제보자도 macOS 에서 색을 프로젝트별 구분용으로만 썼다고 밝혔다).
    ///
    /// 열거값 이름은 그대로 desktop.ini 에 기록되므로 <b>변경하면 기존 태그가 유실된다</b>.
    /// </summary>
    public enum FolderTagColor
    {
        None = 0,
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Purple,
        Gray,
    }

    /// <summary>팔레트 색상값. UI 는 이 값을 브러시로 변환해 사용한다.</summary>
    public static class FolderTagPalette
    {
        /// <summary>사용자에게 제시하는 순서 (None 은 '태그 없음' = 해제).</summary>
        public static readonly FolderTagColor[] SelectableOrder =
        {
            FolderTagColor.Red,
            FolderTagColor.Orange,
            FolderTagColor.Yellow,
            FolderTagColor.Green,
            FolderTagColor.Blue,
            FolderTagColor.Purple,
            FolderTagColor.Gray,
        };

        /// <summary>
        /// 태그 색의 16진 표기(#AARRGGBB). 어두운/밝은 테마 양쪽에서 구분되도록
        /// 채도를 낮추지 않은 선명한 값을 쓴다.
        /// </summary>
        public static string ToHex(FolderTagColor color) => color switch
        {
            FolderTagColor.Red => "#FFE5484D",
            FolderTagColor.Orange => "#FFF76B15",
            FolderTagColor.Yellow => "#FFFFC53D",
            FolderTagColor.Green => "#FF30A46C",
            FolderTagColor.Blue => "#FF3E63DD",
            FolderTagColor.Purple => "#FF8E4EC6",
            FolderTagColor.Gray => "#FF8B8D98",
            _ => "#00000000",
        };

        /// <summary>로컬라이제이션 키 (Tag_Red 등). None 은 '태그 없음'.</summary>
        public static string ToLocalizationKey(FolderTagColor color) =>
            color == FolderTagColor.None ? "Tag_None" : $"Tag_{color}";
    }
}
