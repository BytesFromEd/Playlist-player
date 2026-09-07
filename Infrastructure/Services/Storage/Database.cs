using System.Data.SQLite;
using Core.Models;
using Core.Models.Enums;

namespace Infrastructure.Services.Storage;

internal abstract class Database
{
    protected static SQLiteConnection DbConnection()
    {
        var outputPath = Path.Combine(AppSettings.GetInstance().AppFolder, "database.db");

        if (!File.Exists(outputPath))
        {
            SQLiteConnection.CreateFile(outputPath);
        }

        var conn = new SQLiteConnection($"Data Source={Path.GetFullPath(outputPath)}; Version=3;");
        return conn;
    }

    public static void CreateInitialTables()
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd =
            new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS Playlists (id TEXT PRIMARY KEY, name TEXT, owner TEXT, thumbnail TEXT, provider TEXT);",
                conn);
        cmd.ExecuteNonQuery();
        using var cmd2 =
            new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS Songs (id TEXT PRIMARY KEY, title TEXT, artist TEXT, file TEXT, cover TEXT, duration INT, lastPlayed INTEGER, provider TEXT);",
                conn);
        cmd2.ExecuteNonQuery();
        using var cmd3 =
            new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS Song_Playlist (song_id TEXT, playlist_id TEXT, PRIMARY KEY(song_id, playlist_id));",
                conn);
        cmd3.ExecuteNonQuery();
    }

    public static Song GetSong(string id)
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd =
            new SQLiteCommand(
                "SELECT id, title, artist, file, cover, duration, lastPlayed, provider FROM Songs WHERE id = @id",
                conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        reader.Read();

        var provider = (string)reader["provider"] switch
        {
            "yt" => Provider.Youtube,
            _ => Provider.External,
        };


        var song = new Song(id, (string)reader["title"], (string)reader["artist"],
            (string)reader["file"], (string)reader["cover"], (int)reader["duration"], (DateTime)reader["lastPlayed"],
            provider);

        return song;
    }

    private static List<Song> GetPlaylistSongs(string id)
    {
        List<Song> songs = [];

        using var conn = DbConnection();
        conn.Open();
        using var cmd =
            new SQLiteCommand(
                "SELECT id, title, artist, file, cover, duration, lastPlayed, provider FROM Songs s WHERE EXISTS( SELECT 1 FROM Song_Playlist WHERE playlist_id = @id AND song_id = s.id) ",
                conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var provider = (string)reader["provider"] switch
            {
                "yt" => Provider.Youtube,
                _ => Provider.External,
            };

            songs.Add(new Song((string)reader["id"], (string)reader["title"], (string)reader["artist"],
                (string)reader["file"], (string)reader["cover"], (int)reader["duration"],
                new DateTime((long)reader["lastPlayed"]), provider));
        }

        return songs;
    }

    public static bool PlaylistExists(string id)
    {
        return GetPlaylist(id) != null;
    }

    public static Playlist? GetPlaylist(string id)
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("SELECT id, name, owner, provider, thumbnail FROM Playlists WHERE id = @id",
            conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        var provider = (string)reader["provider"] switch
        {
            "yt" => Provider.Youtube,
            _ => Provider.External,
        };

        var songs = GetPlaylistSongs(id);

        var playlist = new Playlist((string)reader["name"], (string)reader["owner"], (string)reader["id"], provider,
            (string?)reader["thumbnail"], songs);

        return playlist;
    }

    public static List<Playlist> GetPlaylists()
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd = new SQLiteCommand("SELECT id, name, owner, provider, thumbnail FROM Playlists", conn);
        using var reader = cmd.ExecuteReader();
        var playlists = new List<Playlist>();
        while (reader.Read())
        {
            var provider = (string)reader["provider"] switch
            {
                "yt" => Provider.Youtube,
                _ => Provider.External,
            };

            playlists.Add(new Playlist((string)reader["name"], (string)reader["owner"], (string)reader["id"],
                provider, (string?)reader["thumbnail"]));
        }

        return playlists;
    }

    public static void AddSongs(string playlist, params IEnumerable<Song> songs)
    {
        using var conn = DbConnection();
        conn.Open();

        foreach (var song in songs)
        {
            var provider = song.GetProvider() switch
            {
                Provider.Youtube => "yt",
                _ => "ex",
            };

            using var cmd =
                new SQLiteCommand(
                    "INSERT OR IGNORE INTO Songs(id, title, artist, file, cover, duration, lastPlayed, provider) VALUES (@id, @title, @artist, @file, @cover, @duration, @lastPlayed, @provider)",
                    conn);

            cmd.Parameters.AddWithValue("@id", song.GetId());
            cmd.Parameters.AddWithValue("@title", song.GetTitle());
            cmd.Parameters.AddWithValue("@artist", song.GetArtist());
            cmd.Parameters.AddWithValue("@file", song.GetFile());
            cmd.Parameters.AddWithValue("@cover", song.GetCover());
            cmd.Parameters.AddWithValue("@duration", song.GetDuration());
            cmd.Parameters.AddWithValue("@lastPlayed", song.GetLastPlayed());
            cmd.Parameters.AddWithValue("@provider", provider);

            cmd.ExecuteNonQuery();

            using var cmd2 =
                new SQLiteCommand(
                    "INSERT OR IGNORE INTO Song_Playlist (song_id, playlist_id) VALUES (@song, @playlist)",
                    conn);
            cmd2.Parameters.AddWithValue("@song", song.GetId());
            cmd2.Parameters.AddWithValue("@playlist", playlist);
            cmd2.ExecuteNonQuery();
        }
    }


    public static void RemoveSongsLink(string? playlist, params List<Song> songs)
    {
        if (playlist == null && songs.Count == 0)
            throw new ArgumentException("When playlist is null songs cannot be empty");

        using var conn = DbConnection();
        conn.Open();

        var songIds = songs.Select(s => s.GetId()).ToList();
        var paramNames = songIds.Select((_, i) => $"@song{i}").ToList();

        /*
         * If playlist = null
         *      remove songs in songs
         * else if songs not empty
         *          remove all songs linked to playlist.id that aren't in songs
         *      else
         *          remove all songs linked to playlist.id
         */
        var sql = playlist == null
            ? $"DELETE FROM Song_Playlist WHERE song_id IN ({string.Join(",", paramNames)});"
            : (songs.Count > 0
                ? $"DELETE FROM Song_Playlist WHERE playlist_id = @playlist AND song_id NOT IN ({string.Join(",", paramNames)});"
                : $"DELETE FROM Song_Playlist WHERE playlist_id = @playlist");

        using var cmd = new SQLiteCommand(sql, conn);
        if (playlist != null)
            cmd.Parameters.AddWithValue("@playlist", playlist);
        for (var i = 0; i < songIds.Count; i++)
        {
            cmd.Parameters.AddWithValue(paramNames[i], songIds[i]);
        }

        cmd.ExecuteNonQuery();
    }

    public static void RemoveSongs(params List<Song> songs)
    {
        using var conn = DbConnection();
        conn.Open();

        var songIds = songs.Select(s => s.GetId()).ToList();
        var paramNames = songIds.Select((_, i) => $"@song{i}").ToList();

        var sql =
            $"DELETE FROM Songs WHERE id IN ({string.Join(",", paramNames)}) AND EXISTS(SELECT 1 FROM Song_Playlist GROUP BY song_id HAVING COUNT(*) = 0);";
        using var cmd = new SQLiteCommand(sql, conn);

        for (var i = 0; i < songIds.Count; i++)
        {
            cmd.Parameters.AddWithValue(paramNames[i], songIds[i]);
        }

        cmd.ExecuteNonQuery();

        RemoveSongsLink(null, songs);
    }

    public static void AddPlaylist(Playlist playlist)
    {
        var provider = playlist.GetProvider() switch
        {
            Provider.Youtube => "yt",
            _ => "ex",
        };

        using var conn = DbConnection();
        conn.Open();
        using var cmd =
            new SQLiteCommand(
                "INSERT OR IGNORE INTO Playlists(id, name, owner, provider, thumbnail) VALUES (@id, @name, @owner, @provider, @thumbnail)",
                conn);
        cmd.Parameters.AddWithValue("@id", playlist.GetId());
        cmd.Parameters.AddWithValue("@name", playlist.GetName());
        cmd.Parameters.AddWithValue("@owner", playlist.GetOwner());
        cmd.Parameters.AddWithValue("@provider", provider);
        cmd.Parameters.AddWithValue("@thumbnail", playlist.GetThumbanil());
        cmd.ExecuteNonQuery();

        AddSongs(playlist.GetId(), playlist.GetSongs());
    }

    public static void UpdatePlaylist(Playlist playlist)
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd =
            new SQLiteCommand(
                "Update Playlists SET name=@name, owner=@owner WHERE id=@id",
                conn);
        cmd.Parameters.AddWithValue("@id", playlist.GetId());
        cmd.Parameters.AddWithValue("@name", playlist.GetName());
        cmd.Parameters.AddWithValue("@owner", playlist.GetOwner());
        cmd.ExecuteNonQuery();

        AddSongs(playlist.GetId(), playlist.GetSongs());
        RemoveSongsLink(playlist.GetId(), playlist.GetSongs());
    }

    public static void RemovePlaylist(Playlist playlist, bool removeSongs)
    {
        using var conn = DbConnection();
        conn.Open();
        using var cmd =
            new SQLiteCommand(
                "DELETE FROM Playlists WHERE id=@id",
                conn);
        cmd.Parameters.AddWithValue("@id", playlist.GetId());
        cmd.ExecuteNonQuery();
        RemoveSongsLink(playlist.GetId(), playlist.GetSongs());
        if (removeSongs)
        {
            RemoveSongs(playlist.GetSongs());
        }
    }
}