namespace JonUtility
{
    using System;
    using System.Drawing;

    /// <summary>
    ///     Provides a generic container for two readonly same-type struct
    ///     values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Point<T> : IEquatable<Point<T>> where T : struct, IEquatable<T>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Point{T}" /> struct.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        public Point(T x, T y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        ///     Gets the X value.
        /// </summary>
        /// <value>The x value.</value>
        public T X { get; }

        /// <summary>
        ///     Gets the Y value.
        /// </summary>
        /// <value>The y value.</value>
        public T Y { get; }

        public static bool operator ==(Point<T> left, Point<T> right)
        {
            return left.X.Equals(right.X) && left.Y.Equals(right.Y);
        }

        public static bool operator !=(Point<T> left, Point<T> right)
        {
            return !left.X.Equals(right.X) || !left.Y.Equals(right.Y);
        }

        public static explicit operator Point<byte>(Point<T> point)
        {
            return Point<T>.ConvertPoint<byte>(point);
        }

        public static explicit operator Point<short>(Point<T> point)
        {
            return Point<T>.ConvertPoint<short>(point);
        }

        public static explicit operator Point<int>(Point<T> point)
        {
            return Point<T>.ConvertPoint<int>(point);
        }

        public static explicit operator Point<long>(Point<T> point)
        {
            return Point<T>.ConvertPoint<long>(point);
        }

        public static explicit operator Point<float>(Point<T> point)
        {
            return Point<T>.ConvertPoint<float>(point);
        }

        public static explicit operator Point<double>(Point<T> point)
        {
            return Point<T>.ConvertPoint<double>(point);
        }

        public static explicit operator System.Windows.Point(Point<T> point)
        {
            if (typeof(T).IsPrimitive)
            {
                return new System.Windows.Point(
                    (double)Convert.ChangeType(point.X, typeof(double)),
                    (double)Convert.ChangeType(point.Y, typeof(double)));
            }

            return default(System.Windows.Point);
        }

        public static explicit operator System.Drawing.Point(Point<T> point)
        {
            if (typeof(T).IsPrimitive)
            {
                return new Point(
                    (int)Convert.ChangeType(point.X, typeof(int)),
                    (int)Convert.ChangeType(point.Y, typeof(int)));
            }

            return default(Point);
        }

        public static explicit operator PointF(Point<T> point)
        {
            if (typeof(T).IsPrimitive)
            {
                return new PointF(
                    (float)Convert.ChangeType(point.X, typeof(float)),
                    (float)Convert.ChangeType(point.Y, typeof(float)));
            }

            return default(PointF);
        }

        /// <summary>
        ///     Converts the point parameter into a new <see cref="Point{T}" /> instance. This method only converts between
        ///     primitive numeric types; if the target type is not a primitve numeric type, then the default value is returned
        ///     instead.
        /// </summary>
        /// <typeparam name="T2">The type of <see cref="Point{T}" /> to return.</typeparam>
        /// <param name="point">The point to convert.</param>
        /// <returns>A new <see cref="Point{T2}" /> instance.</returns>
        /// <exception cref="System.InvalidCastException">
        ///     This conversion is not supported. Only <see cref="Point{T}" /> instances
        ///     containing primitive numeric types can be converted./>
        /// </exception>
        /// <exception cref="System.FormatException">
        ///     One or both of the point's values are not in a format recognized by the target
        ///     type.
        /// </exception>
        /// <exception cref="System.OverflowException">
        ///     One or both of the point's values represents a number that is out of the
        ///     range of the target type.
        /// </exception>
        public static Point<T2> ConvertPoint<T2>(Point<T> point) where T2 : struct, IEquatable<T2>
        {
            if (typeof(T).IsPrimitive && typeof(T2).IsPrimitive)
            {
                return new Point<T2>(
                    (T2)Convert.ChangeType(point.X, typeof(T2)),
                    (T2)Convert.ChangeType(point.Y, typeof(T2)));
            }

            return default(Point<T2>);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point<T>))
            {
                return false;
            }

            return this == (Point<T>)obj;
        }

        /// <summary>
        ///     Indicates whether the current <see cref="Point{T}" /> is equal to another <see cref="Point{T}" /> of the same
        ///     generic type.
        /// </summary>
        /// <param name="other">An <see cref="Point{T}" /> to compare with this <see cref="Point{T}" />.</param>
        /// <returns>
        ///     <c>true</c> if the current <see cref="Point{T}" /> instance is equal to the other parameter; otherwise,
        ///     <c>false</c>.
        /// </returns>
        public bool Equals(Point<T> other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return this.X + ", " + this.Y;
        }
    }
}
