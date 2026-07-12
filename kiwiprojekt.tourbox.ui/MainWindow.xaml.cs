using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using kiwiprojekt.tourbox.ui.Converters;
using kiwiprojekt.tourbox.ui.Models;
using kiwiprojekt.tourbox.ui.ViewModels;

namespace kiwiprojekt.tourbox.ui;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public bool ForcesClose { get; set; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _vm = viewModel;
        DataContext = viewModel;

        UpdateButtonVisibility();
        _vm.Device.PropertyChanged += (_, _) => UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        var connected = _vm.Device.ConnectionState == Models.ConnectionState.Connected;
        BtnConnect.Visibility = connected ? Visibility.Collapsed : Visibility.Visible;
        BtnDisconnect.Visibility = connected ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!ForcesClose)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void Device_ControlClicked(string controlName)
    {
        _vm.SelectControl(controlName);
    }

    private void MappingList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MappingList.SelectedItem is MappingRow row)
        {
            _vm.SelectControl(row.ControlName);
        }
    }

    private void EditorSave_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveCurrentEditor();
    }
}
