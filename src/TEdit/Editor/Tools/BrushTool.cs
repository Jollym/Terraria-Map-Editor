using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TEdit.Geometry;
using TEdit.Geometry.Primitives;
using TEdit.ViewModel;
using TEdit.Terraria.Objects;

namespace TEdit.Editor.Tools
{
    public sealed class BrushTool : BaseTool
    {
        private bool _isLeftDown;
        private bool _isRightDown;
        private Vector2Int32 _startPoint;
        private Vector2Int32 _endPoint;
        private Vector2Int32 _leftPoint;
        private Vector2Int32 _rightPoint;

        public BrushTool(WorldViewModel worldViewModel)
            : base(worldViewModel)
        {
            Icon = new BitmapImage(new Uri(@"pack://application:,,,/TEdit;component/Images/Tools/paintbrush.png"));
            Name = "Brush";
            ToolType = ToolType.Brush;
        }

        public override void MouseDown(TileMouseState e)
        {
            if (!_isRightDown && !_isLeftDown)
            {
                _startPoint = e.Location;
                _wvm.CheckTiles = new bool[_wvm.CurrentWorld.TilesWide * _wvm.CurrentWorld.TilesHigh];
            }

            _isLeftDown = (e.LeftButton == MouseButtonState.Pressed);
            _isRightDown = (e.RightButton == MouseButtonState.Pressed);

            if (_wvm.Brush.Shape == BrushShape.Square || _wvm.Brush.Height <= 1 || _wvm.Brush.Width <= 1)
            {
                FillRectangle(_startPoint);
            }

            CheckDirectionandDraw(e.Location);
        }

        public override void MouseMove(TileMouseState e)
        {
            _isLeftDown = (e.LeftButton == MouseButtonState.Pressed);
            _isRightDown = (e.RightButton == MouseButtonState.Pressed);
            CheckDirectionandDraw(e.Location);
        }

        public override void MouseUp(TileMouseState e)
        {
            CheckDirectionandDraw(e.Location);
            _isLeftDown = (e.LeftButton == MouseButtonState.Pressed);
            _isRightDown = (e.RightButton == MouseButtonState.Pressed);
            _wvm.UndoManager.SaveUndo();
        }

        public override WriteableBitmap PreviewTool()
        {
            var bmp = new WriteableBitmap(_wvm.Brush.Width + 1, _wvm.Brush.Height + 1, 96, 96, PixelFormats.Bgra32, null);

            bmp.Clear();
            if (_wvm.Brush.Shape == BrushShape.Square || _wvm.Brush.Height <= 1 || _wvm.Brush.Width <= 1)
                bmp.FillRectangle(0, 0, _wvm.Brush.Width, _wvm.Brush.Height, Color.FromArgb(127, 0, 90, 255));
            else if (_wvm.Brush.Shape == BrushShape.Left)
                bmp.DrawLine(0, 0, _wvm.Brush.Width, _wvm.Brush.Height, Color.FromArgb(127, 0, 90, 255));
            else if (_wvm.Brush.Shape == BrushShape.Right)
                bmp.DrawLine(0, _wvm.Brush.Height, _wvm.Brush.Width, 0, Color.FromArgb(127, 0, 90, 255));
            else
                bmp.FillEllipse(0, 0, _wvm.Brush.Width, _wvm.Brush.Height, Color.FromArgb(127, 0, 90, 255));

            _preview = bmp;
            return _preview;
        }

        private void CheckDirectionandDraw(Vector2Int32 tile)
        {
            Vector2Int32 p = tile;
            Vector2Int32 p2 = tile;
            if (_isRightDown)
            {
                if (_isLeftDown)
                    p.X = _startPoint.X;
                else
                    p.Y = _startPoint.Y;

                DrawLine(p);
                _startPoint = p;
            }
            else if (_isLeftDown)
            {
                if ((Keyboard.IsKeyUp(Key.LeftShift)) && (Keyboard.IsKeyUp(Key.RightShift)))
                {
                    DrawLine(p);
                    _startPoint = p;
                    _endPoint = p;
                }
                else if ((Keyboard.IsKeyDown(Key.LeftShift)) || (Keyboard.IsKeyDown(Key.RightShift)))
                {
                    DrawLineP2P(p2);
                    _endPoint = p2;
                }
            }
        }

