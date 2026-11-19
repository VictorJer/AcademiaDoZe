using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class SqliteLogradouroIntegrationTests
{
    [Fact]
    public async Task Matricula_vigente_e_reconhecida_em_banco_sqlite()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"academiadoze-matricula-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Pooling=False";

        try
        {
            await using var logradouroRepo = new LogradouroRepository(connectionString, DatabaseType.Sqlite);
            await using var alunoRepo = new AlunoRepository(connectionString, DatabaseType.Sqlite);
            await using var matriculaRepo = new MatriculaRepository(connectionString, DatabaseType.Sqlite);

            var logradouro = Logradouro.Criar(0, "88000001", "Rua Teste", "Centro", "Florianopolis", "SC", "Brasil").Value!;
            logradouro = await logradouroRepo.Adicionar(logradouro);

            var aluno = Aluno.Criar(
                0,
                "Aluno SQLite",
                "529.982.247-25",
                new DateOnly(2000, 1, 1),
                "(11) 91234-5678",
                "aluno.sqlite@test.com",
                logradouro,
                "123",
                "",
                "SenhaValida123",
                Arquivo.Criar([1, 2, 3]).Value!
            ).Value!;
            aluno = await alunoRepo.Adicionar(aluno);

            var matricula = Matricula.Criar(
                0,
                aluno,
                MatriculaPlano.Mensal,
                DateOnly.FromDateTime(DateTime.Today),
                "Condicionamento",
                MatriculaRestricoes.None,
                null,
                ""
            ).Value!;
            matricula = await matriculaRepo.Adicionar(matricula);

            Assert.True(await matriculaRepo.PossuiMatriculaAtiva(aluno.Id));
            Assert.NotNull(await matriculaRepo.ObterMatriculaAtivaPorAluno(aluno.Id));
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

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
