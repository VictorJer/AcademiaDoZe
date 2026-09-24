using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
using System.Text;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class AcessoAlunoRepository : BaseRepository, IAcessoAlunoRepository
{
    private const int PessoaTipoAluno = 0;

    public AcessoAlunoRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
    {
    }

    public async Task<AcessoAluno?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"SELECT id_acesso, pessoa_tipo, pessoa_id, data_hora
FROM tb_acesso
WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSO_ALUNO_POR_ID", $"Erro ao obter acesso do aluno por ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoAluno>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"SELECT id_acesso, pessoa_tipo, pessoa_id, data_hora
FROM tb_acesso
WHERE pessoa_tipo = @PessoaTipo
ORDER BY data_hora DESC";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<AcessoAluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }

            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS_ACESSOS_ALUNO", $"Erro ao obter acessos dos alunos: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Adicionar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery(@"INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora)
VALUES (@PessoaTipo, @PessoaId, @DataHora)");

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@PessoaId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);

            int id = await command.ExecuteScalarIdAsync("ERRO_ADICIONAR_ACESSO_ALUNO", "Falha ao obter ID do acesso do aluno.", cancellationToken);

            var idProperty = typeof(EntidadeBase).GetProperty(nameof(EntidadeBase.Id));
            var setter = idProperty?.GetSetMethod(nonPublic: true);
            setter?.Invoke(entity, [id]);

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ACESSO_ALUNO", $"Erro ao adicionar acesso do aluno: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Atualizar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"UPDATE tb_acesso
SET pessoa_id = @PessoaId,
    data_hora = @DataHora
WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@PessoaId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
                throw new InfrastructureException("REGISTRO_NAO_ENCONTRADO", $"Nenhum acesso do aluno encontrado com ID {entity.Id} para atualização.");

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO_ALUNO", $"Erro ao atualizar acesso do aluno ID {entity.Id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = @"DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO_ALUNO", $"Erro ao remover acesso do aluno ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoAluno>> ObterAcessosPorAlunoPeriodo(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = new StringBuilder("SELECT id_acesso, pessoa_tipo, pessoa_id, data_hora FROM tb_acesso WHERE pessoa_tipo = @PessoaTipo");
            var parameters = new List<(string Name, object Value, DbType Type)> { ("@PessoaTipo", PessoaTipoAluno, DbType.Int32) };

            if (alunoId.HasValue)
            {
                builder.Append(" AND pessoa_id = @AlunoId");
                parameters.Add(("@AlunoId", alunoId.Value, DbType.Int32));
            }

            if (inicio.HasValue)
            {
                builder.Append(" AND data_hora >= @Inicio");
                parameters.Add(("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime));
            }

            if (fim.HasValue)
            {
                builder.Append(" AND data_hora <= @Fim");
                parameters.Add(("@Fim", fim.Value.ToDateTime(TimeOnly.MaxValue), DbType.DateTime));
            }

            builder.Append(" ORDER BY data_hora DESC");

            await using var command = await CreateCommandAsync(builder.ToString(), cancellationToken);
            foreach (var (name, value, type) in parameters)
            {
                command.AddParameter(name, value, type);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<AcessoAluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }

            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_ALUNO_PERIODO", $"Erro ao obter acessos do aluno no período: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno?> ObterUltimoAcesso(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"SELECT TOP 1 id_acesso, pessoa_tipo, pessoa_id, data_hora
FROM tb_acesso
WHERE pessoa_tipo = @PessoaTipo AND pessoa_id = @AlunoId
ORDER BY data_hora DESC";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO_ALUNO", $"Erro ao obter último acesso do aluno {alunoId}: {ex.Message}", ex);
        }
    }

    public async Task<bool> EstaNaAcademia(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM tb_acesso
    WHERE pessoa_tipo = @PessoaTipo
      AND pessoa_id = @AlunoId
      AND data_hora >= @DataLimite)
THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);
            command.AddParameter("@DataLimite", DateTime.Now.AddHours(-12), DbType.DateTime);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is bool b && b || result is int i && i != 0 || result is long l && l != 0 || result is byte by && by != 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_VERIFICAR_ACESSO_ALUNO", $"Erro ao verificar presença do aluno {alunoId}: {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $@"SELECT {GetDateHourExpression("data_hora")}, COUNT(*)
FROM tb_acesso
WHERE pessoa_tipo = @PessoaTipo AND {GetDateMonthExpression("data_hora")} = @Mes
GROUP BY {GetDateHourExpression("data_hora")}
ORDER BY {GetDateHourExpression("data_hora")}";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@Mes", mes, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var resultado = new Dictionary<TimeOnly, int>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var hora = reader.GetInt32(0);
                resultado[TimeOnly.FromTimeSpan(TimeSpan.FromHours(hora))] = reader.GetInt32(1);
            }

            return resultado;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_HORARIO_PROCURADO_ALUNO", $"Erro ao calcular horários de maior procura do aluno no mês {mes}: {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"SELECT pessoa_id, data_hora
FROM tb_acesso
WHERE pessoa_tipo = @PessoaTipo AND MONTH(data_hora) = @Mes
ORDER BY pessoa_id, data_hora";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@Mes", mes, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessosPorPessoa = new Dictionary<int, List<DateTime>>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var pessoaId = reader.GetInt32Value("pessoa_id");
                var dataHora = reader.GetDateTimeValue("data_hora");
                if (!acessosPorPessoa.ContainsKey(pessoaId)) acessosPorPessoa[pessoaId] = new List<DateTime>();
                acessosPorPessoa[pessoaId].Add(dataHora);
            }

            var resultado = new Dictionary<int, TimeSpan>();
            foreach (var item in acessosPorPessoa)
            {
                if (item.Value.Count < 2)
                    continue;

                var menor = item.Value.Min();
                var maior = item.Value.Max();
                resultado[item.Key] = maior - menor;
            }

            return resultado;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_PERMANENCIA_MEDIA_ALUNO", $"Erro ao calcular permanência média do aluno no mês {mes}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Aluno>> ObterAlunosSemAcessoNosUltimosDias(int dias, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"SELECT a.id_aluno, a.cpf, a.nome, a.nascimento, a.telefone, a.email,
    a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
    l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
FROM tb_aluno a
INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
LEFT JOIN tb_acesso ac ON ac.pessoa_tipo = @PessoaTipo AND ac.pessoa_id = a.id_aluno AND ac.data_hora >= @DataLimite
WHERE ac.id_acesso IS NULL";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@DataLimite", DateTime.Now.AddDays(-dias), DbType.DateTime);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var alunos = new List<Aluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                alunos.Add(AlunoRepository.Map(reader));
            }

            return alunos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ALUNOS_SEM_ACESSO", $"Erro ao obter alunos sem acesso nos últimos {dias} dias: {ex.Message}", ex);
        }
    }

    private static AcessoAluno Map(DbDataReader reader)
    {
        var alunoId = reader.GetInt32Value("pessoa_id");
        var dataHora = reader.GetDateTimeValue("data_hora");
        var id = reader.GetInt32Value("id_acesso");

        var constructor = typeof(AcessoAluno).GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(int), typeof(int), typeof(DateTime) }, null);
        if (constructor == null)
            throw new InfrastructureException("ERRO_MAPEAMENTO_ACESSO_ALUNO", "Construtor interno do acesso do aluno não foi encontrado.");

        var instance = constructor.Invoke(new object[] { id, alunoId, dataHora });
        return (AcessoAluno)instance;
    }

    private static string GetDateHourExpression(string dateColumn)
    {
        return DbProvider.GetDateHourExpression(dateColumn, DatabaseType.SqlServer);
    }

    private static string GetDateMonthExpression(string dateColumn)
    {
        return DbProvider.GetDateMonthExpression(dateColumn, DatabaseType.SqlServer);
    }
}