        private void DrawLine(Vector2Int32 to)
        {
            var line = Shape.DrawLineTool(_startPoint, to).ToList();
            if (_wvm.Brush.Shape == BrushShape.Square || _wvm.Brush.Height <= 1 || _wvm.Brush.Width <= 1)
            {

                for (int i = 1; i < line.Count; i++)
                {
                    FillRectangleLine(line[i - 1], line[i]);
                }
            }
            else if (_wvm.Brush.Shape == BrushShape.Round)
            {
                foreach (Vector2Int32 point in line)
                {
                    FillRound(point);
                }
            }
            else if (_wvm.Brush.Shape == BrushShape.Right || _wvm.Brush.Shape == BrushShape.Left)
            {
                foreach (Vector2Int32 point in line)
                {
                    FillSlope(point);
                }
            }
        }

        private void DrawLineP2P(Vector2Int32 endPoint)
        {
            var line = Shape.DrawLineTool(_startPoint, endPoint).ToList();

            if (_wvm.Brush.Shape == BrushShape.Square || _wvm.Brush.Height <= 1 || _wvm.Brush.Width <= 1)
            {

                for (int i = 1; i < line.Count; i++)
                {
                    FillRectangleLine(line[i - 1], line[i]);
                }
            }
            else if (_wvm.Brush.Shape == BrushShape.Round)
            {
                foreach (Vector2Int32 point in line)
                {
                    FillRound(point);
                }
            }
            else if (_wvm.Brush.Shape == BrushShape.Right || _wvm.Brush.Shape == BrushShape.Left)
            {
                foreach (Vector2Int32 point in line)
                {
                    FillSlope(point);
                }
            }
        }

        private void FillRectangleLine(Vector2Int32 start, Vector2Int32 end)
        {
            IEnumerable<Vector2Int32> area = Fill.FillRectangleVectorCenter(start, end, new Vector2Int32(_wvm.Brush.Width, _wvm.Brush.Height)).ToList();
            FillSolid(area);
        }

        private void FillRectangle(Vector2Int32 point)
        {
            IEnumerable<Vector2Int32> area = Fill.FillRectangleCentered(point, new Vector2Int32(_wvm.Brush.Width, _wvm.Brush.Height));
            if (_wvm.Brush.IsOutline)
            {

                IEnumerable<Vector2Int32> interrior = Fill.FillRectangleCentered(point,
                                                                                 new Vector2Int32(
                                                                                     _wvm.Brush.Width - _wvm.Brush.Outline * 2,
                                                                                     _wvm.Brush.Height - _wvm.Brush.Outline * 2));
                FillHollow(area, interrior);
            }
            else
            {
                FillSolid(area);
            }
        }

        private void FillRound(Vector2Int32 point)
        {
            IEnumerable<Vector2Int32> area = Fill.FillEllipseCentered(point, new Vector2Int32(_wvm.Brush.Width / 2, _wvm.Brush.Height / 2));
            if (_wvm.Brush.IsOutline)
            {
                IEnumerable<Vector2Int32> interrior = Fill.FillEllipseCentered(point, new Vector2Int32(
                                                                                   _wvm.Brush.Width / 2 - _wvm.Brush.Outline * 2,
                                                                                   _wvm.Brush.Height / 2 - _wvm.Brush.Outline * 2));
                FillHollow(area, interrior);
            }
            else
            {
                FillSolid(area);
            }
        }

        private void FillSolid(IEnumerable<Vector2Int32> area)
        {
            foreach (Vector2Int32 pixel in area)
            {
                if (!_wvm.CurrentWorld.ValidTileLocation(pixel)) continue;

                int index = pixel.X + pixel.Y * _wvm.CurrentWorld.TilesWide;
                if (!_wvm.CheckTiles[index])
                {
                    _wvm.CheckTiles[index] = true;
                    if (_wvm.Selection.IsValid(pixel))
                    {
                        _wvm.UndoManager.SaveTile(pixel);
                        _wvm.SetPixel(pixel.X, pixel.Y);

                        /* Heathtech */
                        BlendRules.ResetUVCache(_wvm, pixel.X, pixel.Y, 1, 1);
                    }
                }
            }
        }

