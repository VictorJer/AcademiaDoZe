using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(MatriculaId), "Id")]
public partial class MatriculaViewModel : BaseViewModel
{
    private readonly IMatriculaService _matriculaService;
    private readonly IAlunoService _alunoService;
    private bool _syncing;
    private int _matriculaId;
    private bool _initialized;

    public IEnumerable<AppMatriculaPlano> PlanoTipos { get; } = Enum.GetValues<AppMatriculaPlano>();
    public ObservableCollection<RestricaoSelectable> RestricoesMulti { get; } = [];
    public MatriculaDto Matricula { get; private set; } = CriarMatriculaVazia();

    public int MatriculaId
    {
        get => _matriculaId;
        set => SetProperty(ref _matriculaId, value);
    }

    public bool IsEditMode => MatriculaId > 0;
    public bool HasAlunoVinculado => Matricula.AlunoMatricula.Id > 0;
    public bool HasNoAlunoVinculado => !HasAlunoVinculado;
    public bool IsMenor16 => Matricula.AlunoMatricula.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-16));
    public bool HasRestricoes => Matricula.RestricoesMedicas != AppMatriculaRestricoes.None;
    public bool ExigeLaudo => IsMenor16 || HasRestricoes;
    public bool HasLaudo => Matricula.LaudoMedico?.Conteudo?.Length > 0;
    public DateTime DataInicioDate
    {
        get => Matricula.DataInicio.ToDateTime(TimeOnly.MinValue);
        set
        {
            Matricula.DataInicio = DateOnly.FromDateTime(value);
            OnPropertyChanged(nameof(DataInicioDate));
        }
    }

    public string DataFimDisplay => Matricula.DataFim.ToString("dd/MM/yyyy");

    public MatriculaViewModel(IMatriculaService matriculaService, IAlunoService alunoService)
    {
        _matriculaService = matriculaService;
        _alunoService = alunoService;
        foreach (var value in Enum.GetValues<AppMatriculaRestricoes>().Where(value => value != AppMatriculaRestricoes.None))
        {
            var item = new RestricaoSelectable { Value = value };
            item.PropertyChanged += RestricaoChanged;
            RestricoesMulti.Add(item);
        }
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;
        if (IsEditMode)
        {
            var loaded = await _matriculaService.ObterPorIdAsync(MatriculaId)
                ?? throw new KeyNotFoundException($"Matrícula {MatriculaId} não encontrada.");
            Matricula = loaded;
            Title = "Editar matrícula";
        }
        else
        {
            Title = "Nova matrícula";
        }
        SyncFlags();
        NotifyState();
        _initialized = true;
    }

    [RelayCommand]
    private async Task SearchByCpfAsync()
    {
        var cpf = new string((Matricula.AlunoMatricula.Cpf ?? string.Empty).Where(char.IsDigit).ToArray());
        if (cpf.Length == 0)
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Informe o CPF do aluno.", "OK");
            return;
        }
        try
        {
            IsBusy = true;
            var aluno = await _alunoService.ObterPorCpfAsync(cpf);
            if (aluno is null)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Aluno não encontrado.", "OK");
                return;
            }
            Matricula.AlunoMatricula = aluno;
            OnPropertyChanged(nameof(Matricula));
            NotifyState();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível buscar o aluno: {ex.Message}", "OK");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveMatriculaAsync()
    {
        if (IsBusy)
            return;
        RecomputeFlags();
        if (!Validate())
            return;
        try
        {
            IsBusy = true;
            if (IsEditMode)
                await _matriculaService.AtualizarAsync(Matricula);
            else
                await _matriculaService.AdicionarAsync(Matricula);
            await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula salva com sucesso.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível salvar a matrícula: {ex.Message}", "OK");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private void RemoverLaudo()
    {
        Matricula.LaudoMedico = null;
        Matricula.RemoverLaudo = true;
        OnPropertyChanged(nameof(Matricula));
        NotifyState();
    }

    [RelayCommand]
    private async Task SelecionarLaudoAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecione o laudo médico",
                FileTypes = FilePickerFileType.Images
            });
            if (result is null)
                return;
            await using var stream = await result.OpenReadAsync();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            Matricula.LaudoMedico = new ArquivoDto { Conteudo = memory.ToArray() };
            Matricula.RemoverLaudo = false;
            OnPropertyChanged(nameof(Matricula));
            NotifyState();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível selecionar o laudo: {ex.Message}", "OK");
        }
    }

    private bool Validate()
    {
        var errors = new List<string>();
        if (!HasAlunoVinculado) errors.Add("Selecione um aluno pelo CPF.");
        if (Matricula.DataInicio == default) errors.Add("Informe a data de início.");
        if (string.IsNullOrWhiteSpace(Matricula.Objetivo)) errors.Add("Informe o objetivo da matrícula.");
        if (ExigeLaudo && !HasLaudo) errors.Add("O laudo médico é obrigatório para este aluno.");
        if (errors.Count == 0) return true;
        Shell.Current.DisplayAlertAsync("Dados inválidos", string.Join(Environment.NewLine, errors), "OK");
        return false;
    }

    private void RestricaoChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (!_syncing && args.PropertyName == nameof(RestricaoSelectable.IsSelected))
            RecomputeFlags();
    }

    private void RecomputeFlags()
    {
        Matricula.RestricoesMedicas = RestricoesMulti
            .Where(item => item.IsSelected)
            .Aggregate(AppMatriculaRestricoes.None, (flags, item) => flags | item.Value);
        OnPropertyChanged(nameof(Matricula));
        NotifyState();
    }

    public void RecalcularDataFim()
    {
        var inicio = Matricula.DataInicio;
        Matricula.DataFim = Matricula.Plano switch
        {
            AppMatriculaPlano.Mensal => inicio.AddMonths(1),
            AppMatriculaPlano.Trimestral => inicio.AddMonths(3),
            AppMatriculaPlano.Semestral => inicio.AddMonths(6),
            AppMatriculaPlano.Anual => inicio.AddYears(1),
            _ => inicio
        };
        OnPropertyChanged(nameof(Matricula));
        OnPropertyChanged(nameof(DataFimDisplay));
    }

    private void SyncFlags()
    {
        _syncing = true;
        try
        {
            foreach (var item in RestricoesMulti)
                item.IsSelected = Matricula.RestricoesMedicas.HasFlag(item.Value);
        }
        finally { _syncing = false; }
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasAlunoVinculado));
        OnPropertyChanged(nameof(HasNoAlunoVinculado));
        OnPropertyChanged(nameof(IsMenor16));
        OnPropertyChanged(nameof(HasRestricoes));
        OnPropertyChanged(nameof(ExigeLaudo));
        OnPropertyChanged(nameof(HasLaudo));
    }

    private static MatriculaDto CriarMatriculaVazia() => new()
    {
        AlunoMatricula = new AlunoDto
        {
            Nome = string.Empty,
            Cpf = string.Empty,
            DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
            Telefone = string.Empty,
            Numero = string.Empty
        },
        Plano = AppMatriculaPlano.Anual,
        DataInicio = DateOnly.FromDateTime(DateTime.Today),
        DataFim = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
        Objetivo = string.Empty,
        RestricoesMedicas = AppMatriculaRestricoes.None
    };
}
