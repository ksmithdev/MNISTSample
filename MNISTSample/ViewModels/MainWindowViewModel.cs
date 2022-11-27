namespace MNISTSample.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MNISTSample.Contracts.Services;
using TurtleML;
using TurtleML.Activations;
using TurtleML.Initializers;
using TurtleML.Layers;
using TurtleML.LearningPolicies;
using TurtleML.Loss;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IFileDialogService fileDialogService;
    private readonly TrainingNetwork trainingNetwork;
    private string guess = string.Empty;
    private InferenceNetwork? inferenceNetwork;
    private string result = string.Empty;
    private bool training = false;

    public MainWindowViewModel(IFileDialogService fileDialogService)
    {
        ClearCommand = new RelayCommand(OnClear);
        StopCommand = new RelayCommand(OnStop, () => training);
        TestCommand = new RelayCommand(OnTest, () => !training);
        TrainCommand = new AsyncRelayCommand(OnTrain, () => !training);
        SaveCommand = new AsyncRelayCommand(OnSave, () => !training);
        RestoreCommand = new AsyncRelayCommand(OnRestore, () => !training);
        TrainingOutput = new ObservableCollection<string>();

        trainingNetwork = new TrainingNetwork.Builder()
            .InputShape(28, 28, 1)
            .Layers(
                new ConvoluteLayer.Builder().Initializer(new HeInitializer()).Activation(new LeakyReLUActivation()).Filters(3, 3, 1, 12),
                new MaxPoolingLayer.Builder().Sample(2, 2),
                new ConvoluteLayer.Builder().Initializer(new HeInitializer()).Activation(new LeakyReLUActivation()).Filters(3, 3, 1, 24),
                new MaxPoolingLayer.Builder().Sample(2, 2),
                new ConvoluteLayer.Builder().Initializer(new HeInitializer()).Activation(new LeakyReLUActivation()).Filters(3, 3, 1, 48),
                new MaxPoolingLayer.Builder().Sample(2, 2),
                new DropOutLayer.Builder().DropOut(0.2f),
                new FullyConnectedLayer.Builder().Initializer(new HeInitializer()).Activation(new LeakyReLUActivation()).Outputs(128),
                new FullyConnectedLayer.Builder().Initializer(new HeInitializer()).Activation(new LeakyReLUActivation()).Outputs(10),
                new SoftMaxOutputLayer.Builder()
            )
            .Shuffle(true)
            .Loss(new CrossEntropyLoss())
            .LearningPolicy(new TimeDecayLearningPolicy(0.01f, 0.95f))
            .Build();

        trainingNetwork.TrainingProgress += (object? sender, TrainingProgressEventArgs e) =>
        {
            TrainingOutput.Add($"{e.Epoch}: {e.CycleTime}ms - {e.TrainingError:0.000} training error, {e.ValidationError:0.000} validation error ({e.LearningRate} learning rate)");
        };

        PropertyChanged += MainWindowViewModel_PropertyChanged;
        this.fileDialogService = fileDialogService;
    }

    public ICommand ClearCommand { get; }

    public string Guess
    {
        get => guess;
        set => SetProperty(ref guess, value);
    }

    public AsyncRelayCommand RestoreCommand { get; }

    public string Result
    {
        get => result;
        set => SetProperty(ref result, value);
    }

    public AsyncRelayCommand SaveCommand { get; }

    public float[] Sketch { get; set; } = new float[28 * 28];

    public RelayCommand StopCommand { get; }

    public RelayCommand TestCommand { get; }

    public AsyncRelayCommand TrainCommand { get; }

    public bool Training
    {
        get => training;
        set => SetProperty(ref training, value);
    }

    public ObservableCollection<string> TrainingOutput { get; }

    private static TensorSet LoadTrainingSet()
    {
        var images = new List<float[]>();
        foreach (var image in ReadImages(@".\DataSets\train-images-idx3-ubyte.gz"))
        {
            images.Add(image);
        }

        var labels = new List<byte>();
        foreach (var label in ReadLabels(@".\DataSets\train-labels-idx1-ubyte.gz"))
        {
            labels.Add(label);
        }

        var trainingSet = new TensorSet();
        for (int i = 0; i < labels.Count; i++)
        {
            var input = images[i];
            var output = new float[10];
            output[labels[i]] = 1.0f;

            trainingSet.Add(Tensor.Wrap(input).Reshape(28, 28, 1), Tensor.Wrap(output));
        }

        images.Clear();
        labels.Clear();

        return trainingSet;
    }

    private static TensorSet LoadValidationSet()
    {
        var images = new List<float[]>();
        foreach (var image in ReadImages(@".\DataSets\t10k-images-idx3-ubyte.gz"))
        {
            images.Add(image);
        }

        var labels = new List<byte>();
        foreach (var label in ReadLabels(@".\DataSets\t10k-labels-idx1-ubyte.gz"))
        {
            labels.Add(label);
        }

        var trainingSet = new TensorSet();
        for (int i = 0; i < labels.Count; i++)
        {
            var input = images[i];
            var output = new float[10];
            output[labels[i]] = 1.0f;

            trainingSet.Add(Tensor.Wrap(input).Reshape(28, 28, 1), Tensor.Wrap(output));
        }

        images.Clear();
        labels.Clear();

        return trainingSet;
    }

    private static IEnumerable<float[]> ReadImages(string fileName)
    {
        var source = new FileInfo(fileName);
        var stream = source.OpenRead();
        var decompressed = new GZipStream(stream, CompressionMode.Decompress);
        var reader = new BinaryReader(decompressed);

        static int ReadBigEndianInt32(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(sizeof(int));
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        var checksum = ReadBigEndianInt32(reader);
        if (checksum != 2051)
        {
            throw new InvalidOperationException();
        }

        int numberOfImages = ReadBigEndianInt32(reader);

        int width = ReadBigEndianInt32(reader);
        int height = ReadBigEndianInt32(reader);
        int length = width * height;

        var buffer = new byte[length];
        for (int i = 0; i < numberOfImages; i++)
        {
            yield return reader.ReadBytes(length).Select(b => b / 255.0f).ToArray();
        }
    }

    private static IEnumerable<byte> ReadLabels(string fileName)
    {
        var source = new FileInfo(fileName);
        var stream = source.OpenRead();
        var decompressed = new GZipStream(stream, CompressionMode.Decompress);
        var reader = new BinaryReader(decompressed);

        static int ReadBigEndianInt32(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(sizeof(int));
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        var checksum = ReadBigEndianInt32(reader);
        if (checksum != 2049)
        {
            throw new InvalidOperationException();
        }

        int numberOfImages = ReadBigEndianInt32(reader);

        for (int i = 0; i < numberOfImages; i++)
        {
            yield return reader.ReadByte();
        }
    }

    private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Training):
                StopCommand.NotifyCanExecuteChanged();
                TestCommand.NotifyCanExecuteChanged();
                TrainCommand.NotifyCanExecuteChanged();
                SaveCommand.NotifyCanExecuteChanged();
                RestoreCommand.NotifyCanExecuteChanged();
                break;
        }
    }

    private void OnClear()
    {
        OnPropertyChanging(nameof(Sketch));
        Array.Clear(Sketch);
        OnPropertyChanged(nameof(Sketch));
    }

    private async Task OnRestore()
    {
        var file = await fileDialogService.GetSourceFile();
        if (file != null)
        {
            inferenceNetwork = InferenceNetwork.Restore(file);
        }
    }

    private async Task OnSave()
    {
        var file = await fileDialogService.GetDestinationFile();
        if (file != null)
        {
            trainingNetwork.Dump(file);
        }
    }

    private void OnStop()
    {
        trainingNetwork.Abort();
    }

    private void OnTest()
    {
        var input = Tensor.Create(Sketch)
            .Reshape(28, 28, 1);

        var network = inferenceNetwork ?? trainingNetwork;
        var output = network.CalculateOutputs(input);

        int digit = 0;
        float max = 0f;
        for (int i = 0; i < output.Length; i++)
        {
            if (output[i] < max)
                continue;

            max = output[i];
            digit = i;
        }

        Guess = $"{digit} ({max:0.00} confidence)";
    }

    private async Task OnTrain()
    {
        Training = true;
        TrainingOutput.Clear();

        await Task.Run(() =>
        {
            TensorSet trainingSet = LoadTrainingSet();
            TensorSet validationSet = LoadValidationSet();

            trainingNetwork.Fit(trainingSet, validationSet, 30);
        });

        Training = false;
    }
}