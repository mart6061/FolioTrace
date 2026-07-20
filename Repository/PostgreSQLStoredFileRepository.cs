using System.Data;
using FolioTrace.Aggregates;
using Marten;
using Npgsql;
using NpgsqlTypes;

namespace Repository;

public sealed class PostgreSQLStoredFileRepository(
    NpgsqlDataSource dataSource,
    IDocumentStore documentStore) : IStoredFileRepository
{
    private const string TableName = "folio_stored_files";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                CREATE TABLE IF NOT EXISTS {TableName} (
                    id uuid PRIMARY KEY,
                    file_name text NOT NULL,
                    media_type text NOT NULL,
                    content_length bigint NOT NULL,
                    sha256 text NOT NULL,
                    content bytea NOT NULL,
                    created_at_utc timestamptz NOT NULL DEFAULT now()
                )
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await MigrateLegacyDocumentsAsync(cancellationToken);
    }

    public async Task<StoredFileMetadata?> GetMetadataAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT id, file_name, media_type, content_length, sha256 FROM {TableName} WHERE id = @id";
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return ReadMetadata(reader);

        return await GetLegacyMetadataAsync(id, cancellationToken);
    }

    public async Task<StoredFileRead?> OpenReadAsync(Guid id, long offset = 0, long? length = null, CancellationToken cancellationToken = default)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (length is <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = length.HasValue
            ? $"SELECT id, file_name, media_type, content_length, sha256, substring(content from @start for @length) FROM {TableName} WHERE id = @id"
            : $"SELECT id, file_name, media_type, content_length, sha256, content FROM {TableName} WHERE id = @id";
        command.Parameters.AddWithValue("id", id);
        if (length.HasValue)
        {
            command.Parameters.AddWithValue("start", offset + 1);
            command.Parameters.AddWithValue("length", length.Value);
        }

        var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var metadata = ReadMetadata(reader);
            var owner = new ReaderOwner(reader, command, connection);
            return new StoredFileRead(metadata, reader.GetStream(5), owner);
        }

        await reader.DisposeAsync();
        await command.DisposeAsync();
        await connection.DisposeAsync();
        return await OpenLegacyReadAsync(id, offset, length, cancellationToken);
    }

    internal async Task InsertAsync(NpgsqlConnection connection, StoredFileWrite file, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO {TableName} (id, file_name, media_type, content_length, sha256, content)
            VALUES (@id, @file_name, @media_type, @content_length, @sha256, @content)
            ON CONFLICT (id) DO NOTHING
            """;
        command.Parameters.AddWithValue("id", file.Id);
        command.Parameters.AddWithValue("file_name", file.FileName);
        command.Parameters.AddWithValue("media_type", file.MediaType);
        command.Parameters.AddWithValue("content_length", file.ContentLength);
        command.Parameters.AddWithValue("sha256", file.SHA256);
        command.Parameters.Add("content", NpgsqlDbType.Bytea).Value = file.Content;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task ClearAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"TRUNCATE TABLE {TableName}";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MigrateLegacyDocumentsAsync(CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        var legacyFiles = await session.Query<StoredFilePayload>().ToListAsync(cancellationToken);
        foreach (var legacy in legacyFiles)
        {
            await using var content = new MemoryStream(legacy.Content, writable: false);
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await InsertAsync(connection, new StoredFileWrite(
                legacy.Id,
                legacy.FileName,
                legacy.MediaType,
                legacy.Content.LongLength,
                legacy.SHA256,
                content), cancellationToken);
        }
    }

    private async Task<StoredFileMetadata?> GetLegacyMetadataAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        var legacy = await session.LoadAsync<StoredFilePayload>(id, cancellationToken);
        return legacy is null ? null : LegacyMetadata(legacy);
    }

    private async Task<StoredFileRead?> OpenLegacyReadAsync(Guid id, long offset, long? length, CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        var legacy = await session.LoadAsync<StoredFilePayload>(id, cancellationToken);
        if (legacy is null || offset >= legacy.Content.LongLength)
            return null;

        var available = legacy.Content.LongLength - offset;
        var selectedLength = Math.Min(length ?? available, available);
        return new StoredFileRead(
            LegacyMetadata(legacy),
            new MemoryStream(legacy.Content, checked((int)offset), checked((int)selectedLength), writable: false));
    }

    private static StoredFileMetadata ReadMetadata(NpgsqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3), reader.GetString(4));

    private static StoredFileMetadata LegacyMetadata(StoredFilePayload legacy) =>
        new(legacy.Id, legacy.FileName, legacy.MediaType, legacy.Content.LongLength, legacy.SHA256);

    private sealed class ReaderOwner(
        NpgsqlDataReader reader,
        NpgsqlCommand command,
        NpgsqlConnection connection) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await reader.DisposeAsync();
            await command.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
