using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class SqliteLogradouroIntegrationTests
{
    [Fact]
    public async Task Logradouro_CRUD_funciona_em_banco_sqlite_novo()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"academiadoze-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Pooling=False";

        try
        {
            await using (var repository = new LogradouroRepository(connectionString, DatabaseType.Sqlite))
            {
                var entidade = Logradouro.Criar(0, "88000001", "Rua Teste", "Centro", "Florianópolis", "SC", "Brasil").Value!;

                var criado = await repository.Adicionar(entidade);
                Assert.True(criado.Id > 0);

                var consultado = await repository.ObterPorId(criado.Id);
                Assert.NotNull(consultado);
                Assert.Equal("Rua Teste", consultado.Nome);

                var atualizado = Logradouro.Criar(criado.Id, "88000002", "Rua Atualizada", "Centro", "Florianópolis", "SC", "Brasil").Value!;
                await repository.Atualizar(atualizado);
                var consultadoAtualizado = await repository.ObterPorId(criado.Id);
                Assert.NotNull(consultadoAtualizado);
                Assert.Equal("Rua Atualizada", consultadoAtualizado.Nome);
                Assert.Equal("88000002", consultadoAtualizado.Cep.Valor);

                Assert.True(await repository.Remover(criado.Id));
                Assert.Null(await repository.ObterPorId(criado.Id));
            }
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }
}
