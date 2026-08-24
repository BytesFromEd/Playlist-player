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
                "CREATE TABLE IF NOT EXISTS `yt-dlp` (`version` TEXT not null, `filename` TEXT not null);", conn);
        cmd.ExecuteNonQuery();

        try
        {
            GetVersion();
        }
        catch
        {
            using var conn2 = DbConnection();
            conn2.Open();
            using var cmd2 = new SQLiteCommand("INSERT INTO `yt-dlp`(version, filename) VALUES('xx', 'xx')", conn2);
            cmd2.ExecuteNonQuery();
        }
    }

    public static string GetVersion()
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("SELECT VERSION FROM `yt-dlp` LIMIT 1", conn);
        using var reader = cmd.ExecuteReader();

        reader.Read();
        return reader.GetString("VERSION");
    }

    public static string GetFilename()
    {
        {
            try
            {
                using var conn = DbConnection();
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT FILENAME FROM `yt-dlp` LIMIT 1", conn);
                using var reader = cmd.ExecuteReader();

                reader.Read();
                return reader.GetString("FILENAME");
            }
            catch
            {
                throw;
            }
        }
    }

    public static bool SetVersion(string version)
    {
        if (GetVersion() == version) return false;

        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("UPDATE `yt-dlp` SET VERSION = @version", conn);
        cmd.Parameters.AddWithValue("@version", version);
        cmd.ExecuteNonQuery();

        return true;
    }

    public static bool SetFilename(string filename)
    {
        if (GetFilename() == filename) return false;

        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("UPDATE `yt-dlp` SET filename = @filename", conn);
        cmd.Parameters.AddWithValue("@filename", filename);
        cmd.ExecuteNonQuery();

        return true;
    }
}