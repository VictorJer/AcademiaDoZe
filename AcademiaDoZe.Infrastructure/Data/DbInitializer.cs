using AcademiaDoZe.Infrastructure.Exceptions;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
namespace AcademiaDoZe.Infrastructure.Data;

public static class DbInitializer
{
    private static readonly ConcurrentDictionary<string, bool> _bancosInicializados = new();
    public static async Task InicializarAsync(string connectionString, DatabaseType databaseType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var key = $"{databaseType}:{connectionString}";
        if (_bancosInicializados.ContainsKey(key)) return;

        try
        {
            if (databaseType == DatabaseType.SqlServer)
            {
                var databaseBuilder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = databaseBuilder.InitialCatalog;
                if (string.IsNullOrWhiteSpace(databaseName))
                    throw new InfrastructureException("BANCO_NAO_INFORMADO", "Informe o banco de dados na string de conexão do SQL Server.");

                var masterBuilder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master"
                };

                await using (var masterConnection = DbProvider.CreateConnection(masterBuilder.ConnectionString, databaseType))
                {
                    await masterConnection.OpenAsync(cancellationToken);
                    const string databaseExistsQuery = "SELECT CASE WHEN DB_ID(@DatabaseName) IS NOT NULL THEN 1 ELSE 0 END;";
                    await using var databaseExistsCommand = DbProvider.CreateCommand(databaseExistsQuery, masterConnection);
                    databaseExistsCommand.AddParameter("@DatabaseName", databaseName, System.Data.DbType.String);
                    var databaseExists = Convert.ToInt32(await databaseExistsCommand.ExecuteScalarAsync(cancellationToken)) == 1;

                    if (!databaseExists)
                    {
                        var escapedDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
                        await using var createDatabaseCommand = DbProvider.CreateCommand($"CREATE DATABASE [{escapedDatabaseName}];", masterConnection);
                        await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }

            var scriptSql = ObterScript(databaseType);
            await using var connection = DbProvider.CreateConnection(connectionString, databaseType);
            await connection.OpenAsync(cancellationToken);
            await using var command = DbProvider.CreateCommand(scriptSql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            _bancosInicializados.TryAdd(key, true);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_INICIALIZAR_BANCO", $"Erro ao inicializar banco de dados: {ex.Message}", ex);
        }
    }
    public static string ObterScript(DatabaseType databaseType)
    {
        var nomeScript = DbProvider.GetScriptName(databaseType);
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
        .FirstOrDefault(r => r.EndsWith(nomeScript, StringComparison.OrdinalIgnoreCase))
        ?? throw new InfrastructureException("SCRIPT_EMBARCADO_NAO_ENCONTRADO", $"Script SQL embarcado '{nomeScript}' não encontrado.");
        using var stream = assembly.GetManifestResourceStream(resourceName)
        ?? throw new InfrastructureException("ERRO_LEITURA_SCRIPT", $"Erro ao carregar o fluxo do script embarcado '{nomeScript}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}