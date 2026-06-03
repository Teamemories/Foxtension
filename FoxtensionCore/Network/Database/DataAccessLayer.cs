using Dapper;
using Foxtension.Network.ConnectionString;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Foxtension.Network.Database
{
    public sealed class DataAccessLayer : IDisposable, IAsyncDisposable
    {
        #region Connection
        private readonly string _connectionString;
        private readonly StringBuilderType _engine;
        public DataAccessLayer(string connectionString, StringBuilderType dbType)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _engine = dbType;
        }
        public async Task<IDbConnection> ConnectionAsync(CancellationToken ct = default)
        {
            IDbConnection conn = _engine switch
            {
                StringBuilderType.SqlServer => new SqlConnection(_connectionString),
                StringBuilderType.Sqlite => new SqliteConnection(_connectionString),
                _ => throw new NotSupportedException()
            };

            switch (_engine)
            {
                case StringBuilderType.SqlServer:
                    await ((SqlConnection)conn).OpenAsync(ct);
                    break;
                case StringBuilderType.Sqlite:
                    await ((SqliteConnection)conn).OpenAsync(ct);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return conn;
        }

        public async Task<bool> ConnectionCheckAsync()
        {
            try
            {
                using (IDbConnection connection = await ConnectionAsync())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new OperationCanceledException(ex.Message);
            }
        }

        private async Task DisconnectionAsync(IDbConnection connection)
        {
            if (connection == null) return;
            try
            {
                if (connection is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    connection.Dispose();
                }

            }
            catch (Exception ex)
            {
                throw new OperationCanceledException(ex.Message);
            }
        }
        #endregion

        #region Select
        public async Task<IEnumerable<T>> SelectAsync<T>(IDataEntity entity) where T : class, IDataEntity, new()
        {
            if (string.IsNullOrWhiteSpace(entity.TableName))
                throw new ArgumentNullException(nameof(entity.TableName) + " must be provided.");

            using var conn = await ConnectionAsync();

            string cols = entity.Columns != null && entity.Columns.Length > 0
                ? string.Join(", ", entity.Columns)
                : "*";
            var conditions = entity.Conditions?.Where(c => c.Value != null).ToList() ?? new List<KeyValuePair<string, object>>();
            string where = conditions.Any()
                ? " WHERE " + string.Join(" AND ", conditions.Select(c => $"[{c.Key}]=@{c.Key}"))
                : "";

            string query = $"SELECT {cols} FROM {entity.TableName}{where}";

            try
            {
                return await conn.QueryAsync<T>(query, entity.Conditions);
            }
            finally
            {
                await DisconnectionAsync(conn);
            }
        }
        #endregion

        #region Insert
        public async Task<int> InsertAsync(IEnumerable<IDataEntity> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("Entities collection is empty.");

            var first = entities.First();
            if (string.IsNullOrWhiteSpace(first.TableName))
                throw new ArgumentNullException(nameof(first.TableName) + " must be provided.");
            if (!first.Properties.Any())
                throw new ArgumentNullException("Properties must be provided for INSERT action.");

            string cols = string.Join(", ", first.Properties.Keys.Select(k => $"[{k}]"));
            string vals = string.Join(", ", first.Properties.Keys.Select(k => $"@{k}"));

            string query = $"INSERT INTO {first.TableName} ({cols}) VALUES ({vals});";

            using var conn = await ConnectionAsync();

            var paramList = entities.Select(e => e.Properties).ToList();

            try
            {
                return await conn.ExecuteAsync(query, paramList);
            }
            finally
            {
                await DisconnectionAsync(conn);
            }
        }
        #endregion

        #region Update
        public async Task<int> UpdateAsync(IEnumerable<IDataEntity> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("Entities collection is empty.");

            var first = entities.First();
            if (string.IsNullOrWhiteSpace(first.TableName))
                throw new ArgumentNullException(nameof(first.TableName) + " must be provided.");

            if (!first.Properties.Any() || !first.Conditions.Any())
                throw new ArgumentException("Properties and Conditions must be provided for UPDATE action.");

            string set = string.Join(", ", first.Properties.Keys.Select(k => $"[{k}]=@{k}"));
            string where = string.Join(" AND ", first.Conditions.Keys.Select(k => $"[{k}]=@{k}"));
            string query = $"UPDATE {first.TableName} SET {set} WHERE {where};";

            using var conn = await ConnectionAsync();

            var paramList = entities.Select(e => e.Properties.Concat(e.Conditions).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList();

            try
            {
                return await conn.ExecuteAsync(query, paramList);
            }
            finally
            {
                await DisconnectionAsync(conn);
            }
        }
        #endregion

        #region Delete
        public async Task<int> DeleteAsync(IEnumerable<IDataEntity> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("Entities collection is empty.");

            var first = entities.First();
            if (string.IsNullOrWhiteSpace(first.TableName))
                throw new ArgumentNullException(nameof(first.TableName) + " must be provided.");

            if (!first.Conditions.Any())
                throw new ArgumentException("Conditions must be provided for DELETE action.");

            string where = string.Join(" AND ", first.Conditions.Keys.Select(k => $"[{k}]=@{k}"));
            string query = $"DELETE FROM {first.TableName} WHERE {where};";

            using var conn = await ConnectionAsync();

            var paramList = entities.Select(e => e.Conditions).ToList();

            try
            {
                return await conn.ExecuteAsync(query, paramList);
            }
            finally
            {
                await DisconnectionAsync(conn);
            }
        }
        #endregion

        #region Truncate
        public async Task<bool> TruncateTableAsync(IDataEntity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.TableName))
                throw new ArgumentException("Conditions must be provided for TRUNCATE action.");

            using var conn = await ConnectionAsync();
            string query = GetTruncate(entity.TableName);

            try
            {
                return await conn.ExecuteAsync(query) > 0 ? true : false;
            }
            finally
            {
                await DisconnectionAsync(conn);
            }
        }
        #endregion

        #region Trigger equiped by Transaction
        public async Task<bool> TriggerAsync(IDataEntity entity, bool transaction)
        {
            if (string.IsNullOrWhiteSpace(entity.Trigger))
                throw new ArgumentNullException("TRIGGER can not be empty.");

            using var conn = await ConnectionAsync();

            try
            {
                if (transaction)
                {
                    using var transact = conn.BeginTransaction();
                    int res = await conn.ExecuteAsync(entity.Trigger, transact);
                    transact.Commit();
                    return res > 0;
                }
                else
                {
                    return (await conn.ExecuteAsync(entity.Trigger)) > 0;
                }
            }
            finally
            {
                await DisconnectionAsync(conn);
            }
        }
        #endregion

        #region Backup
        public async Task<bool> BackupAsync(string dbName, string path)
        {
            if (!_engine.Equals(StringBuilderType.SqlServer)) return false;
            if (string.IsNullOrWhiteSpace(dbName))
                throw new ArgumentNullException(nameof(dbName));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            var query = $@"BACKUP DATABASE [{dbName}] TO DISK = {Path.Combine(path, dbName)}[{DateTime.UtcNow:yyyy_MMMM_dd~HH.mm.ss.tt}].bak";

            await using var conn = new SqlConnection(_connectionString);
            CancellationToken ct = default(CancellationToken);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using var cmd = new SqlCommand(query, conn);

            return await (cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false)) > 0;
        }
        #endregion

        #region Helpers
        private string GetLimitOne(string query) => _engine == StringBuilderType.Sqlite ? query + " LIMIT 1;" : query.Replace("SELECT", "SELECT TOP 1");

        private string GetTruncate(string table) => _engine == StringBuilderType.SqlServer ? $"TRUNCATE TABLE {table};" : $"DELETE FROM {table};";
        #endregion

        #region Dispose

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}