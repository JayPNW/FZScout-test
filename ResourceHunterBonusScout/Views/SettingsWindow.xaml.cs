using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using ResourceHunterBonusScout.ViewModels;

namespace ResourceHunterBonusScout.Views;

public partial class SettingsWindow : Window
{
    readonly SettingsViewModel viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = this.viewModel = viewModel;
        // PasswordBox.Password can't be bound, so keep it in step by hand.
        ApiKeyPasswordBox.Password = viewModel.ApiKey;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.CloseRequested += (_, _) => Close();
    }

    void OnPasswordChanged(object sender, RoutedEventArgs e) => viewModel.ApiKey = ApiKeyPasswordBox.Password;

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.ApiKey) && ApiKeyPasswordBox.Password != viewModel.ApiKey)
            ApiKeyPasswordBox.Password = viewModel.ApiKey;
    }

    void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
