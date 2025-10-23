using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace WebViewWidget;

public sealed class AreaSparkline : FrameworkElement {
    public static readonly DependencyProperty PointsProperty =
        DependencyProperty.Register(nameof(Points), typeof(IList<Point>), typeof(AreaSparkline),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LineThicknessProperty =
        DependencyProperty.Register(nameof(LineThickness), typeof(double), typeof(AreaSparkline),
            new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LineBrushProperty =
        DependencyProperty.Register(nameof(LineBrush), typeof(Brush), typeof(AreaSparkline),
            new FrameworkPropertyMetadata(Brushes.DeepSkyBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillBrushProperty =
        DependencyProperty.Register(nameof(FillBrush), typeof(Brush), typeof(AreaSparkline),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(80, 30, 144, 255)),
                FrameworkPropertyMetadataOptions.AffectsRender));

    public IList<Point>? Points {
        get => (IList<Point>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public double LineThickness {
        get => (double)GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    public Brush LineBrush {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public Brush FillBrush {
        get => (Brush)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    protected override void OnRender(DrawingContext dc) {
        base.OnRender(dc);
        if (Points == null || Points.Count < 2) {
            return;
        }

        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) {
            return;
        }

        var minX = Points[0].X;
        var maxX = Points[^1].X;
        var minY = Points.Min(p => p.Y);
        var maxY = Points.Max(p => p.Y);
        var dx = Math.Max(1e-9, maxX - minX);
        var dy = Math.Max(1e-9, maxY - minY);

        Point Map(Point p) {
            return new Point(
                (p.X - minX) / dx * w,
                h - (p.Y - minY) / dy * h
            );
        }

        var mapped = Points.Select(Map).ToList();

        var geo = new StreamGeometry { FillRule = FillRule.Nonzero };
        using (var ctx = geo.Open()) {
            ctx.BeginFigure(new Point(mapped[0].X, h), true, true);
            WriteSmooth(ctx, mapped);
            ctx.LineTo(new Point(mapped[^1].X, h), true, false);
        }

        dc.DrawGeometry(FillBrush, null, geo);

        var lineGeo = new StreamGeometry();
        using (var ctx = lineGeo.Open()) {
            ctx.BeginFigure(mapped[0], false, false);
            WriteSmooth(ctx, mapped);
        }

        dc.DrawGeometry(null, new Pen(LineBrush, LineThickness) { LineJoin = PenLineJoin.Round }, lineGeo);
    }

    private static void WriteSmooth(StreamGeometryContext ctx, IList<Point> pts) {
        for (var i = 1; i < pts.Count; i++) {
            var p0 = pts[i - 1];
            var p1 = pts[i];
            var t = 0.2;
            var prev = i - 2 >= 0 ? pts[i - 2] : p0;
            var next = i + 1 < pts.Count ? pts[i + 1] : p1;

            var c1 = new Point(p0.X + t * (p1.X - prev.X), p0.Y + t * (p1.Y - prev.Y));
            var c2 = new Point(p1.X - t * (next.X - p0.X), p1.Y - t * (next.Y - p0.Y));

            ctx.BezierTo(c1, c2, p1, true, false);
        }
    }

    protected override Size MeasureOverride(Size availableSize) {
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize) {
        return finalSize;
    }
}