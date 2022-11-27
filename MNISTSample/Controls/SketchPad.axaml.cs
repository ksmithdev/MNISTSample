namespace MNISTSample.Controls;

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

public partial class SketchPad : UserControl
{
    public static readonly DirectProperty<SketchPad, float[]> SketchProperty = AvaloniaProperty.RegisterDirect<SketchPad, float[]>(nameof(Sketch), c => c.Sketch, (c, v) => c.Sketch = v);
    private const int GRID_HEIGHT = 28;
    private const int GRID_LENGTH = GRID_WIDTH * GRID_HEIGHT;
    private const int GRID_WIDTH = 28;
    private readonly Rectangle[] rectangles = new Rectangle[GRID_LENGTH];
    private Point? lastPosition;

    public SketchPad()
    {
        InitializeComponent(true);

        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                GetRectangle(x, y) = new Rectangle { Width = 1, Height = 1, Opacity = 0.0, Fill = Brushes.Black };
                GetRectangle(x, y).SetValue(Canvas.LeftProperty, x);
                GetRectangle(x, y).SetValue(Canvas.TopProperty, y);
            }
        }

        Drawing.Children.AddRange(rectangles);
    }

    public float[] Sketch
    {
        get => rectangles.Select(r => (float)r.Opacity).ToArray();
        set
        {
            if (value?.Length != GRID_LENGTH)
            {
                return;
            }

            for (int i = 0; i < GRID_LENGTH; i++)
            {
                rectangles[i].Opacity = value[i];
            }
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var availableSize = base.ArrangeOverride(finalSize);
        double width = availableSize.Width / GRID_WIDTH;
        double height = availableSize.Height / GRID_HEIGHT;
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                GetRectangle(x, y).Width = width;
                GetRectangle(x, y).Height = height;
                GetRectangle(x, y).SetValue(Canvas.LeftProperty, x * width);
                GetRectangle(x, y).SetValue(Canvas.TopProperty, y * height);
            }
        }
        Drawing.Width = availableSize.Width;
        Drawing.Height = availableSize.Height;
        return availableSize;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        UpdateSketch(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        UpdateSketch(e);
    }

    private ref Rectangle GetRectangle(int x, int y) => ref rectangles[x + y * GRID_WIDTH];

    private void UpdateSketch(PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var position = point.Position;

        double width = Drawing.Width / GRID_WIDTH;
        double height = Drawing.Height / GRID_HEIGHT;
        int x = (int)(position.X / width);
        int y = (int)(position.Y / height);

        if (lastPosition == new Point(x, y)) return;

        lastPosition = new Point(x, y);

        static bool isValid(int x, int y) => x >= 0 && y >= 0 && x < GRID_WIDTH && y < GRID_HEIGHT;

        if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
        {
            for (int xOffset = x - 1; xOffset <= x + 1; xOffset++)
            {
                for (int yOffset = y - 1; yOffset <= y + 1; yOffset++)
                {
                    if (!isValid(xOffset, yOffset)) continue;

                    Rectangle rect = GetRectangle(xOffset, yOffset);
                    double value = rect.Opacity;
                    if (point.Properties.IsLeftButtonPressed)
                    {
                        value += 0.5;
                    }
                    else
                    {
                        value -= 0.5;
                    }
                    rect.Opacity = Math.Clamp(value, 0.0, 1.0);
                }
            }
            RaisePropertyChanged(SketchProperty, Optional<float[]>.Empty, Sketch);
        }
    }
}