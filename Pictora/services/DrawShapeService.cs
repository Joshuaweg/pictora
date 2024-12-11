using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;

namespace Pictora.Services
{
    public class Shape
    {
        public string Type { get; set; }  // "rectangle", "ellipse", "triangle"
        public RectF Bounds { get; set; }
        public Color ShapeColor { get; set; }
        public bool IsSelected { get; set; }
    }

    public class DrawShapeService
    {
        private ObservableCollection<Shape> _shapes;
        private Shape _currentShape;
        private PointF _startPoint;
        private bool _isDrawing;

        public ObservableCollection<Shape> Shapes => _shapes;

        public DrawShapeService()
        {
            _shapes = new ObservableCollection<Shape>();
            _isDrawing = false;
        }

        public void StartDrawing(string shapeType, PointF startPoint, Color color)
        {
            _isDrawing = true;
            _startPoint = startPoint;
            _currentShape = new Shape
            {
                Type = shapeType,
                ShapeColor = color,
                Bounds = new RectF(startPoint.X, startPoint.Y, 0, 0)
            };
            _shapes.Add(_currentShape);
        }

        public void UpdateDrawing(PointF currentPoint)
        {
            if (!_isDrawing || _currentShape == null) return;

            float left = Math.Min(_startPoint.X, currentPoint.X);
            float top = Math.Min(_startPoint.Y, currentPoint.Y);
            float width = Math.Abs(currentPoint.X - _startPoint.X);
            float height = Math.Abs(currentPoint.Y - _startPoint.Y);

            _currentShape.Bounds = new RectF(left, top, width, height);
        }

        public void EndDrawing()
        {
            _isDrawing = false;
            _currentShape = null;
        }

        public void UpdateShapeColor(int shapeIndex, Color newColor)
        {
            if (shapeIndex >= 0 && shapeIndex < _shapes.Count)
            {
                _shapes[shapeIndex].ShapeColor = newColor;
            }
        }

        public void RemoveShape(int index)
        {
            if (index >= 0 && index < _shapes.Count)
            {
                _shapes.RemoveAt(index);
            }
        }

        public void ClearShapes()
        {
            _shapes.Clear();
        }

        public Shape GetShapeAtPoint(PointF point)
        {
            for (int i = _shapes.Count - 1; i >= 0; i--)
            {
                var shape = _shapes[i];
                if (shape.Bounds.Contains(point))
                {
                    return shape;
                }
            }
            return null;
        }
    }

    public class ShapeDrawable : IDrawable
    {
        private readonly ObservableCollection<Shape> _shapes;

        public ShapeDrawable(ObservableCollection<Shape> shapes)
        {
            _shapes = shapes;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            foreach (var shape in _shapes)
            {
                canvas.StrokeColor = shape.ShapeColor;
                canvas.StrokeSize = 2;
                canvas.FillColor = shape.ShapeColor.WithAlpha(0.3f);

                switch (shape.Type.ToLower())
                {
                    case "rectangle":
                        canvas.DrawRectangle(shape.Bounds);
                        canvas.FillRectangle(shape.Bounds);
                        break;

                    case "ellipse":
                        canvas.DrawEllipse(shape.Bounds);
                        canvas.FillEllipse(shape.Bounds);
                        break;

                    case "triangle":
                        var points = new PathF();
                        points.MoveTo(shape.Bounds.Left + shape.Bounds.Width / 2, shape.Bounds.Top);
                        points.LineTo(shape.Bounds.Left, shape.Bounds.Bottom);
                        points.LineTo(shape.Bounds.Right, shape.Bounds.Bottom);
                        points.Close();
                        canvas.DrawPath(points);
                        canvas.FillPath(points);
                        break;
                }

                if (shape.IsSelected)
                {
                    canvas.StrokeColor = Colors.White;
                    canvas.StrokeSize = 1;
                    canvas.DrawRectangle(shape.Bounds);
                }
            }
        }
    }
}