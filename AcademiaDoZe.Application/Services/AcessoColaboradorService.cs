// Victor Jeremiasiago Kovalski
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public class AcessoColaboradorService : IAcessoColaboradorService
{
    private readonly Func<IAcessoColaboradorRepository> _acessoRepoFactory;
    private readonly Func<IColaboradorRepository> _colaboradorRepoFactory;

    public AcessoColaboradorService(Func<IAcessoColaboradorRepository> acessoRepoFactory, Func<IColaboradorRepository> colaboradorRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _colaboradorRepoFactory = colaboradorRepoFactory ?? throw new ArgumentNullException(nameof(colaboradorRepoFactory));
    }

    public async Task<AcessoColaboradorDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        => (await _acessoRepoFactory().ObterPorId(id, cancellationToken))?.ToDto();

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterTodosAsync(CancellationToken cancellationToken = default)
        => (await _acessoRepoFactory().ObterTodos(cancellationToken)).Select(acesso => acesso.ToDto());

    public async Task<AcessoColaboradorDto> AdicionarAsync(AcessoColaboradorDto acessoDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acessoDto);
        var colaborador = await _colaboradorRepoFactory().ObterPorId(acessoDto.ColaboradorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Colaborador com ID {acessoDto.ColaboradorId} não encontrado.");
        var result = Domain.Entities.AcessoColaborador.Criar(acessoDto.Id, colaborador, acessoDto.DataHora);
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

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterPorColaboradorPeriodoAsync(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
        => (await _acessoRepoFactory().ObterAcessosPorColaboradorPeriodo(colaboradorId, inicio, fim, cancellationToken)).Select(acesso => acesso.ToDto());

    public async Task<AcessoColaboradorDto?> ObterUltimoAcessoAsync(int colaboradorId, CancellationToken cancellationToken = default)
        => (await _acessoRepoFactory().ObterUltimoAcesso(colaboradorId, cancellationToken))?.ToDto();

    public Task<TimeSpan> ObterHorasTrabalhadasNoDiaAsync(int colaboradorId, DateOnly data, CancellationToken cancellationToken = default)
        => _acessoRepoFactory().ObterHorasTrabalhadasNoDia(colaboradorId, data, cancellationToken);
}