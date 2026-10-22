using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class ColaboradorListViewModel : BaseViewModel
{
    private readonly IColaboradorService _colaboradorService;
    private List<ColaboradorDto> _todosColaboradores = [];
    private string _searchText = string.Empty;

    public ObservableCollection<ColaboradorDto> Colaboradores { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public ColaboradorListViewModel(IColaboradorService colaboradorService)
    {
        _colaboradorService = colaboradorService;
        Title = "Colaboradores";
    }

    [RelayCommand]
    private async Task AddColaboradorAsync() => await Shell.Current.GoToAsync("colaborador");

    [RelayCommand]
    private async Task EditColaboradorAsync(ColaboradorDto colaborador)
    {
        if (colaborador != null)
            await Shell.Current.GoToAsync($"colaborador?Id={colaborador.Id}");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;
        IsRefreshing = true;
        await LoadColaboradoresAsync();
    }

    [RelayCommand]
    private void SearchColaboradores() => ApplySearch();

    [RelayCommand]
    private async Task LoadColaboradoresAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            _todosColaboradores = [.. await _colaboradorService.ObterTodosAsync(cts.Token)];
            ApplySearch();
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo esgotado", "O carregamento dos colaboradores excedeu o tempo limite.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível carregar os colaboradores: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteColaboradorAsync(ColaboradorDto colaborador)
    {
        if (colaborador == null)
            return;

        var confirmar = await Shell.Current.DisplayAlertAsync(
            "Excluir colaborador",
            $"Deseja realmente excluir o cadastro de {colaborador.Nome}?",
            "Excluir", "Cancelar");
        if (!confirmar)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if (await _colaboradorService.RemoverAsync(colaborador.Id, cts.Token))
            {
                _todosColaboradores.RemoveAll(item => item.Id == colaborador.Id);
                ApplySearch();
                await Shell.Current.DisplayAlertAsync("Cadastro removido", "O colaborador foi excluído.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Não encontrado", "O cadastro já não existe ou não pôde ser removido.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo esgotado", "A exclusão do colaborador excedeu o tempo limite.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível excluir o colaborador: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySearch()
    {
        var termo = SearchText.Trim();
        var resultado = string.IsNullOrEmpty(termo)
            ? _todosColaboradores
            : _todosColaboradores.Where(colaborador =>
                colaborador.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                colaborador.Cpf.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                colaborador.Tipo.ToString().Contains(termo, StringComparison.OrdinalIgnoreCase));

        Colaboradores.Clear();
        foreach (var colaborador in resultado)
            Colaboradores.Add(colaborador);
    }
}
