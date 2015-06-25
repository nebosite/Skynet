using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkyNet
{
    //-------------------------------------------------------------------------------------
    /// <summary>
    /// Heatmap - Provides ability to render heatmap textures
    /// </summary>
    //-------------------------------------------------------------------------------------
    class Heatmap
    {
        WriteableBitmap _heatMap;
        public ImageSource Bitmap { get { return _heatMap; } }
        int[] _heatMapPixels;
        double[] _heatMapValues;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public double Transparency { get; set; }
        int[] palette = new int[256];

        public double[,] _brushValues;
        int _brushSize = 200;
        double _brushCenter = 100;
        int _brushRadius = 99;
        double[] _radiusMapAdjust;

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        //-------------------------------------------------------------------------------------
        public Heatmap(int width, int height, double transparency = 0)
        {
            Width = width;
            Height = height;
            Transparency = transparency;
            _heatMap = new WriteableBitmap(width, height, width, height, PixelFormats.Bgra32, null);
            _heatMapPixels = new int[width * height];
            _heatMapValues = new double[width * height];

            // Setup up line-by-line scaling values
            _radiusMapAdjust = new double[height];
            for (int i = 0; i < height; i++)
            {
                var theta = ((double)i - Height / 2) / (Height / 2) * Math.PI / 2;
                var cos = Math.Cos(theta);
                var radiusScaleFactor = double.MaxValue;
                if (cos != 0.0) radiusScaleFactor = 1 / Math.Cos(theta);
                if (radiusScaleFactor > 2.5) radiusScaleFactor = 2.5;
                _radiusMapAdjust[i] = radiusScaleFactor;
            }

            // set up the color palette
            ulong baseColor = (ulong)(0x6f) << 24;
            var newPalette = new List<int>();
            newPalette.Add((int)(baseColor));
            //for (ulong i = 1; i <= 10; i++)
            //{
            //    ulong red = i * 10;
            //    newPalette.Add((int)(baseColor + (red << 16)));
            //}
            //for (ulong i = 1; i <= 50; i++)
            //{
            //    ulong red = 50 + i * 2;
            //    newPalette.Add((int)(baseColor + (red << 16)));
            //}
            for (ulong i = 1; i <= 50; i++)
            {
                ulong red = 255;
                ulong green = i * 3;
                newPalette.Add((int)(baseColor + (red << 16) + (green << 8)));
            }
            for (ulong i = 1; i <= 50; i++)
            {
                ulong red = 255;
                ulong green = 150 + i * 2;
                newPalette.Add((int)(baseColor + (red << 16) + (green << 8)));
            }
            for (ulong i = 1; i <= 50; i++)
            {
                ulong red = 255 - i * 5;
                ulong green = 255;
                newPalette.Add((int)(baseColor + (red << 16) + (green << 8)));
            }
            for (ulong i = 1; i <= 25; i++)
            {
                ulong red = i * 10;
                ulong blue = i * 10;
                ulong green = 255;
                newPalette.Add((int)(baseColor + (red << 16) + (green << 8) + blue));
            }
            palette = newPalette.ToArray();

                //for (int i = 0; i < 256; i++)
                //{
                //    if (i < 1)
                //    {
                //        palette[i] = (int)(baseColor);
                //        continue;
                //    }

                //    if ((i / 64) == 0) palette[i] = (int)(baseColor + (0xff0000));
                //    else if ((i / 64) == 1) palette[i] = (int)(baseColor + (0xFFFF00));
                //    else if ((i / 64) == 2) palette[i] = (int)(baseColor + (0x00FF00));
                //    else palette[i] = (int)(baseColor + (0xFFFFFF)); ;

                //    //if ((i %4) == 0) palette[i] = (int)(baseColor + (0xff0000));
                //    //else if ((i % 4) == 1) palette[i] = (int)(baseColor + (0xFFFF00));
                //    //else if ((i % 4) == 2) palette[i] = (int)(baseColor + (0x00FF00));
                //    //else palette[i] = (int)(baseColor + (0xFFFFFF)); ;

                //}

            _brushValues = new double[_brushSize*2, _brushSize*2];
            _brushCenter = _brushSize;
            for(int x = 0; x < _brushSize*2; x++)
            {
                for (int y = 0; y < _brushSize*2; y++)
                {
                    var dx = x - _brushCenter;
                    var dy = y - _brushCenter;
                    var d = Math.Sqrt(dx * dx + dy * dy);
                    if(d > _brushRadius)
                    {
                        _brushValues[x, y] = 0;
                    }
                    else
                    {
                        d = (_brushRadius - d) / (_brushRadius * .3);
                        if (d < 0) d = 0;
                        if (d > 1) d = 1;
                        _brushValues[x, y] = d;
                    }
                }
            }
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// Render - render heatmap data as colors to the texture map
        /// </summary>
        //-------------------------------------------------------------------------------------
        public void Render()
        {
            int maxColor = palette.Length - 1;
            //ulong baseColor = ((ulong)(0xFF * (1-Transparency))) << 24;
            for (int i = 0; i < _heatMapValues.Length; i++)
            {
                int byteValue = (int)(maxColor * _heatMapValues[i]);
                if (byteValue > maxColor) byteValue = maxColor;
                _heatMapPixels[i] = palette[byteValue];// (int)(baseColor + byteValue);
            }
            var stride = Width * _heatMap.Format.BitsPerPixel / 8;
            _heatMap.WritePixels(new Int32Rect(0, 0, Width, Height), _heatMapPixels, stride, 0);
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// Clear
        /// </summary>
        //-------------------------------------------------------------------------------------
        public void Clear()
        {
            for (int i = 0; i < _heatMapValues.Length; i++)
            {
                _heatMapValues[i] = 0;
            }
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// DrawSpot
        /// </summary>
        //-------------------------------------------------------------------------------------
        public void DrawSpot3(double x, double y, int radius, double strength)
        {
            var brushyScaleFactor = (double)_brushRadius / radius;
            var xoff = x - (int)x;
            var yoff = y - (int)y;
            var r2 = radius * radius;
            for (int dy = -radius; dy <= radius; dy++)
            {
                var yy = y + dy;
                if (yy < 0 || yy >= Height)
                {
                    continue;
                }

                var brushy = (int)(_brushCenter + (dy - yoff) * brushyScaleFactor);
                var radiusScaleFactor = _radiusMapAdjust[(int)yy];

                var radiusTrim = Math.Sqrt(r2 - dy * dy) / radius;
                var adjustedRadius = (int)(radius * radiusScaleFactor * radiusTrim);

                var startx = (int)(x - adjustedRadius);
                startx = (startx + Width) % Width;
                var index = (int)startx + (int)yy * Width;
                var span = adjustedRadius * 2;

                
                for (int i = 0; i < span; i++)
                {
                    _heatMapValues[index] += strength;
                    index++;
                    //startx++;
                    //if (startx >= Width)
                    //{
                    //    startx = 0;
                    //    index -= Width;
                    //}
                }
            }
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// DrawSpot
        /// </summary>
        //-------------------------------------------------------------------------------------
        public void DrawSpot2(double x, double y, int radius, double strength)
        {
            var brushyScaleFactor = (double)_brushRadius / radius;
            var xoff = x - (int)x;
            var yoff = y - (int)y;
            var r2 = radius * radius;
            for (int dy = -radius; dy <= radius; dy++)
            {
                var yy = y + dy;
                if (yy < 0 || yy >= Height)
                {
                    continue;
                }

                var brushy = (int)(_brushCenter + (dy - yoff) * brushyScaleFactor);
                var radiusScaleFactor = _radiusMapAdjust[(int)yy];

                var radiusTrim = Math.Sqrt(r2 - dy * dy) / radius;
                var adjustedRadius = (int)(radius * radiusScaleFactor * radiusTrim);

                var startx = (int)(x - adjustedRadius);
                startx = (startx + Width) % Width;
                var index = (int)startx + (int)yy * Width;
                var span = adjustedRadius * 2;

                for (int i = 0; i < span; i++)
                {
                    _heatMapValues[index] += strength;
                    index++;
                    startx++;
                    if (startx >= Width)
                    {
                        startx = 0;
                        index -= Width;
                    }
                }
            }
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// DrawSpot
        /// </summary>
        //-------------------------------------------------------------------------------------
        public void DrawSpot1(double x, double y, int radius, double strength)
        {
            var brushyScaleFactor = (double)_brushRadius / radius;
            var xoff = x - (int)x;
            var yoff = y - (int)y;
            for (int dy = -radius; dy <= radius; dy++)
            {
                var yy = y + dy;
                if (yy < 0 || yy >= Height)
                {
                    continue;
                }

                var brushy = (int)(_brushCenter + (dy - yoff) * brushyScaleFactor);
                var theta = (yy - Height / 2) / (Height / 2) * Math.PI / 2;
                var radiusScaleFactor = 1 / Math.Cos(theta);
                if (radiusScaleFactor > 2.5) radiusScaleFactor = 2.5;

                var adjustedRadius = (int)(radius * radiusScaleFactor);
                var brushxScaleFactor = (double)_brushRadius / adjustedRadius;

                for (int dx = -adjustedRadius; dx <= adjustedRadius; dx++)
                {

                    var xx = x + dx;
                    var brushx = (int)(_brushCenter + (dx - xoff) * brushxScaleFactor);
                    var value = _brushValues[brushx, brushy];
                    if (value > 0)
                    {

                        var realx = (xx + Width) % Width;
                        var index = (int)realx + (int)yy * Width;
                        _heatMapValues[index] += value * strength;
                    }
                }
            }
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        //-------------------------------------------------------------------------------------
        public void DrawTest(int x, int y)
        {
            for (int yy = y - 3; yy <= y + 3; yy++)
            {
                if (yy < 0 || yy >= Height) continue;
                for (int xx = x - 3; xx <= x + 3; xx++)
                {

                    var realx = (xx + Width) % Width;
                    _heatMapValues[realx + yy * Width] += .2;
                }
            }
        }
    }


}
