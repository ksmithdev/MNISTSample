namespace MNISTSample;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MNISTSample.Contracts.Services;
using MNISTSample.Contracts.Views;
using MNISTSample.Services;
using MNISTSample.ViewModels;
using MNISTSample.Views;

public partial class App : Application, IFileDialogService
{
    public async Task<FileInfo?> GetDestinationFile()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save trained network file",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "TNN Files", Extensions = { "tnn" } },
                    new FileDialogFilter {Name = "All Files", Extensions = { "*" } },
                }
            };
            var result = await dialog.ShowAsync(desktop.MainWindow);
            if (result?.Length > 0)
            {
                return new FileInfo(result);
            }
        }
        return null;
    }

    public async Task<FileInfo?> GetSourceFile()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open trained network file",
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "TNN Files", Extensions = { "tnn" } },
                    new FileDialogFilter {Name = "All Files", Extensions = { "*" } },
                }
            };
            var result = await dialog.ShowAsync(desktop.MainWindow);
            if (result?.Length > 0)
            {
                return new FileInfo(result[0]);
            }
        }
        return null;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var serviceProvider = services.BuildServiceProvider();

            desktop.MainWindow = new MainWindow
            {
                DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<IFileDialogService>(this);
        // add view models and views
        services.AddTransientForSubtypes<ViewModelBase>();
        services.AddTransientForSubtypes<IView>();
    }
}