namespace MNISTSample.Views;

using Avalonia.Controls;
using MNISTSample.Contracts.Views;

public partial class MainWindow : Window, IMainView
{
    public MainWindow()
    {
        InitializeComponent(true);
    }
}