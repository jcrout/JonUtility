namespace JonUtility.WPF
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public static class WpfExtensionMethods
    {
        private static readonly MethodInfo removeLogicalChildMethod;
        private static readonly MethodInfo removeVisualChildMethod;

        static WpfExtensionMethods()
        {
            WpfExtensionMethods.removeVisualChildMethod = typeof(FrameworkElement).GetMethod(
                "RemoveVisualChild",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(Visual) },
                null);

            WpfExtensionMethods.removeLogicalChildMethod = typeof(FrameworkElement).GetMethod(
                "RemoveLogicalChild",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(object) },
                null);
        }

        public static bool ForceRemoveChild(this FrameworkElement @this, object child)
        {
            if (@this == null || child == null)
            {
                return false;
            }

            if (WpfExtensionMethods.removeLogicalChildMethod == null ||
                WpfExtensionMethods.removeVisualChildMethod == null)
            {
                return false;
            }

            try
            {
                if (WpfExtensionMethods.removeLogicalChildMethod != null)
                {
                    WpfExtensionMethods.removeLogicalChildMethod.Invoke(@this, new[] { child });
                }

                if (WpfExtensionMethods.removeVisualChildMethod != null)
                {
                    WpfExtensionMethods.removeVisualChildMethod.Invoke(@this, new object[] { (Visual)child });
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Color GetOffsetColor(this Color @this, int offset, bool applyToAlpha = false)
        {
            byte alpha = applyToAlpha ? WpfExtensionMethods.GetByte(@this.A, offset) : @this.A;
            byte red = WpfExtensionMethods.GetByte(@this.R, offset);
            byte green = WpfExtensionMethods.GetByte(@this.G, offset);
            byte blue = WpfExtensionMethods.GetByte(@this.B, offset);

            return Color.FromArgb(alpha, red, green, blue);
        }

        public static Color WithAlpha(this Color @this, byte alpha)
        {
            return Color.FromArgb(alpha, @this.R, @this.G, @this.B);
        }

        public static RenderTargetBitmap ConvertToBitmap(this UIElement @this, double resolution)
        {
            var scale = resolution / 96d;

            @this.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            var sz = @this.DesiredSize;
            var rect = new Rect(sz);
            @this.Arrange(rect);

            var bmp = new RenderTargetBitmap(
                (int)(scale * (rect.Width)),
                (int)(scale * (rect.Height)),
                scale * 96,
                scale * 96,
                PixelFormats.Default);
            bmp.Render(@this);

            return bmp;
        }

        public static void ConvertToJpeg(UIElement uiElement, string path, double resolution)
        {
            var jpegString = WpfExtensionMethods.CreateJpeg(uiElement.ConvertToBitmap(resolution));

            if (path != null)
            {
                try
                {
                    using (var fileStream = File.Create(path))
                    {
                        using (var streamWriter = new StreamWriter(fileStream, Encoding.Default))
                        {
                            streamWriter.Write(jpegString);
                            streamWriter.Close();
                        }

                        fileStream.Close();
                    }
                }
                catch
                {
                }
            }
        }

        public static string CreateJpeg(RenderTargetBitmap bitmap)
        {
            var jpeg = new JpegBitmapEncoder();
            jpeg.Frames.Add(BitmapFrame.Create(bitmap));
            string result;

            using (var memoryStream = new MemoryStream())
            {
                jpeg.Save(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                using (var streamReader = new StreamReader(memoryStream, Encoding.Default))
                {
                    result = streamReader.ReadToEnd();
                    streamReader.Close();
                }

                memoryStream.Close();
            }

            return result;
        }

        public static byte[] GetPixelBytes(this BitmapSource @this)
        {
            int stride = @this.PixelWidth * (@this.Format.BitsPerPixel / 8);
            var imgBytes = new byte[stride * @this.PixelHeight];
            @this.CopyPixels(imgBytes, stride, 0);

            return imgBytes;
        }

        public static PixelData GetPixelData(this BitmapSource @this, int stride = -1)
        {
            int imgStride = @this.PixelWidth * (@this.Format.BitsPerPixel / 8);
            var imgBytes = new byte[imgStride * @this.PixelHeight];
            @this.CopyPixels(imgBytes, imgStride, 0);

            return new PixelData(
                imgStride,
                @this.PixelWidth,
                @this.PixelHeight,
                imgBytes);
        }

        public static BitmapSource ConvertFormat(this BitmapSource @this, PixelFormat targetFormat)
        {
            var currentFormat = @this.Format;
            if (currentFormat == targetFormat)
            {
                return @this;
            }

            FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();
            newFormatedBitmapSource.BeginInit();
            newFormatedBitmapSource.Source = @this;
            newFormatedBitmapSource.DestinationFormat = targetFormat;
            newFormatedBitmapSource.EndInit();

            return newFormatedBitmapSource;
        }

        /// <summary>
        ///     Converts the string representation of a <see cref="Color" /> in
        ///     [#]AARRGGBB hexadecimal format to the corresponding
        ///     <see cref="Color" />.
        /// </summary>
        /// <param name="this">The string to convert.</param>
        /// <returns>
        ///     The <see cref="Color" /> that corresponds to this string
        ///     instance.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">@this</exception>
        /// <exception cref="System.FormatException">
        ///     Input string was not in a correct format.
        /// </exception>
        public static Color ToColorARGB(this string @this)
        {
            if (@this == null)
            {
                throw new ArgumentNullException(nameof(@this));
            }

            @this = @this.Replace("#", "");

            try
            {
                var values = Enumerable.Range(0, 4).Select(i => (byte)int.Parse(@this.Substring(i * 2, 2), NumberStyles.HexNumber)).ToArray();
                var color = Color.FromArgb(values[0], values[1], values[2], values[3]);
                return color;
            }
            catch (Exception ex)
            {
                throw new FormatException("Input string was not in a correct format.", ex);
            }
        }

        private static byte GetByte(byte initialValue, int offset)
        {
            int value = initialValue + offset;
            if (value < 0)
            {
                return 0;
            }
            if (value > 255)
            {
                return 255;
            }
            return (byte)value;
        }

        /// <summary>
        ///     Executes the target ICommand only if the ICommand instance is not null and if the CanExecute(parameter) method
        ///     returns true.
        /// </summary>
        /// <param name="this">The ICommand to execute.</param>
        /// <param name="parameter">
        ///     The data to pass to the ICommand.CanExecute and ICommand.Execute methods.
        ///     This parameter can be null if no data is needed.
        /// </param>
        [DebuggerStepThrough]
        public static void ExecuteIfAbleTo(this ICommand @this, object parameter = null)
        {
            if (@this == null)
            {
                return;
            }

            if (@this.CanExecute(parameter))
            {
                @this.Execute(parameter);
            }
        }
    }
}
