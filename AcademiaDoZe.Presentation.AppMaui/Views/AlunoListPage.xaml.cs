using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class AlunoListPage : ContentPage
{
    public AlunoListPage(AlunoListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AlunoListViewModel viewModel)
            await viewModel.LoadAlunosCommand.ExecuteAsync(null);
    }

    private async void OnEditButtonClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: AlunoDto aluno } &&
            BindingContext is AlunoListViewModel viewModel)
            await viewModel.EditAlunoCommand.ExecuteAsync(aluno);
    }

    private async void OnDeleteButtonClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: AlunoDto aluno } &&
            BindingContext is AlunoListViewModel viewModel)
            await viewModel.DeleteAlunoCommand.ExecuteAsync(aluno);
    }
}
