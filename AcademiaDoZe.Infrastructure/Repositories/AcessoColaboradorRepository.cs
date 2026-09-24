using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
using System.Text;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class AcessoColaboradorRepository : BaseRepository, IAcessoColaboradorRepository
{
    private const int PessoaTipoColaborador = 1;

    public AcessoColaboradorRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
    {
    }

    public async Task<AcessoColaborador?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"SELECT id_acesso, pessoa_tipo, pessoa_id, data_hora
FROM tb_acesso
WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSO_COLABORADOR_POR_ID", $"Erro ao obter acesso do colaborador por ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoColaborador>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"SELECT id_acesso, pessoa_tipo, pessoa_id, data_hora
FROM tb_acesso
WHERE pessoa_tipo = @PessoaTipo
ORDER BY data_hora DESC";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<AcessoColaborador>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }

            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS_ACESSOS_COLABORADOR", $"Erro ao obter acessos dos colaboradores: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador> Adicionar(AcessoColaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery(@"INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora)
VALUES (@PessoaTipo, @PessoaId, @DataHora)");

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);
            command.AddParameter("@PessoaId", entity.ColaboradorId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);

            int id = await command.ExecuteScalarIdAsync("ERRO_ADICIONAR_ACESSO_COLABORADOR", "Falha ao obter ID do acesso do colaborador.", cancellationToken);

            var idProperty = typeof(EntidadeBase).GetProperty(nameof(EntidadeBase.Id));
            var setter = idProperty?.GetSetMethod(nonPublic: true);
            setter?.Invoke(entity, [id]);

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ACESSO_COLABORADOR", $"Erro ao adicionar acesso do colaborador: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador> Atualizar(AcessoColaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"UPDATE tb_acesso
SET pessoa_id = @PessoaId,
    data_hora = @DataHora
WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);
            command.AddParameter("@PessoaId", entity.ColaboradorId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
                throw new InfrastructureException("REGISTRO_NAO_ENCONTRADO", $"Nenhum acesso do colaborador encontrado com ID {entity.Id} para atualização.");

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO_COLABORADOR", $"Erro ao atualizar acesso do colaborador ID {entity.Id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = @"DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO_COLABORADOR", $"Erro ao remover acesso do colaborador ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoColaborador>> ObterAcessosPorColaboradorPeriodo(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = new StringBuilder("SELECT id_acesso, pessoa_tipo, pessoa_id, data_hora FROM tb_acesso WHERE pessoa_tipo = @PessoaTipo");
            var parameters = new List<(string Name, object Value, DbType Type)> { ("@PessoaTipo", PessoaTipoColaborador, DbType.Int32) };

            if (colaboradorId.HasValue)
            {
                builder.Append(" AND pessoa_id = @ColaboradorId");
                parameters.Add(("@ColaboradorId", colaboradorId.Value, DbType.Int32));
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
            var acessos = new List<AcessoColaborador>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }

            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_COLABORADOR_PERIODO", $"Erro ao obter acessos do colaborador no período: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador?> ObterUltimoAcesso(int colaboradorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"SELECT TOP 1 id_acesso, pessoa_tipo, pessoa_id, data_hora
FROM tb_acesso
WHERE pessoa_tipo = @PessoaTipo AND pessoa_id = @ColaboradorId
ORDER BY data_hora DESC";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);
            command.AddParameter("@ColaboradorId", colaboradorId, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO_COLABORADOR", $"Erro ao obter último acesso do colaborador {colaboradorId}: {ex.Message}", ex);
        }
    }

    public async Task<TimeSpan> ObterHorasTrabalhadasNoDia(int colaboradorId, DateOnly data, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataInicio = data.ToDateTime(TimeOnly.MinValue);
            var dataFim = data.ToDateTime(TimeOnly.MaxValue);

            var query = @"SELECT data_hora
FROM tb_acesso
WHERE pessoa_tipo = @PessoaTipo AND pessoa_id = @ColaboradorId AND data_hora >= @DataInicio AND data_hora <= @DataFim
ORDER BY data_hora";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);
            command.AddParameter("@ColaboradorId", colaboradorId, DbType.Int32);
            command.AddParameter("@DataInicio", dataInicio, DbType.DateTime);
            command.AddParameter("@DataFim", dataFim, DbType.DateTime);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<DateTime>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(reader.GetDateTimeValue("data_hora"));
            }

            if (acessos.Count <= 1)
                return TimeSpan.Zero;

            return acessos[^1] - acessos[0];
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_HORAS_TRABALHADAS_COLABORADOR", $"Erro ao calcular horas trabalhadas do colaborador {colaboradorId}: {ex.Message}", ex);
        }
    }

    private static AcessoColaborador Map(DbDataReader reader)
    {
        var colaboradorId = reader.GetInt32Value("pessoa_id");
        var dataHora = reader.GetDateTimeValue("data_hora");
        var id = reader.GetInt32Value("id_acesso");

        var constructor = typeof(AcessoColaborador).GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(int), typeof(int), typeof(DateTime) }, null);
        if (constructor == null)
            throw new InfrastructureException("ERRO_MAPEAMENTO_ACESSO_COLABORADOR", "Construtor interno do acesso do colaborador não foi encontrado.");

        var instance = constructor.Invoke(new object[] { id, colaboradorId, dataHora });
        return (AcessoColaborador)instance;
    }
}
