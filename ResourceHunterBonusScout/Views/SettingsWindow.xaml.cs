using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using ResourceHunterBonusScout.ViewModels;

namespace ResourceHunterBonusScout.Views;

public partial class SettingsWindow : Window
{
    readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = this._viewModel = viewModel;
        // PasswordBox.Password can't be bound, so keep it in step by hand.
        this.ApiKeyPasswordBox.Password = viewModel.ApiKey;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.CloseRequested += (_, _) => Close();
    }

    void OnPasswordChanged(object sender, RoutedEventArgs e) => this._viewModel.ApiKey = this.ApiKeyPasswordBox.Password;

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.ApiKey) && this.ApiKeyPasswordBox.Password != this._viewModel.ApiKey)
        {
            this.ApiKeyPasswordBox.Password = this._viewModel.ApiKey;
        }
    }

    void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
