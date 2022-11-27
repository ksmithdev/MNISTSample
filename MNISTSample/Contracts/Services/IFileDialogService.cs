namespace MNISTSample.Contracts.Services;

using System.IO;
using System.Threading.Tasks;

public interface IFileDialogService
{
    Task<FileInfo?> GetDestinationFile();

    Task<FileInfo?> GetSourceFile();
}