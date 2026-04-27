namespace LumiFiles.Tests.Models;

[TestClass]
public class ViewModeTests
{
    [TestMethod]
    public void ViewMode_HasExpectedValues()
    {
        Assert.AreEqual(0, (int)LumiFiles.Models.ViewMode.MillerColumns);
        Assert.AreEqual(1, (int)LumiFiles.Models.ViewMode.Details);
        Assert.AreEqual(2, (int)LumiFiles.Models.ViewMode.IconSmall);
        Assert.AreEqual(3, (int)LumiFiles.Models.ViewMode.IconMedium);
        Assert.AreEqual(4, (int)LumiFiles.Models.ViewMode.IconLarge);
        Assert.AreEqual(5, (int)LumiFiles.Models.ViewMode.IconExtraLarge);
        Assert.AreEqual(6, (int)LumiFiles.Models.ViewMode.Home);
    }

    [TestMethod]
    public void PreviewType_HasExpectedValues()
    {
        Assert.AreEqual(0, (int)LumiFiles.Models.PreviewType.None);
        Assert.AreEqual(1, (int)LumiFiles.Models.PreviewType.Image);
        Assert.AreEqual(2, (int)LumiFiles.Models.PreviewType.Text);
        Assert.AreEqual(3, (int)LumiFiles.Models.PreviewType.Pdf);
        Assert.AreEqual(4, (int)LumiFiles.Models.PreviewType.Media);
        Assert.AreEqual(5, (int)LumiFiles.Models.PreviewType.Folder);
        Assert.AreEqual(6, (int)LumiFiles.Models.PreviewType.HexBinary);
        Assert.AreEqual(7, (int)LumiFiles.Models.PreviewType.Font);
        Assert.AreEqual(8, (int)LumiFiles.Models.PreviewType.Archive);
        Assert.AreEqual(9, (int)LumiFiles.Models.PreviewType.Markdown);
        Assert.AreEqual(10, (int)LumiFiles.Models.PreviewType.Csv);
        Assert.AreEqual(11, (int)LumiFiles.Models.PreviewType.Generic);
    }

    [TestMethod]
    public void ActivePane_HasExpectedValues()
    {
        Assert.AreEqual(0, (int)LumiFiles.Models.ActivePane.Left);
        Assert.AreEqual(1, (int)LumiFiles.Models.ActivePane.Right);
    }
}
