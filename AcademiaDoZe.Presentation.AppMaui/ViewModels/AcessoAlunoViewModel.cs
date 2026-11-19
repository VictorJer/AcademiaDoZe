using System.Collections.ObjectModel;
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AcessoAlunoViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService;
    private readonly IAcessoAlunoService _acessoService;
    private readonly IMatriculaService _matriculaService;
    private AlunoDto? _alunoSelecionado;
    private string _situacao = "Selecione um aluno.";

    public ObservableCollection<AlunoDto> Alunos { get; } = [];
    public string Situacao { get => _situacao; set => SetProperty(ref _situacao, value); }
    public AlunoDto? AlunoSelecionado
    {
        get => _alunoSelecionado;
        set
        {
            if (SetProperty(ref _alunoSelecionado, value))
                _ = AtualizarSituacaoAsync();
        }
    }

    public AcessoAlunoViewModel(IAlunoService alunoService, IAcessoAlunoService acessoService, IMatriculaService matriculaService)
    {
        _alunoService = alunoService;
        _acessoService = acessoService;
        _matriculaService = matriculaService;
        Title = "Entrada e saída de alunos";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Alunos.Clear();
            foreach (var aluno in await _alunoService.ObterTodosAsync(cts.Token))
                Alunos.Add(aluno);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        if (AlunoSelecionado == null)
        {
            await Shell.Current.DisplayAlertAsync("Seleção obrigatória", "Selecione um aluno.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var pessoaId = AlunoSelecionado.Id;
            var todos = (await _acessoService.ObterPorAlunoPeriodoAsync(pessoaId, null, null, cts.Token)).ToList();
            var estaDentro = todos.Count % 2 == 1;
            var agora = DateTime.Now;

            if (estaDentro)
            {
                var entrada = todos.OrderByDescending(a => a.DataHora).First();
                await _acessoService.AdicionarAsync(new AcessoAlunoDto { AlunoId = pessoaId, DataHora = agora }, cts.Token);
                var permanencia = agora - entrada.DataHora;
                await Shell.Current.DisplayAlertAsync("Saída registrada",
                    $"Tempo na academia: {permanencia.Hours:D2}h {permanencia.Minutes:D2}min.", "OK");
            }
            else
            {
                var matricula = await _matriculaService.ObterMatriculaAtivaPorAlunoAsync(pessoaId, cts.Token);
                if (matricula == null)
                {
                    await Shell.Current.DisplayAlertAsync("Entrada não autorizada",
                        "O aluno não possui matrícula ativa.", "OK");
                    return;
                }
                await _acessoService.AdicionarAsync(new AcessoAlunoDto { AlunoId = pessoaId, DataHora = agora }, cts.Token);
                var restante = matricula.DataFim.ToDateTime(TimeOnly.MaxValue) - agora;
                await Shell.Current.DisplayAlertAsync("Entrada registrada",
                    $"Plano ativo por aproximadamente {Math.Max(0, restante.Days)} dia(s).", "OK");
            }
            await AtualizarSituacaoAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }

    private async Task AtualizarSituacaoAsync()
    {
        if (AlunoSelecionado == null) { Situacao = "Selecione um aluno."; return; }
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var todos = await _acessoService.ObterPorAlunoPeriodoAsync(AlunoSelecionado.Id, null, null, cts.Token);
            var dentro = todos.Count() % 2 == 1;
            Situacao = dentro ? "Status: dentro da academia" : "Status: fora da academia";
        }
        catch { Situacao = "Não foi possível verificar o status."; }
    }
}
