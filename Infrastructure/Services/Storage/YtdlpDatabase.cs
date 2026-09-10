using System.Data;
using System.Data.SQLite;

namespace Infrastructure.Services.Storage;

internal abstract class YtdlpDatabase : Database
{
    public static void CreateTable()
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd =
            new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS `yt-dlp` (`version` TEXT not null, `filename` TEXT not null);" +
                "CREATE TABLE IF NOT EXISTS `ffmpeg` (`version` TEXT not null);" +
                "CREATE TABLE IF NOT EXISTS `deno` (`version` TEXT not null);", conn);
        cmd.ExecuteNonQuery();

        var sql = "";

        try
        {
            GetVersionYtdlp();
        }
        catch
        {
            sql += "INSERT INTO `yt-dlp`(version, filename) VALUES('xx', 'xx');";
        }

        try
        {
            GetVersionFfmpeg();
        }
        catch
        {
            sql += "INSERT INTO `ffmpeg`(version) VALUES('xx');";
        }

        try
        {
            GetVersionDeno();
        }
        catch
        {
            sql += "INSERT INTO `deno`(version) VALUES('xx');";
        }

        using var conn2 = DbConnection();
        conn2.Open();
        using var cmd2 = new SQLiteCommand(sql, conn2);
        cmd2.ExecuteNonQuery();
    }

    #region yt-dlp

    public static string GetVersionYtdlp()
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("SELECT VERSION FROM `yt-dlp` LIMIT 1", conn);
        using var reader = cmd.ExecuteReader();

        reader.Read();
        return reader.GetString("VERSION");
    }

    public static string GetFilenameYtdlp()
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("SELECT FILENAME FROM `yt-dlp` LIMIT 1", conn);
        using var reader = cmd.ExecuteReader();

        reader.Read();
        return reader.GetString("FILENAME");
    }

    public static void SetVersionYtdlp(string version)
    {
        if (GetVersionYtdlp() == version) return;

        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("UPDATE `yt-dlp` SET VERSION = @version", conn);
        cmd.Parameters.AddWithValue("@version", version);
        cmd.ExecuteNonQuery();
    }

    public static void SetFilenameYtdlp(string filename)
    {
        if (GetFilenameYtdlp() == filename) return;

        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("UPDATE `yt-dlp` SET filename = @filename", conn);
        cmd.Parameters.AddWithValue("@filename", filename);
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region ffmpeg

    public static string GetVersionFfmpeg()
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("SELECT VERSION FROM `ffmpeg` LIMIT 1", conn);
        using var reader = cmd.ExecuteReader();

        reader.Read();
        return reader.GetString("VERSION");
    }

    public static void SetVersionFfmpeg(string version)
    {
        if (GetVersionFfmpeg() == version) return;

        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("UPDATE `ffmpeg` SET VERSION = @version", conn);
        cmd.Parameters.AddWithValue("@version", version);
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region deno

    public static string GetVersionDeno()
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("SELECT VERSION FROM `deno` LIMIT 1", conn);
        using var reader = cmd.ExecuteReader();

        reader.Read();
        return reader.GetString("VERSION");
    }

    public static void SetVersionDeno(string version)
    {
        if (GetVersionDeno() == version) return;

        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("UPDATE `deno` SET VERSION = @version", conn);
        cmd.Parameters.AddWithValue("@version", version);
        cmd.ExecuteNonQuery();
    }

    #endregion
}