using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using kiwiprojekt.tourbox.ui.Models;
using kiwiprojekt.tourbox.ui.ViewModels;

namespace kiwiprojekt.tourbox.ui;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    /// <summary>
    /// When true, the window actually closes instead of hiding.
    /// Set by App on shutdown.
    /// </summary>
    public bool ForcesClose { get; set; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Toggle connect/disconnect button visibility
        UpdateButtonVisibility();
        _viewModel.Device.PropertyChanged += (_, _) => UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        var connected = _viewModel.Device.ConnectionState == Models.ConnectionState.Connected;
        BtnConnect.Visibility = connected ? Visibility.Collapsed : Visibility.Visible;
        BtnDisconnect.Visibility = connected ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!ForcesClose)
        {
            // Hide instead of close — tray app
            e.Cancel = true;
            Hide();
        }
    }

    private void Device_ControlClicked(string controlName)
    {
        _viewModel.EditControlMapping(controlName);
    }

    private void MappingList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.ListView listView && listView.SelectedItem is MappingRow row)
        {
            _viewModel.EditControlMapping(row.ControlName);
        }
    }
}
