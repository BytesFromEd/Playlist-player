namespace playlistPlayer.Infrastructure.Storage;

using System.Data.SQLite;

public abstract class Database
{
    const string outputPath = "./database.db";

    protected static SQLiteConnection DbConnection()
    {
        if (!File.Exists(outputPath))
        {
            SQLiteConnection.CreateFile(outputPath);
        }
        var conn = new SQLiteConnection($"Data Source={Path.Combine()}; Version=3;");
        conn.Open();
        return conn;
    }
}