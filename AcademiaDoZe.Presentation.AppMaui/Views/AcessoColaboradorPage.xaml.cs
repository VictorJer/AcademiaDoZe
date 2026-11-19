using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class AcessoColaboradorPage : ContentPage
{
    public AcessoColaboradorPage(AcessoColaboradorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AcessoColaboradorViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
