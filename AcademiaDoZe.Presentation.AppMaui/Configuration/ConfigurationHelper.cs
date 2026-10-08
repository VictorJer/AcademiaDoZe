using AcademiaDoZe.Application.DependencyInjection;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Presentation.AppMaui.Message;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDoZe.Presentation.AppMaui.Configuration;

public static class ConfigurationHelper
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var (connectionString, databaseType) = ObterConfiguracaoAtual();

        var repoConfig = new RepositoryConfig
        {
            ConnectionString = connectionString,
            DatabaseType = databaseType.ToInfrastructure()
        };

        // Configura a fábrica de repositórios com a string de conexão e tipo de banco
        services.AddSingleton(repoConfig);

        // Assina mensagens de alteração de banco de dados para atualizar o RepositoryConfig diretamente
        WeakReferenceMessenger.Default.Register<RepositoryConfig, BancoPreferencesUpdatedMessage>(repoConfig, (r, m) =>
        {
            var (novaConnStr, novoDbType) = ObterConfiguracaoAtual();

            r.ConnectionString = novaConnStr;
            r.DatabaseType = novoDbType.ToInfrastructure();
        });

        // Configura os serviços da camada de aplicação
        services.AddApplicationServices();
    }

    /// <summary>
    /// Obtém a Connection String e o tipo de banco ativos a partir das Preferences do usuário.
    /// </summary>
    public static (string ConnectionString, AppDatabaseType DatabaseType) ObterConfiguracaoAtual()
    {
        var databaseTypeStr = Preferences.Get("DatabaseType", AppDatabaseType.Sqlite.ToString());

        if (!Enum.TryParse<AppDatabaseType>(databaseTypeStr, out var databaseType))
        {
            databaseType = AppDatabaseType.Sqlite;
        }

        if (databaseType == AppDatabaseType.MySql)
        {
            throw new NotSupportedException("MySQL ainda não está implementado. Selecione SQLite ou SQL Server nas configurações.");
        }

        if (databaseType == AppDatabaseType.SqlServer)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Preferences.Get("SqlServer_Servidor", "localhost"),
                InitialCatalog = Preferences.Get("SqlServer_Banco", "db_academia_do_ze"),
                UserID = Preferences.Get("SqlServer_Usuario", "sa"),
                Password = Preferences.Get("SqlServer_Senha", string.Empty),
                Encrypt = true,
                TrustServerCertificate = true,
                ConnectTimeout = 5
            };

            var complementoSqlServer = Preferences.Get("SqlServer_Complemento", string.Empty);
            if (!string.IsNullOrWhiteSpace(complementoSqlServer))
            {
                var complementoBuilder = new SqlConnectionStringBuilder(complementoSqlServer);
                foreach (string key in complementoBuilder.Keys)
                    builder[key] = complementoBuilder[key];
            }

            return (builder.ConnectionString, databaseType);
        }

        string connectionString;

        if (databaseType == AppDatabaseType.Sqlite)
        {
            var defaultDbPath = Path.Combine(FileSystem.AppDataDirectory, "db_academia_do_ze.db");

            var dbPath = Preferences.Get("Sqlite_Caminho", Preferences.Get("SqliteCaminho", defaultDbPath));

            if (string.IsNullOrWhiteSpace(dbPath))
                dbPath = defaultDbPath;

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                ForeignKeys = true,
                DefaultTimeout = 5
            };

            var complemento = Preferences.Get("Sqlite_Complemento", string.Empty);
            if (!string.IsNullOrWhiteSpace(complemento))
            {
                var complementoBuilder = new SqliteConnectionStringBuilder(complemento);
                foreach (string key in complementoBuilder.Keys)
                    builder[key] = complementoBuilder[key];
            }

            connectionString = builder.ConnectionString;
        }
        else
        {
            var prefix = databaseType == AppDatabaseType.SqlServer ? "SqlServer" : "MySql";

            var defaultServer = databaseType == AppDatabaseType.SqlServer ? "172.24.32.1" : "10.30.21.16";
            var defaultUser = databaseType == AppDatabaseType.SqlServer ? "sa" : "root";
            var defaultComplemento = databaseType == AppDatabaseType.SqlServer
                ? "TrustServerCertificate=True;Encrypt=True;Connect Timeout=5;Connection Timeout=5;"
                : "Connection Timeout=5;Default Command Timeout=30;";

            var dbServer = Preferences.Get($"{prefix}_Servidor", Preferences.Get("Servidor", defaultServer));
            var dbDatabase = Preferences.Get($"{prefix}_Banco", Preferences.Get("Banco", "db_academia_do_ze"));
            var dbUser = Preferences.Get($"{prefix}_Usuario", Preferences.Get("Usuario", defaultUser));
            var dbPassword = Preferences.Get($"{prefix}_Senha", Preferences.Get("Senha", "abcBolinhas12345"));
            var dbComplemento = Preferences.Get($"{prefix}_Complemento", Preferences.Get("Complemento", defaultComplemento));

            connectionString = $"Server={dbServer};Database={dbDatabase};User Id={dbUser};Password={dbPassword};{dbComplemento}";
        }

        return (connectionString, databaseType);
    }
}