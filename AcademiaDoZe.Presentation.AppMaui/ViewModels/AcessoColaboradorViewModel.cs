using System.Collections.ObjectModel;
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AcessoColaboradorViewModel : BaseViewModel
{
    private readonly IColaboradorService _colaboradorService;
    private readonly IAcessoColaboradorService _acessoService;
    private ColaboradorDto? _colaboradorSelecionado;
    private string _situacao = "Selecione um colaborador.";

    public ObservableCollection<ColaboradorDto> Colaboradores { get; } = [];
    public string Situacao { get => _situacao; set => SetProperty(ref _situacao, value); }
    public ColaboradorDto? ColaboradorSelecionado
    {
        get => _colaboradorSelecionado;
        set
        {
            if (SetProperty(ref _colaboradorSelecionado, value))
                _ = AtualizarSituacaoAsync();
        }
    }

    public AcessoColaboradorViewModel(IColaboradorService colaboradorService, IAcessoColaboradorService acessoService)
    {
        _colaboradorService = colaboradorService;
        _acessoService = acessoService;
        Title = "Entrada e saída de colaboradores";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Colaboradores.Clear();
            foreach (var colaborador in await _colaboradorService.ObterTodosAsync(cts.Token))
                Colaboradores.Add(colaborador);
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        if (ColaboradorSelecionado == null)
        {
            await Shell.Current.DisplayAlertAsync("Seleção obrigatória", "Selecione um colaborador.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var pessoaId = ColaboradorSelecionado.Id;
            var todos = (await _acessoService.ObterPorColaboradorPeriodoAsync(pessoaId, null, null, cts.Token)).ToList();
            var estaDentro = todos.Count % 2 == 1;
            var agora = DateTime.Now;

            if (estaDentro)
            {
                var entrada = todos.OrderByDescending(a => a.DataHora).First();
                await _acessoService.AdicionarAsync(new AcessoColaboradorDto { ColaboradorId = pessoaId, DataHora = agora }, cts.Token);
                var horasDia = await _acessoService.ObterHorasTrabalhadasNoDiaAsync(pessoaId, DateOnly.FromDateTime(agora), cts.Token);
                var permanencia = agora - entrada.DataHora;
                await Shell.Current.DisplayAlertAsync("Saída registrada",
                    $"Tempo nesta permanência: {permanencia.Hours:D2}h {permanencia.Minutes:D2}min.\nTotal do dia: {horasDia.Hours:D2}h {horasDia.Minutes:D2}min.", "OK");
            }
            else
            {
                var horasDia = await _acessoService.ObterHorasTrabalhadasNoDiaAsync(pessoaId, DateOnly.FromDateTime(agora), cts.Token);
                var limite = ColaboradorSelecionado.Vinculo == AppColaboradorVinculo.Estagio ? TimeSpan.FromHours(6) : TimeSpan.FromHours(8);
                if (horasDia >= limite)
                {
                    await Shell.Current.DisplayAlertAsync("Entrada não autorizada",
                        $"O colaborador já atingiu o limite de {limite.TotalHours:N0} horas hoje.", "OK");
                    return;
                }
                await _acessoService.AdicionarAsync(new AcessoColaboradorDto { ColaboradorId = pessoaId, DataHora = agora }, cts.Token);
                await Shell.Current.DisplayAlertAsync("Entrada registrada",
                    $"Limite do vínculo: {limite.TotalHours:N0} horas por dia.", "OK");
            }
            await AtualizarSituacaoAsync();
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK"); }
        finally { IsBusy = false; }
    }

    private async Task AtualizarSituacaoAsync()
    {
        if (ColaboradorSelecionado == null) { Situacao = "Selecione um colaborador."; return; }
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var todos = await _acessoService.ObterPorColaboradorPeriodoAsync(ColaboradorSelecionado.Id, null, null, cts.Token);
            Situacao = todos.Count() % 2 == 1 ? "Status: dentro da academia" : "Status: fora da academia";
        }
        catch { Situacao = "Não foi possível verificar o status."; }
    }
}
