// Victor Jeremiasiago Kovalski
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public class AcessoAlunoService : IAcessoAlunoService
{
    private readonly Func<IAcessoAlunoRepository> _acessoRepoFactory;
    private readonly Func<IAlunoRepository> _alunoRepoFactory;
    private readonly Func<IMatriculaRepository> _matriculaRepoFactory;

    public AcessoAlunoService(
        Func<IAcessoAlunoRepository> acessoRepoFactory,
        Func<IAlunoRepository> alunoRepoFactory,
        Func<IMatriculaRepository> matriculaRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _alunoRepoFactory = alunoRepoFactory ?? throw new ArgumentNullException(nameof(alunoRepoFactory));
        _matriculaRepoFactory = matriculaRepoFactory ?? throw new ArgumentNullException(nameof(matriculaRepoFactory));
    }

    public async Task<AcessoAlunoDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        => (await _acessoRepoFactory().ObterPorId(id, cancellationToken))?.ToDto();

    public async Task<IEnumerable<AcessoAlunoDto>> ObterTodosAsync(CancellationToken cancellationToken = default)
        => (await _acessoRepoFactory().ObterTodos(cancellationToken)).Select(acesso => acesso.ToDto());

    public async Task<AcessoAlunoDto> AdicionarAsync(AcessoAlunoDto acessoDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acessoDto);
        var aluno = await _alunoRepoFactory().ObterPorId(acessoDto.AlunoId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aluno com ID {acessoDto.AlunoId} não encontrado.");
        if (!await _matriculaRepoFactory().PossuiMatriculaAtiva(aluno.Id, cancellationToken))
            throw new InvalidOperationException("O aluno não possui matrícula ativa.");

        var result = Domain.Entities.AcessoAluno.Criar(acessoDto.Id, aluno, acessoDto.DataHora);
        if (result.IsFailure)
            throw new InvalidOperationException($"Erro de validação ao registrar acesso: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
        var acesso = await _acessoRepoFactory().Adicionar(result.Value!, cancellationToken);
        return acesso.ToDto();
    }

    public async Task<bool> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _acessoRepoFactory().ObterPorId(id, cancellationToken) == null) return false;
        return await _acessoRepoFactory().Remover(id, cancellationToken);
    }

    public async Task<IEnumerable<AcessoAlunoDto>> ObterPorAlunoPeriodoAsync(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
        => (await _acessoRepoFactory().ObterAcessosPorAlunoPeriodo(alunoId, inicio, fim, cancellationToken)).Select(acesso => acesso.ToDto());

    public async Task<AcessoAlunoDto?> ObterUltimoAcessoAsync(int alunoId, CancellationToken cancellationToken = default)
        => (await _acessoRepoFactory().ObterUltimoAcesso(alunoId, cancellationToken))?.ToDto();

    public Task<bool> EstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default)
        => _acessoRepoFactory().EstaNaAcademia(alunoId, cancellationToken);

    public Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMesAsync(int mes, CancellationToken cancellationToken = default)
        => _acessoRepoFactory().ObterHorarioMaisProcuradoPorMes(mes, cancellationToken);

    public Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMesAsync(int mes, CancellationToken cancellationToken = default)
        => _acessoRepoFactory().ObterPermanenciaMediaPorMes(mes, cancellationToken);

    public async Task<IEnumerable<AlunoDto>> ObterAlunosSemAcessoNosUltimosDiasAsync(int dias, CancellationToken cancellationToken = default)
        => (await _acessoRepoFactory().ObterAlunosSemAcessoNosUltimosDias(dias, cancellationToken)).Select(aluno => aluno.ToDto());
}
