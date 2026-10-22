using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(ColaboradorId), "Id")]
public partial class ColaboradorViewModel : BaseViewModel
{
    private readonly IColaboradorService _colaboradorService;
    private readonly ILogradouroService _logradouroService;
    private int _colaboradorId;
    private bool _initialized;
    private DateTime _dataNascimento = DateTime.Today.AddYears(-18);
    private DateTime _dataAdmissao = DateTime.Today;
    private LogradouroDto? _selectedEndereco;
    private string _tipoSelecionado = "Instrutor";
    private string _vinculoSelecionado = "CLT";

    public ObservableCollection<LogradouroDto> Enderecos { get; } = [];
    public ObservableCollection<string> Tipos { get; } = ["Administrador", "Atendente", "Instrutor"];
    public ObservableCollection<string> Vinculos { get; } = ["CLT", "Estagiário"];

    public ColaboradorDto Colaborador { get; private set; } = CriarColaboradorVazio();

    public int ColaboradorId
    {
        get => _colaboradorId;
        set => SetProperty(ref _colaboradorId, value);
    }

    public DateTime DataNascimento
    {
        get => _dataNascimento;
        set => SetProperty(ref _dataNascimento, value);
    }

    public DateTime DataAdmissao
    {
        get => _dataAdmissao;
        set => SetProperty(ref _dataAdmissao, value);
    }

    public LogradouroDto? SelectedEndereco
    {
        get => _selectedEndereco;
        set => SetProperty(ref _selectedEndereco, value);
    }

    public string TipoSelecionado
    {
        get => _tipoSelecionado;
        set => SetProperty(ref _tipoSelecionado, value);
    }

    public string VinculoSelecionado
    {
        get => _vinculoSelecionado;
        set => SetProperty(ref _vinculoSelecionado, value);
    }

    public bool IsEditMode => ColaboradorId > 0;
    public bool IsCpfEditable => !IsEditMode;

