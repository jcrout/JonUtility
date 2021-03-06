namespace JonUtility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Web;

    public static class ExtensionMethods
    {
        public static T Or<T>(this T @this, T other) where T : class
        {
            return @this != null ? @this : other;
        }

        public static DateTime ToDateFromLongStamp(this string @this)
        {
            return new DateTime(
                Int32.Parse(@this.Substring(0, 4)),
                Int32.Parse(@this.Substring(4, 2)),
                Int32.Parse(@this.Substring(6, 2)),
                Int32.Parse(@this.Substring(8, 2)),
                Int32.Parse(@this.Substring(10, 2)),
                Int32.Parse(@this.Substring(12, 2)));
        }

        public static string ToLongDateTimeStamp(this DateTime @this)
        {
            return @this.ToString("yyyyMMddHHmmss");
        }

        public static string AppendIfMissing(this string @this, string extra)
        {
            return @this != null ? (!@this.EndsWith(extra) ? @this + extra : @this) : null;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> @this)
        {
            return @this == null || !@this.Any();
        }

        public static IEnumerable<T> AsEnumerable<T>(this IEnumerable @this)
        {
            var items = new List<T>();
            foreach (var item in @this)
            {
                items.Add((T)item);
            }

            return items;
        }

        public static string Or(this string @this, string other)
        {
            return !String.IsNullOrEmpty(@this) ? @this : other;
        }

        public static string Left(this string @this, int count)
        {
            return !String.IsNullOrEmpty(@this) ? (@this.Length < count ? @this : @this.Substring(0, count)) : null;
        }

        public static SecureString ConvertToSecureString(this string @this)
        {
            unsafe
            {
                fixed (char* c = @this)
                {
                    SecureString ss = new SecureString(c, @this.Length);
                    ss.MakeReadOnly();
                    return ss;
                }
            }
        }

        public static string ConvertToString(this SecureString @this)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(@this);
                unsafe
                {
                    char* c = (char*)ptr;
                    return new string(c, 0, @this.Length);
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }

        /// <summary>
        ///     Removes a substring from the string and inserts a new string at
        ///     the same index.
        /// </summary>
        /// <param name="this">The string instance.</param>
        /// <param name="insertionIndex">
        ///     Index of the insertion/deletion point.
        /// </param>
        /// <param name="deletionCount">
        ///     The number of characters to delete.
        /// </param>
        /// <param name="textToInsert">The text to insert.</param>
        /// <returns>System.String.</returns>
        public static string InsertReplace(this string @this, int insertionIndex, int deletionCount, string textToInsert)
        {
            @this = @this.Remove(insertionIndex, deletionCount);
            @this = @this.Insert(insertionIndex, textToInsert);
            return @this;
        }

        public static bool IsNaNorInfinity(this Double @this)
        {
            return Double.IsNaN(@this) || Double.IsInfinity(@this);
        }

        public static bool IsNanInfinityOrZero(this Double @this)
        {
            return Double.IsNaN(@this) || Double.IsInfinity(@this) || @this == 0d;
        }

        public static void TraceError(this TraceSource @this, Exception ex)
        {
            string data;
            if (ex is AggregateException)
            {
                var errors = from error in ((AggregateException)ex).Flatten().InnerExceptions
                             select error.GetType().Name + ": " + error.Message;
                data = string.Join(Environment.NewLine, errors);
            }
            else
            {
                data = ex.Message;
            }

            @this.TraceData(TraceEventType.Error, 0, data);
        }

        /// <summary>
        ///     Raises the event in a thread-safe manner and checks for a null event. Uses <see cref="EventArgs.Empty" /> in the
        ///     invocation.
        /// </summary>
        /// <param name="this">The <see cref="EventHandler" /> instance.</param>
        /// <param name="sender">The sender of the event.</param>
        public static void SafeRaise(this EventHandler @this, object sender)
        {
            if (@this != null)
            {
                @this(sender, EventArgs.Empty);
            }
        }

        /// <summary>
        ///     Raises the event in a thread-safe manner and checks for a null event.
        /// </summary>
        /// <param name="this">The <see cref="EventHandler" /> instance.</param>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="eventArgs">The <see cref="EventArgs" /> instance to use during invocation.</param>
        public static void SafeRaise(this EventHandler @this, object sender, EventArgs eventArgs)
        {
            if (@this != null)
            {
                @this(sender, eventArgs);
            }
        }

        /// <summary>
        ///     Raises the event in a thread-safe manner and checks for a null event.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="EventArgs" />.</typeparam>
        /// <param name="this">The <see cref="EventHandler" /> instance.</param>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="eventArgs">The <see cref="EventArgs" /> instance to use during invocation.</param>
        public static void SafeRaise<T>(this EventHandler<T> @this, object sender, T eventArgs) where T : EventArgs
        {
            if (@this != null)
            {
                @this(sender, eventArgs);
            }
        }

        /// <summary>
        ///     Raises the event in a thread-safe manner and checks for a null event.
        /// </summary>
        /// <param name="this">The <see cref="PropertyChangedEventHandler" /> instance.</param>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="eventArgs">The <see cref="PropertyChangedEventArgs" /> instance to use during invocation.</param>
        public static void SafeRaise(this PropertyChangedEventHandler @this, object sender, PropertyChangedEventArgs eventArgs)
        {
            if (@this != null)
            {
                @this(sender, eventArgs);
            }
        }

        /// <summary>
        ///     Gets the default value of the <see cref="Type" />.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <returns>the default{T} value for a value type; otherwise, null for a reference type</returns>
        public static object GetDefaultValue(this Type @this)
        {
            if (@this.IsValueType)
            {
                return Activator.CreateInstance(@this);
            }

            return null;
        }
    }
}
