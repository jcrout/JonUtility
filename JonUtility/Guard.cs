namespace JonUtility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    ///     This class contains static methods to simplify checking for null/empty conditions.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        ///     Throws an <see cref="ArgumentNullException"/> if <paramref name="parameterValue"/> is null.
        /// </summary>
        /// <typeparam name="T">The type of the parameter to check.</typeparam>
        /// <param name="parameterValue">The parameter to check for null.</param>
        /// <param name="name">The name of the parameter. If null, <see cref="String.Empty"/> is used instead.</param>
        public static void AgainstNull<T>(T parameterValue, string name) where T : class
        {
            if (parameterValue == null)
            {
                throw new ArgumentNullException(name ?? String.Empty);
            }
        }

        /// <summary>
        ///     Throws an <see cref="ArgumentNullException"/> if <paramref name="parameterValue"/> is null. 
        ///     If <paramref name="parameterValue"/> is empty, this method instead throws an 
        ///     <see cref="ArgumentException"/> with the value of <paramref name="isEmptyMessage"/>.
        /// </summary>
        /// <param name="parameterValue">The string parameter to check if null or empty.</param>
        /// <param name="name">The name of the string parameter. If null, <see cref="String.Empty"/> is used instead.</param>
        /// <param name="isEmptyMessage">The message to display in the exception if the string parameter is empty. 
        ///     If null, <see cref="String.Empty"/> is used instead.</param>
        public static void AgainstNullOrEmptyString(string parameterValue, string name, string isEmptyMessage = "String cannot be null or empty.")
        {
            if (parameterValue == null)
            {
                throw new ArgumentNullException(name ?? String.Empty);
            }

            if (String.Equals(parameterValue, String.Empty))
            {
                throw new ArgumentException(isEmptyMessage ?? String.Empty, name ?? String.Empty);
            }
        }

        /// <summary>
        ///     Throws an <see cref="ArgumentNullException"/> if <paramref name="parameterValue"/> is null. 
        ///     If <paramref name="parameterValue"/> is composed only of whitespce, this method instead throws an 
        ///     <see cref="ArgumentException"/> with the value of <paramref name="isWhiteSpaceMessage"/>.
        /// </summary>
        /// <param name="parameterValue">The string parameter to check if null or empty.</param>
        /// <param name="name">The name of the string parameter. If null, <see cref="String.Empty"/> is used instead.</param>
        /// <param name="isWhiteSpaceMessage">The message to display in the exception if the string parameter is composed only of whitespace. 
        ///     If null, <see cref="String.Empty"/> is used instead.</param>
        public static void AgainstNullOrWhiteSpaceString(string parameterValue, string name, string isWhiteSpaceMessage = "String cannot be null, empty, or contain only whitespace characters.")
        {
            if (parameterValue == null)
            {
                throw new ArgumentNullException(name ?? String.Empty);
            }

            if (String.IsNullOrWhiteSpace(parameterValue))
            {
                throw new ArgumentException(isWhiteSpaceMessage ?? String.Empty, name ?? String.Empty);
            }
        }

        /// <summary>
        ///     Throws an <see cref="ArgumentNullException"/> if <paramref name="parameterValue"/> is null.
        /// </summary>
        /// <typeparam name="T">The type of the element contained in the <paramref name="parameterValue"/>.</typeparam>
        /// <param name="parameterValue"></param>
        /// <param name="name">The name of the enumerable parameter. If null, <see cref="String.Empty"/> is used instead.</param>
        /// <param name="isEmptyMessage">The message to display in the exception if the enumerable contains no elements. 
        ///     If null, <see cref="String.Empty"/> is used instead.</param>
        public static void AgainstNullOrEmptyEnumerable<T>(IEnumerable<T> parameterValue, string name, string isEmptyMessage = "Enumerable cannot be empty.")
        {
            if (parameterValue == null)
            {
                throw new ArgumentNullException(name ?? String.Empty);
            }

            if (!parameterValue.Any())
            {
                throw new ArgumentException(isEmptyMessage ?? String.Empty, name ?? String.Empty);
            }
        }

        /// <summary>
        ///     Throws an <see cref="ArgumentNullException"/> if <paramref name="parameterValue"/> is null. 
        ///     In addition, if any of the contained instance properties with public getters are null, this method will
        ///     throw an <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <typeparam name="T">The type of the parameter to check.</typeparam>
        /// <param name="parameterValue">The parameter to check for null alongside its properties.</param>
        /// <param name="name">The name of the string parameter. If null, <see cref="String.Empty"/> is used instead.</param>
        /// <param name="includeContainerNameWhenPropertyIsNull">A <see cref="bool"/> value expressing whether or not to include 
        ///     the <paramref name="name"/> value with a dot before the property name, such as "String.Empty" instead of just "Empty".</param>
        public static void AgainstNullDataContainer<T>(T parameterValue, string name, bool includeContainerNameWhenPropertyIsNull = true) where T : class
        {
            if (parameterValue == null)
            {
                throw new ArgumentNullException(name ?? String.Empty);
            }

            var properties = parameterValue.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => !p.PropertyType.IsValueType && p.CanRead);
            foreach (var property in properties)
            {
                var value = property.GetValue(parameterValue);
                if (value == null)
                {
                    var propertyName = (includeContainerNameWhenPropertyIsNull && name != null ? name + "." : String.Empty) + property.Name;
                    throw new ArgumentNullException(propertyName);
                }
            }
        }

        public static void AgainstDuplicateKey<T, T2>(IDictionary<T,T2> dictionary, T key, string name)
        {
            if (dictionary.ContainsKey(key))
            {
                throw new ArgumentException("Dictionary cannot contain duplicate keys.", name);
            }
        }
    }
}
