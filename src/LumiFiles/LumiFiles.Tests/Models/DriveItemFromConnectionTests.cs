namespace LumiFiles.Tests.Models;

[TestClass]
public class DriveItemFromConnectionTests
{
    [TestInitialize]
    public void Setup()
    {
        // IconService.Current is null → FromConnection will use fallback glyphs
        LumiFiles.Services.IconService.Current = null;
    }

    // ── SMB protocol ────────────────────────────────────

    [TestMethod]
    public void FromConnection_Smb_UsesUncPathAsPath()
    {
        var conn = new LumiFiles.Models.ConnectionInfo
        {
            DisplayName = "NAS Share",
            Protocol = LumiFiles.Models.RemoteProtocol.SMB,
            UncPath = @"\\nas\media"
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.AreEqual(@"\\nas\media", drive.Path);
    }

    [TestMethod]
    public void FromConnection_Smb_SetsDisplayName()
    {
        var conn = new LumiFiles.Models.ConnectionInfo
        {
            DisplayName = "Office NAS",
            Protocol = LumiFiles.Models.RemoteProtocol.SMB,
            UncPath = @"\\server\docs"
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.AreEqual("Office NAS", drive.Name);
    }

    [TestMethod]
    public void FromConnection_Smb_NetworkGlyphFallback()
    {
        var conn = new LumiFiles.Models.ConnectionInfo
        {
            Protocol = LumiFiles.Models.RemoteProtocol.SMB,
            UncPath = @"\\srv\share"
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        // IconService.Current is null → fallback \uEDCF (ri-global-line)
        Assert.AreEqual("\uEDCF", drive.IconGlyph);
    }

    [TestMethod]
    public void FromConnection_Smb_NullUncPath_EmptyPath()
    {
        var conn = new LumiFiles.Models.ConnectionInfo
        {
            Protocol = LumiFiles.Models.RemoteProtocol.SMB,
            UncPath = null
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.AreEqual(string.Empty, drive.Path);
    }

    // ── Non-SMB protocols ───────────────────────────────

    [TestMethod]
    public void FromConnection_Sftp_UsesUriAsPath()
    {
        var conn = new LumiFiles.Models.ConnectionInfo
        {
            DisplayName = "Dev Server",
            Protocol = LumiFiles.Models.RemoteProtocol.SFTP,
            Username = "deploy",
            Host = "dev.example.com",
            Port = 22,
            RemotePath = "/var/www"
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.AreEqual("sftp://deploy@dev.example.com:22/var/www", drive.Path);
    }

    [TestMethod]
    public void FromConnection_Ftp_ServerGlyphFallback()
    {
        var conn = new LumiFiles.Models.ConnectionInfo
        {
            Protocol = LumiFiles.Models.RemoteProtocol.FTP,
            Host = "ftp.example.com"
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        // IconService.Current is null → fallback \uF0DF (ri-server-fill)
        Assert.AreEqual("\uF0DF", drive.IconGlyph);
    }

    [TestMethod]
    public void FromConnection_Ftps_ServerGlyphFallback()
    {
        var conn = new LumiFiles.Models.ConnectionInfo
        {
            Protocol = LumiFiles.Models.RemoteProtocol.FTPS,
            Host = "secure.example.com"
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.AreEqual("\uF0DF", drive.IconGlyph);
    }

    // ── Common fields ───────────────────────────────────

    [TestMethod]
    public void FromConnection_AlwaysSetsIsRemoteConnection()
    {
        var conn = new LumiFiles.Models.ConnectionInfo
        {
            Protocol = LumiFiles.Models.RemoteProtocol.SFTP
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.IsTrue(drive.IsRemoteConnection);
    }

    [TestMethod]
    public void FromConnection_SetsConnectionId()
    {
        var conn = new LumiFiles.Models.ConnectionInfo();
        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.AreEqual(conn.Id, drive.ConnectionId);
    }

    [TestMethod]
    public void FromConnection_IsNetworkDrive_TrueWhenRemote()
    {
        var conn = new LumiFiles.Models.ConnectionInfo
        {
            Protocol = LumiFiles.Models.RemoteProtocol.SFTP
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.IsTrue(drive.IsNetworkDrive);
    }

    // ── IconService.Current set ─────────────────────────

    [TestMethod]
    public void FromConnection_Smb_WithIconService_UsesNetworkGlyph()
    {
        LumiFiles.Services.IconService.Current = new LumiFiles.Services.IconService();

        var conn = new LumiFiles.Models.ConnectionInfo
        {
            Protocol = LumiFiles.Models.RemoteProtocol.SMB,
            UncPath = @"\\srv\data"
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.AreEqual("\uEDD4", drive.IconGlyph); // stub NetworkGlyph
    }

    [TestMethod]
    public void FromConnection_NonSmb_WithIconService_UsesServerGlyph()
    {
        LumiFiles.Services.IconService.Current = new LumiFiles.Services.IconService();

        var conn = new LumiFiles.Models.ConnectionInfo
        {
            Protocol = LumiFiles.Models.RemoteProtocol.SFTP,
            Host = "host"
        };

        var drive = LumiFiles.Models.DriveItem.FromConnection(conn);

        Assert.AreEqual("\uEE71", drive.IconGlyph); // stub ServerGlyph
    }

    [TestCleanup]
    public void Cleanup()
    {
        LumiFiles.Services.IconService.Current = null;
    }
}
