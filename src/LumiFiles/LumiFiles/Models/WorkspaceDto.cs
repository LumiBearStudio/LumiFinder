using System;
using System.Collections.Generic;

namespace LumiFiles.Models
{
    public record WorkspaceDto(
        string Id,
        string Name,
        List<TabStateDto> Tabs,
        int ActiveTabIndex,
        DateTime CreatedAt,
        DateTime LastUsedAt
    );
}