        private void FillHollow(IEnumerable<Vector2Int32> area, IEnumerable<Vector2Int32> interrior)
        {
            IEnumerable<Vector2Int32> border = area.Except(interrior);

            // Draw the border
            if (_wvm.TilePicker.TileStyleActive)
            {
                foreach (Vector2Int32 pixel in border)
                {
                    if (!_wvm.CurrentWorld.ValidTileLocation(pixel)) continue;

                    int index = pixel.X + pixel.Y * _wvm.CurrentWorld.TilesWide;

                    if (!_wvm.CheckTiles[index])
                    {
                        _wvm.CheckTiles[index] = true;
                        if (_wvm.Selection.IsValid(pixel))
                        {
                            _wvm.UndoManager.SaveTile(pixel);
                            if (_wvm.TilePicker.WallStyleActive)
                            {
                                _wvm.TilePicker.WallStyleActive = false;
                                _wvm.SetPixel(pixel.X, pixel.Y, mode: PaintMode.TileAndWall);
                                _wvm.TilePicker.WallStyleActive = true;
                            }
                            else
                                _wvm.SetPixel(pixel.X, pixel.Y, mode: PaintMode.TileAndWall);

                            /* Heathtech */
                            BlendRules.ResetUVCache(_wvm, pixel.X, pixel.Y, 1, 1);
                        }
                    }
                }
            }

            // Draw the wall in the interrior, exclude the border so no overlaps
            foreach (Vector2Int32 pixel in interrior)
            {
                if (!_wvm.CurrentWorld.ValidTileLocation(pixel)) continue;

                if (_wvm.Selection.IsValid(pixel))
                {
                    _wvm.UndoManager.SaveTile(pixel);
                    _wvm.SetPixel(pixel.X, pixel.Y, mode: PaintMode.TileAndWall, erase: true);

                    if (_wvm.TilePicker.WallStyleActive)
                    {
                        if (_wvm.TilePicker.TileStyleActive)
                        {
                            _wvm.TilePicker.TileStyleActive = false;
                            _wvm.SetPixel(pixel.X, pixel.Y, mode: PaintMode.TileAndWall);
                            _wvm.TilePicker.TileStyleActive = true;
                        }
                        else
                            _wvm.SetPixel(pixel.X, pixel.Y, mode: PaintMode.TileAndWall);
                    }

                    /* Heathtech */
                    BlendRules.ResetUVCache(_wvm, pixel.X, pixel.Y, 1, 1);
                }
            }
        }

        private void FillSlope(Vector2Int32 point)
        {
            if (_wvm.Brush.Shape == BrushShape.Right)
            {
                _leftPoint = new Vector2Int32(point.X - _wvm.Brush.Width / 2, point.Y + _wvm.Brush.Height / 2);
                _rightPoint = new Vector2Int32(point.X + _wvm.Brush.Width / 2, point.Y - _wvm.Brush.Height / 2);
            }
            else
            {
                _leftPoint = new Vector2Int32(point.X - _wvm.Brush.Width / 2, point.Y - _wvm.Brush.Height / 2);
                _rightPoint = new Vector2Int32(point.X + _wvm.Brush.Width / 2, point.Y + _wvm.Brush.Height / 2);
            }
            IEnumerable<Vector2Int32> area = Shape.DrawLine(_leftPoint, _rightPoint);
            foreach (Vector2Int32 pixel in area)
            {
                if (!_wvm.CurrentWorld.ValidTileLocation(pixel)) continue;

                int index = pixel.X + pixel.Y * _wvm.CurrentWorld.TilesWide;
                if (!_wvm.CheckTiles[index])
                {
                    _wvm.CheckTiles[index] = true;
                    if (_wvm.Selection.IsValid(pixel))
                    {
                        _wvm.UndoManager.SaveTile(pixel);
                        _wvm.SetPixel(pixel.X, pixel.Y);

                        /* Heathtech */
                        BlendRules.ResetUVCache(_wvm, pixel.X, pixel.Y, 1, 1);
                    }
                }
            }
        }
    }
}