    public ColaboradorViewModel(IColaboradorService colaboradorService, ILogradouroService logradouroService)
    {
        _colaboradorService = colaboradorService;
        _logradouroService = logradouroService;
        Title = "Novo colaborador";
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var enderecos = await _logradouroService.ObterTodosAsync(cts.Token);
            Enderecos.Clear();
            foreach (var endereco in enderecos)
                Enderecos.Add(endereco);

            if (IsEditMode)
            {
                var colaborador = await _colaboradorService.ObterPorIdAsync(ColaboradorId, cts.Token)
                    ?? throw new KeyNotFoundException("O colaborador solicitado não foi encontrado.");
                Colaborador = colaborador;
                DataNascimento = colaborador.DataNascimento.ToDateTime(TimeOnly.MinValue);
                DataAdmissao = colaborador.DataAdmissao.ToDateTime(TimeOnly.MinValue);
                TipoSelecionado = NomeTipo(colaborador.Tipo);
                VinculoSelecionado = NomeVinculo(colaborador.Vinculo);
                SelectedEndereco = Enderecos.FirstOrDefault(endereco => endereco.Id == colaborador.Endereco?.Id);
                Title = "Editar colaborador";
                OnPropertyChanged(nameof(Colaborador));
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsCpfEditable));
            }
            else
            {
                Colaborador = CriarColaboradorVazio();
                DataNascimento = DateTime.Today.AddYears(-18);
                DataAdmissao = DateTime.Today;
                TipoSelecionado = "Instrutor";
                VinculoSelecionado = "CLT";
                SelectedEndereco = null;
                Title = "Novo colaborador";
                OnPropertyChanged(nameof(Colaborador));
                OnPropertyChanged(nameof(IsCpfEditable));
            }

            if (Enderecos.Count == 0)
                await Shell.Current.DisplayAlertAsync("Endereço necessário", "Cadastre um logradouro antes de incluir colaboradores.", "OK");

            _initialized = true;
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo esgotado", "Não foi possível carregar os dados do formulário no tempo limite.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível preparar o cadastro do colaborador: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task SaveColaboradorAsync()
    {
        if (IsBusy)
            return;
        var email = Colaborador.Email;
        if (string.IsNullOrWhiteSpace(Colaborador.Nome) ||
            string.IsNullOrWhiteSpace(Colaborador.Cpf) ||
            string.IsNullOrWhiteSpace(Colaborador.Telefone) ||
            string.IsNullOrWhiteSpace(email))
        {
            await Shell.Current.DisplayAlertAsync("Dados obrigatórios", "Preencha nome, CPF, telefone e e-mail.", "OK");
            return;
        }
        if (SelectedEndereco == null)
        {
            await Shell.Current.DisplayAlertAsync("Endereço obrigatório", "Selecione um logradouro para o colaborador.", "OK");
            return;
        }
        if (!IsEditMode && string.IsNullOrWhiteSpace(Colaborador.Senha))
        {
            await Shell.Current.DisplayAlertAsync("Senha obrigatória", "Informe uma senha para o novo colaborador.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            Colaborador.Endereco = SelectedEndereco;
            Colaborador.DataNascimento = DateOnly.FromDateTime(DataNascimento);
            Colaborador.DataAdmissao = DateOnly.FromDateTime(DataAdmissao);
            Colaborador.Tipo = Enum.Parse<AppColaboradorTipo>(TipoSelecionado);
            Colaborador.Vinculo = VinculoSelecionado == "Estagiário"
                ? AppColaboradorVinculo.Estagio
                : AppColaboradorVinculo.CLT;
            Colaborador.Nome = Colaborador.Nome.Trim();
            Colaborador.Cpf = Colaborador.Cpf.Trim();
            Colaborador.Telefone = Colaborador.Telefone.Trim();
            Colaborador.Email = email.Trim();
            Colaborador.Numero = Colaborador.Numero.Trim();
            Colaborador.Complemento = Colaborador.Complemento?.Trim();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if (IsEditMode)
            {
                await _colaboradorService.AtualizarAsync(Colaborador, cts.Token);
                await Shell.Current.DisplayAlertAsync("Cadastro atualizado", "Os dados do colaborador foram salvos.", "OK");
            }
            else
            {
                await _colaboradorService.AdicionarAsync(Colaborador, cts.Token);
                await Shell.Current.DisplayAlertAsync("Colaborador cadastrado", "O cadastro foi realizado com sucesso.", "OK");
            }
            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo esgotado", "A gravação do cadastro excedeu o tempo limite.", "OK");
        }
        catch (ArgumentException ex)
        {
            await Shell.Current.DisplayAlertAsync("Validação", ex.Message, "OK");
        }
        catch (InvalidOperationException ex)
        {
            await Shell.Current.DisplayAlertAsync("Regra de negócio", ex.Message, "OK");
        }
        catch (KeyNotFoundException ex)
        {
            await Shell.Current.DisplayAlertAsync("Cadastro não encontrado", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Não foi possível salvar o cadastro do colaborador: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static ColaboradorDto CriarColaboradorVazio() => new()
    {
        Nome = string.Empty,
        Cpf = string.Empty,
        DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
        Telefone = string.Empty,
        Email = string.Empty,
        Numero = string.Empty,
        Complemento = string.Empty,
        DataAdmissao = DateOnly.FromDateTime(DateTime.Today),
        Tipo = AppColaboradorTipo.Instrutor,
        Vinculo = AppColaboradorVinculo.CLT
    };

    private static string NomeTipo(AppColaboradorTipo tipo) => tipo switch
    {
        AppColaboradorTipo.Administrador => "Administrador",
        AppColaboradorTipo.Atendente => "Atendente",
        _ => "Instrutor"
    };

    private static string NomeVinculo(AppColaboradorVinculo vinculo) =>
        vinculo == AppColaboradorVinculo.Estagio ? "Estagiário" : "CLT";
}
