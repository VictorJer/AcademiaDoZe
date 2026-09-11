using AcademiaDoZe.Infrastructure.Data;
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
namespace AcademiaDoZe.Infrastructure.Tests;

public abstract class TestBase
{
    private const DatabaseType SelectedDatabaseType = DatabaseType.SqlServer;
    protected string ConnectionString { get; }
    protected DatabaseType DatabaseType { get; }
    protected TestBase()
    {
        DatabaseType = SelectedDatabaseType;
        ConnectionString = DatabaseType switch
        {
            DatabaseType.SqlServer => "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=db_academia_do_ze;Integrated Security=True;TrustServerCertificate=True;",
            _ => throw new ArgumentOutOfRangeException(nameof(DatabaseType), DatabaseType, "SGBD não suportado para testes.")
        };
    }
    #region Geradores de dados aleatórios
    private static int _counter = 10000;
    protected static string GerarCep() => (80000000 + ((int)(DateTime.UtcNow.Ticks % 8000000)) + Interlocked.Increment(ref _counter)).ToString("D8")[..8];
    protected static string GerarCpf()
    {
        int[] digits = new int[9];
        for (var i = 0; i < 9; i++)
        {
            digits[i] = Random.Shared.Next(0, 10);
        }

        if (digits.All(d => d == digits[0]))
        {
            digits[8] = (digits[8] + 1) % 10;
        }

        int sum1 = 0;
        for (var i = 0; i < 9; i++)
        {
            sum1 += digits[i] * (10 - i);
        }

        int d1 = sum1 % 11;
        d1 = d1 < 2 ? 0 : 11 - d1;

        int sum2 = 0;
        for (var i = 0; i < 9; i++)
        {
            sum2 += digits[i] * (11 - i);
        }
        sum2 += d1 * 2;

        int d2 = sum2 % 11;
        d2 = d2 < 2 ? 0 : 11 - d2;

        return string.Concat(digits.Concat(new[] { d1, d2 }));
    }
    protected static string GerarEmail() => $"user_{Guid.NewGuid().ToString("N")[..8]}@test.com";
    protected static string GerarTelefone() => (49990000000L + ((DateTime.UtcNow.Ticks % 8000000000L)) + Interlocked.Increment(ref _counter)).ToString("D11")[..11];
    #endregion
}