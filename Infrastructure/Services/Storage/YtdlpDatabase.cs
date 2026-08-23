using System.Data;
using System.Data.SQLite;

namespace Infrastructure.Services.Storage;

internal abstract class YtdlpDatabase : Database
{
    public static void CreateTable()
    {
        try
        {
            using var conn = DbConnection();
            conn.Open();
            using var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS `yt-dlp` (`version` TEXT not null, `filename` TEXT not null);", conn);
            cmd.ExecuteNonQuery();
        }
        catch
        {
            throw;
        }

        try
        {
            GetVersion();
        }
        catch
        {
            using var conn = DbConnection();
            conn.Open();
            using var cmd = new SQLiteCommand("INSERT INTO `yt-dlp`(version, filename) VALUES('xx', 'xx')", conn);
            cmd.ExecuteNonQuery();
        }
    }

    public static string GetVersion()
    {
        {
            try
            {
                using var conn = DbConnection();
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT VERSION FROM `yt-dlp` LIMIT 1", conn);
                using var reader = cmd.ExecuteReader();

                reader.Read();
                return reader.GetString("VERSION");
            }
            catch
            {
                throw;
            }
        }
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
        {
            try
            {
                try
                {
                    if (GetVersion() == version) return false;
                }
                catch { }

                using var conn = DbConnection();
                conn.Open();
                using var cmd = new SQLiteCommand("UPDATE `yt-dlp` SET VERSION = @version", conn);
                cmd.Parameters.AddWithValue("@version", version);
                cmd.ExecuteNonQuery();

                return true;
            }
            catch
            {
                throw;
            }
        }
    }

    public static bool SetFilename(string filename)
    {
        {
            try
            {
                try
                {
                    if (GetFilename() == filename) return false;
                }
                catch { }

                using var conn = DbConnection();
                conn.Open();
                using var cmd = new SQLiteCommand("UPDATE `yt-dlp` SET filename = @filename", conn);
                cmd.Parameters.AddWithValue("@filename", filename);
                cmd.ExecuteNonQuery();

                return true;
            }
            catch
            {
                throw;
            }
        }
    }
}