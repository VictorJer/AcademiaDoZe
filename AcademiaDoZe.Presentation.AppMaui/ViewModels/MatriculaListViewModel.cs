using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class MatriculaListViewModel : BaseViewModel
{
    private readonly IMatriculaService _matriculaService;
    private string _searchText = string.Empty;
    private string _selectedFilterType = "Todas";

    public ObservableCollection<string> FilterTypes { get; } = ["Todas", "Id", "Id Aluno", "Ativas", "Vencendo em dias"];
    public ObservableCollection<MatriculaDto> Matriculas { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public MatriculaListViewModel(IMatriculaService matriculaService)
    {
        _matriculaService = matriculaService;
        Title = "Matrículas";
    }

    [RelayCommand]
    private Task AddMatriculaAsync() => Shell.Current.GoToAsync("matricula");

    [RelayCommand]
    private Task EditMatriculaAsync(MatriculaDto matricula)
        => matricula is null ? Task.CompletedTask : Shell.Current.GoToAsync($"matricula?Id={matricula.Id}");

    [RelayCommand]
    private async Task RefreshAsync() => await LoadMatriculasAsync();

    [RelayCommand]
    private async Task LoadMatriculasAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var items = await SearchAsync(cts.Token);
            Matriculas.Clear();
            foreach (var item in items)
                Matriculas.Add(item);
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo esgotado", "A busca excedeu o tempo limite.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível carregar as matrículas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task SearchMatriculasAsync() => LoadMatriculasAsync();

    [RelayCommand]
    private async Task DeleteMatriculaAsync(MatriculaDto matricula)
    {
        if (matricula is null || !await Shell.Current.DisplayAlertAsync(
                "Excluir matrícula", $"Deseja excluir a matrícula {matricula.Id}?", "Excluir", "Cancelar"))
            return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if (await _matriculaService.RemoverAsync(matricula.Id, cts.Token))
            {
                Matriculas.Remove(matricula);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula excluída.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível excluir a matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<IEnumerable<MatriculaDto>> SearchAsync(CancellationToken cancellationToken)
    {
        var text = SearchText.Trim();
        if (SelectedFilterType == "Todas")
            return await _matriculaService.ObterTodasAsync(cancellationToken);
        if (SelectedFilterType == "Id")
            return int.TryParse(text, out var id) ? Single(await _matriculaService.ObterPorIdAsync(id, cancellationToken)) : [];
        if (SelectedFilterType == "Id Aluno")
            return int.TryParse(text, out var alunoId) ? await _matriculaService.ObterPorAlunoIdAsync(alunoId, cancellationToken) : [];
        if (SelectedFilterType == "Vencendo em dias")
            return int.TryParse(text, out var dias) ? await _matriculaService.ObterVencendoEmDiasAsync(dias, cancellationToken) : [];
        return int.TryParse(text, out var activeAlunoId)
            ? await _matriculaService.ObterAtivasAsync(activeAlunoId, cancellationToken)
            : await _matriculaService.ObterAtivasAsync(cancellationToken: cancellationToken);
    }

    private static IEnumerable<MatriculaDto> Single(MatriculaDto? item)
        => item is null ? [] : [item];
}
