using Microsoft.Maui.Graphics;
using PointFt = Microsoft.Maui.Graphics.PointF;

namespace Pictora.Components
{
    public class MaskDrawable : IDrawable
    {
        private readonly List<List<PointFt>> _paths;
        private const float STROKE_WIDTH = 20f;

        public MaskDrawable(List<List<PointFt>> paths)
        {
            _paths = paths;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_paths == null || !_paths.Any()) return;

            // Set up drawing parameters
            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = STROKE_WIDTH;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.FillColor = Colors.White;

            foreach (var path in _paths)
            {
                if (path.Count < 2) continue;

                var pathF = new PathF();
                pathF.MoveTo(path[0].X, path[0].Y);

                for (int i = 1; i < path.Count; i++)
                {
                    pathF.LineTo(path[i].X, path[i].Y);
                }

                canvas.DrawPath(pathF);

                // Draw dots at each point for better visibility
                foreach (var point in path)
                {
                    canvas.FillCircle(point.X, point.Y, STROKE_WIDTH / 2);
                }
            }
        }
    }
}