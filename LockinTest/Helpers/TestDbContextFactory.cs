
using Lock_In.Infrastructure.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LockinTest.Helpers;

public sealed class TestDbContextFactory :
    IDbContextFactory<Contexto>, IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<Contexto> options;

    private TestDbContextFactory(
        SqliteConnection connection,
        DbContextOptions<Contexto> options)
    {
        this.connection = connection;
        this.options = options;
    }

    public static async Task<TestDbContextFactory> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<Contexto>()
            .UseSqlite(connection)
            .Options;

        try
        {
            await using var contexto = new Contexto(options);
            await contexto.Database.EnsureCreatedAsync();

            return new TestDbContextFactory(connection, options);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public Contexto CreateDbContext()
    {
        return new Contexto(options);
    }

    public Task<Contexto> CreateDbContextAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
    }
}
