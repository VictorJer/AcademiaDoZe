namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class MatriculaPage : ContentPage
{
    public MatriculaPage(ViewModels.MatriculaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ViewModels.MatriculaViewModel viewModel)
        {
            await viewModel.InitializeAsync();
            viewModel.RecalcularDataFim();
        }
    }

    private void OnPlanoChanged(object? sender, EventArgs e)
    {
        if (BindingContext is ViewModels.MatriculaViewModel viewModel)
            viewModel.RecalcularDataFim();
    }

    private void OnInicioChanged(object? sender, DateChangedEventArgs e)
    {
        if (BindingContext is ViewModels.MatriculaViewModel viewModel)
            viewModel.RecalcularDataFim();
    }
}
