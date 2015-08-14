namespace JonUtility.WPF
{
    public enum ImageDrawingMode
    {
        /// <summary>
        ///     Indicates that all pixel data should be copied except for the
        ///     alpha value if it is below 255.
        /// </summary>
        IgnoreAlpha,

        /// <summary>
        ///     Indicates that all pixel data should be copied as-is, including alpha.
        /// </summary>
        ExactCopy
    }
}
