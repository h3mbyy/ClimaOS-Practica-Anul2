using SQLite;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _database;

    private async Task InitAsync()
    {
        if (_database is not null)
            return;

        SQLitePCL.Batteries_V2.Init();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "ClimaOS.db3");
        _database = new SQLiteAsyncConnection(dbPath);
        
        // Creăm tabelele de care avem nevoie
        await _database.CreateTableAsync<FavoriteLocation>();
    }

    public async Task<List<FavoriteLocation>> GetFavoriteLocationsAsync()
    {
        await InitAsync();
        return await _database.Table<FavoriteLocation>().ToListAsync();
    }

    public async Task<int> AddFavoriteLocationAsync(FavoriteLocation location)
    {
        await InitAsync();
        return await _database.InsertAsync(location);
    }

    public async Task<int> RemoveFavoriteLocationAsync(FavoriteLocation location)
    {
        await InitAsync();
        return await _database.DeleteAsync(location);
    }
}