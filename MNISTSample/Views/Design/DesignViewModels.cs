namespace MNISTSample.Views.Design;

using System.IO;
using System.Threading.Tasks;
using MNISTSample.Contracts.Services;
using MNISTSample.ViewModels;

public class DesignMainWindowViewModel : MainWindowViewModel
{
    public DesignMainWindowViewModel()
        : base(new DesignFileDialogService())
    {
    }
}

public class DesignFileDialogService : IFileDialogService
{
    public Task<FileInfo?> GetDestinationFile() => Task.FromResult<FileInfo?>(null);

    public Task<FileInfo?> GetSourceFile() => Task.FromResult<FileInfo?>(null);
}