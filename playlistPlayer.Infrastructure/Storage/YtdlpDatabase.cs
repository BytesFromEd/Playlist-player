namespace playlistPlayer.Infrastructure.Storage;

using System.Data;
using System.Data.SQLite;

public class YtdlpDatabase : Database
{
    public static void CreateTable()
    {
        try
        {
            using var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS `yt-dlp` (`version` TEXT not null, `filename` TEXT not null);", DbConnection());
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

            using var cmd = new SQLiteCommand("INSERT INTO `yt-dlp`(version, filename) VALUES('xx', 'xx')", DbConnection());
            cmd.ExecuteNonQuery();
        }
    }

    public static string GetVersion()
    {
        {
            try
            {
                using var cmd = new SQLiteCommand("SELECT VERSION FROM `yt-dlp` LIMIT 1", DbConnection());
                using SQLiteDataReader reader = cmd.ExecuteReader();

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
                using var cmd = new SQLiteCommand("SELECT FILENAME FROM `yt-dlp` LIMIT 1", DbConnection());
                using SQLiteDataReader reader = cmd.ExecuteReader();

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

                using var cmd = new SQLiteCommand("UPDATE `yt-dlp` SET VERSION = @version", DbConnection());
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
}