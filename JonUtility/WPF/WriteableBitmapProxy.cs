namespace JonUtility.WPF
{
    using System;
    using System.Security;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    /// <summary>
    ///     Proxy class for a <see cref="WriteableBitmap" /> instance. Drawing
    ///     is thread-safe provided that the <see cref="WriteableBitmap" /> is
    ///     locked.
    /// </summary>
    [SecurityCritical]
    public class WriteableBitmapProxy
    {
        private readonly WriteableBitmap bitmap;
        private readonly int bytesPerPixel;

        [SecurityCritical]
        public WriteableBitmapProxy(WriteableBitmap bitmap)
        {
            this.BackBuffer = bitmap.BackBuffer;
            this.BackBufferStride = bitmap.BackBufferStride;
            this.PixelHeight = bitmap.PixelHeight;
            this.PixelWidth = bitmap.PixelWidth;
            this.bytesPerPixel = this.BackBufferStride / this.PixelWidth;
            this.bitmap = bitmap;
        }

        public IntPtr BackBuffer { get; }

        public int BackBufferStride { get; }

        public int PixelHeight { get; }

        public int PixelWidth { get; }

        public void AddDirtyRect(Int32Rect rect)
        {
            this.bitmap.AddDirtyRect(rect);
        }

        /// <summary>
        ///     Draws the image pixels at the specified coordinates. This method does not call
        ///     <see cref="WriteableBitmap.AddDirtyRect(Int32Rect)" /> afterward.
        /// </summary>
        /// <param name="imageData">
        ///     The <see cref="PixelData" /> instance containing the data to draw to the current
        ///     <see cref="WriteableBitmap" />.
        /// </param>
        /// <param name="left">The X coordinate of the <see cref="WriteableBitmap" /> where thte drawing will begin.</param>
        /// <param name="top">The Y coordinate of the <see cref="WriteableBitmap" /> where thte drawing will begin.</param>
        /// <param name="mode">The mode.</param>
        /// <exception cref="System.ArgumentNullException">imageData</exception>
        /// <exception cref="System.IndexOutOfRangeException">
        ///     the image rectangle must be within the bounds of the <see cref="WriteableBitmap" />.
        /// </exception>
        [SecurityCritical]
        public void DrawImagePixels(PixelData imageData, int left, int top, ImageDrawingMode mode = ImageDrawingMode.IgnoreAlpha)
        {
            var data = imageData;

            // The contents of the PixelData instance are verified in its constructor. I just need to check for null here.
            if (data == null)
            {
                throw new ArgumentNullException(nameof(imageData));
            }

            if (left < 0)
            {
                throw new IndexOutOfRangeException(string.Format("{0} must be greater than or equal to zero.", nameof(left)));
            }

            if (top < 0)
            {
                throw new IndexOutOfRangeException(string.Format("{0} must be greater than or equal to zero.", nameof(top)));
            }

            if (left + data.PixelWidth > this.PixelWidth)
            {
                throw new IndexOutOfRangeException(string.Format("{0} plus the {1}'s width must not be greater than the {1}.", nameof(left), nameof(imageData), nameof(this.PixelWidth)));
            }

            if (top + data.PixelHeight > this.PixelHeight)
            {
                throw new IndexOutOfRangeException(string.Format("{0} plus the {1}'s height must not be greater than the {1}.", nameof(top), nameof(imageData), nameof(this.PixelWidth)));
            }

            var bytesToDraw = data.Pixels;
            int drawtop = this.BackBufferStride * top;
            int drawLeft = drawtop + (left * this.bytesPerPixel);
            int imageStride = data.Stride;

            unsafe
            {
                byte* bufferStart = (byte*)this.BackBuffer;

                int count = data.Pixels.Length;
                byte* pixel = bufferStart + drawLeft;
                int target = imageStride - this.bytesPerPixel;
                int columnIndex = 0;

                if (mode == ImageDrawingMode.ExactCopy)
                {
                    for (int i = 0; i < count; i += this.bytesPerPixel)
                    {
                        pixel[0] = bytesToDraw[i];
                        pixel[1] = bytesToDraw[i + 1];
                        pixel[2] = bytesToDraw[i + 2];
                        pixel[3] = bytesToDraw[i + 3];

                        if (i == target)
                        {
                            target += imageStride;
                            columnIndex += 1;
                            pixel = bufferStart + drawLeft + (this.BackBufferStride * columnIndex);
                        }
                        else
                        {
                            pixel += this.bytesPerPixel;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < count; i += this.bytesPerPixel)
                    {
                        byte alphaByte = bytesToDraw[i + 3];

                        if (alphaByte == 255)
                        {
                            pixel[0] = bytesToDraw[i];
                            pixel[1] = bytesToDraw[i + 1];
                            pixel[2] = bytesToDraw[i + 2];
                            pixel[3] = alphaByte;
                        }

                        if (i == target)
                        {
                            target += imageStride;
                            columnIndex += 1;
                            pixel = bufferStart + drawLeft + (this.BackBufferStride * columnIndex);
                        }
                        else
                        {
                            pixel += this.bytesPerPixel;
                        }
                    }
                }
            }
        }

        [SecurityCritical]
        public void DrawRectangle(Brush brush, Int32Rect rect)
        {
            var drawBrush = brush;
            if (drawBrush == null)
            {
                throw new ArgumentNullException(nameof(brush));
            }

            if (drawBrush is SolidColorBrush)
            {
                this.DrawRectangleSolidColorBrush((SolidColorBrush)drawBrush, rect);
            }
            else if (drawBrush is LinearGradientBrush)
            {
                this.DrawRectangleLinearGradientBrush((LinearGradientBrush)drawBrush, rect);
            }
        }

        public Color GetPixel(int x, int y)
        {
            int drawtop = this.BackBufferStride * y;
            int drawLeft = drawtop + (x * this.bytesPerPixel);

            unsafe
            {
                byte* buffer = (byte*)this.BackBuffer + drawLeft;

                return Color.FromArgb(buffer[3], buffer[2], buffer[1], buffer[0]);
            }
        }

        public void Lock()
        {
            this.bitmap.Lock();
        }

        public void Unlock()
        {
            this.bitmap.Unlock();
        }

        [SecurityCritical]
        private void DrawRectangleLinearGradientBrush(LinearGradientBrush brush, Int32Rect rect)
        {
            var stops = brush.GradientStops;
            var stopCount = stops.Count;
            for (int i = 0; i < stopCount; i++)
            {
                var gradientStop = stops[i];
                var offset = gradientStop.Offset;
            }
        }
        
        // bgra32
        [SecurityCritical]
        private void DrawRectangleSolidColorBrush(SolidColorBrush brush, Int32Rect rect)
        {
            var brushToUse = brush;
            if (brush == null)
            {
                return;
            }

            var brushColor = brushToUse.Color;
            if (brushColor.A == 0)
            {
                return;
            }

            int drawLeft = (this.PixelWidth * rect.Y) + rect.X;
            int imageStride = rect.Width;
            int count = rect.Width * rect.Height;
            int distance = this.PixelWidth - rect.Width;

            unsafe
            {
                uint* bufferStart = (uint*)this.BackBuffer + drawLeft;
                uint* buffer = bufferStart;
                int widthEnd = rect.X + rect.Width;
                int heightEnd = rect.Y + rect.Height;

                if (brushColor.A == 255)
                {
                    uint color = (uint)(brushColor.B | brushColor.G << 8 | brushColor.R << 16 | brushColor.A << 24);

                    if (!BitConverter.IsLittleEndian)
                    {
                        color = (uint)(brushColor.A | brushColor.R << 8 | brushColor.G << 16 | brushColor.B << 24);
                    }

                    for (int r = rect.X; r < widthEnd; r++)
                    {
                        for (int c = rect.Y; c < heightEnd; c++)
                        {
                            *buffer = color;
                            buffer++;
                        }

                        buffer += distance;
                    }
                }
                else
                {
                    float alphaDst = brushColor.A / 255.0F;
                    float alphaSrc = 1.0F - alphaDst;
                    float dstA = 1.0f / 255f * brushColor.A;

                    float destB = brushColor.B * alphaDst;
                    float destG = brushColor.G * alphaDst;
                    float destR = brushColor.R * alphaDst;

                    for (int r = rect.X; r < widthEnd; r++)
                    {
                        for (int c = rect.Y; c < heightEnd; c++)
                        {
                            byte* tempBuffer = (byte*)buffer;

                            if (tempBuffer[3] == 0)
                            {
                                tempBuffer[0] = (byte)destB;
                                tempBuffer[1] = (byte)destG;
                                tempBuffer[2] = (byte)destR;
                                tempBuffer[3] = brushColor.A;
                            }
                            else
                            {
                                float srcA = 1.0f / 255f * tempBuffer[3];
                                float outA = (srcA + (dstA - dstA * srcA));

                                tempBuffer[0] = (byte)(destB + (tempBuffer[0] * alphaSrc));
                                tempBuffer[1] = (byte)(destG + (tempBuffer[1] * alphaSrc));
                                tempBuffer[2] = (byte)(destR + (tempBuffer[2] * alphaSrc));
                                tempBuffer[3] = (byte)(outA * 255);
                            }

                            buffer++;
                        }

                        buffer += distance;
                    }
                }
            }
        }
    }
}
