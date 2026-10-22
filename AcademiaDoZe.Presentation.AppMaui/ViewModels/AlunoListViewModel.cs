using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AlunoListViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService;
    private List<AlunoDto> _todosAlunos = [];
    private string _searchText = string.Empty;

    public ObservableCollection<AlunoDto> Alunos { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public AlunoListViewModel(IAlunoService alunoService)
    {
        _alunoService = alunoService;
        Title = "Alunos";
    }

    [RelayCommand]
    private async Task AddAlunoAsync() => await Shell.Current.GoToAsync("aluno");

    [RelayCommand]
    private async Task EditAlunoAsync(AlunoDto aluno)
    {
        if (aluno != null)
            await Shell.Current.GoToAsync($"aluno?Id={aluno.Id}");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;
        IsRefreshing = true;
        await LoadAlunosAsync();
    }

    [RelayCommand]
    private void SearchAlunos() => ApplySearch();

    [RelayCommand]
    private async Task LoadAlunosAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            _todosAlunos = [.. await _alunoService.ObterTodosAsync(cts.Token)];
            ApplySearch();
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo esgotado", "O carregamento dos alunos excedeu o tempo limite.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível carregar os alunos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAlunoAsync(AlunoDto aluno)
    {
        if (aluno == null)
            return;

        var confirmar = await Shell.Current.DisplayAlertAsync(
            "Excluir aluno",
            $"Deseja realmente excluir o cadastro de {aluno.Nome}?",
            "Excluir", "Cancelar");
        if (!confirmar)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if (await _alunoService.RemoverAsync(aluno.Id, cts.Token))
            {
                _todosAlunos.RemoveAll(item => item.Id == aluno.Id);
                ApplySearch();
                await Shell.Current.DisplayAlertAsync("Cadastro removido", "O aluno foi excluído.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Não encontrado", "O cadastro já não existe ou não pôde ser removido.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo esgotado", "A exclusão do aluno excedeu o tempo limite.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível excluir o aluno: {ex.Message}", "OK");
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
            ? _todosAlunos
            : _todosAlunos.Where(aluno =>
                aluno.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                aluno.Cpf.Contains(termo, StringComparison.OrdinalIgnoreCase));

        Alunos.Clear();
        foreach (var aluno in resultado)
            Alunos.Add(aluno);
    }
}
