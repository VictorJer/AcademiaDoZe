// Thiago Kovalski
using AcademiaDoZe.Application.DTOs;

namespace AcademiaDoZe.Application.Interfaces;

public interface IAcessoAlunoService
{
    Task<AcessoAlunoDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AcessoAlunoDto>> ObterTodosAsync(CancellationToken cancellationToken = default);
    Task<AcessoAlunoDto> AdicionarAsync(AcessoAlunoDto acessoDto, CancellationToken cancellationToken = default);
    Task<bool> RemoverAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AcessoAlunoDto>> ObterPorAlunoPeriodoAsync(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default);
    Task<AcessoAlunoDto?> ObterUltimoAcessoAsync(int alunoId, CancellationToken cancellationToken = default);
    Task<bool> EstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default);
    Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMesAsync(int mes, CancellationToken cancellationToken = default);
    Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMesAsync(int mes, CancellationToken cancellationToken = default);
    Task<IEnumerable<AlunoDto>> ObterAlunosSemAcessoNosUltimosDiasAsync(int dias, CancellationToken cancellationToken = default);
}