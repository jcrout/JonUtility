namespace JonUtility.WPF
{
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public class PixelData
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PixelData" />class.
        /// </summary>
        /// <param name="stride">
        ///     The
        ///     <see cref="WriteableBitmap.BackBufferStride" /> property.
        /// </param>
        /// <param name="pixelWidth">The PixelWidth.</param>
        /// <param name="pixelHeight">The PixelHeight.</param>
        /// <param name="pixels">The pixels.</param>
        public PixelData(int stride, int pixelWidth, int pixelHeight, byte[] pixels)
        {
            Requires.NotNullOrEmpty(pixels, nameof(pixels));
            Requires.GreaterThanZero(pixelWidth, nameof(pixelWidth));
            Requires.GreaterThanZero(pixelHeight, nameof(pixelHeight));
            Requires.GreaterThan(stride, pixelWidth, nameof(stride), null);

            this.Stride = stride;
            this.PixelWidth = pixelWidth;
            this.PixelHeight = pixelHeight;
            this.Pixels = pixels;
        }

        /// <summary>
        ///     Gets the <see cref="BitmapSource.PixelHeight" /> value.
        /// </summary>
        /// <value>The PixelHeight of the <see cref="BitmapSource" /> in pixels.</value>
        public int PixelHeight { get; }

        /// <summary>
        ///     Gets the array of pixels that represents the <see cref="BitmapSource" />.
        /// </summary>
        /// <value>The array of pixels, represented as an array of bytes, that makes up the <see cref="BitmapSource" />.</value>
        public byte[] Pixels { get; }

        /// <summary>
        ///     Gets the <see cref="BitmapSource.PixelWidth" /> value.
        /// </summary>
        /// <value>The PixelWidth of the <see cref="BitmapSource" /> in pixels.</value>
        public int PixelWidth { get; }

        /// <summary>
        ///     Gets the Stride of the <see cref="BitmapSource" />.
        /// </summary>
        /// <value>
        ///     The <see cref="BitmapSource.Width" /> times the <see cref="BitmapSource.Format" />'s
        ///     <see cref="PixelFormat.BitsPerPixel" /> value.
        /// </value>
        public int Stride { get; }
    }
}
