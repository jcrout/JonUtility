namespace JonUtility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    ///     Utility class for checking method arguments for various conditions.
    ///     Appropriate <see cref="System.Exception" /> types will be thrown in
    ///     the event that the specified condition(s) fail.
    /// </summary>
    [DebuggerStepThrough]
    public static class Requires
    {
        public static void Condition(Func<bool> condition, string message)
        {
            if (!condition())
            {
                throw new Exception(message ?? string.Empty);
            }
        }

        public static void Condition<T>(Func<bool> condition, string message) where T : Exception
        {
            if (!condition())
            {
                try
                {
                    throw (Exception)Activator.CreateInstance(typeof(T), message ?? string.Empty);
                }
                catch
                {
                    throw new Exception(message ?? string.Empty);
                }
            }
        }

        public static void Condition(Func<bool> condition, string message, Type exceptionType)
        {
            if (!condition())
            {
                try
                {
                    throw (Exception)Activator.CreateInstance(exceptionType, message ?? string.Empty);
                }
                catch
                {
                    throw new Exception(message ?? string.Empty);
                }
            }
        }

        public static void NotNull<T>(T argument, string argumentName) where T : class
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName ?? "argument");
            }
        }

        public static void NotNullOrEmpty<T>(string argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName ?? "string");
            }

            if (!argument.Any())
            {
                var type = argument.GetType();
                throw new ArgumentException(string.Format(
                    "{0} must contain text.",
                    argumentName ?? "string"));
            }
        }

        public static void NotNullOrEmpty<T>(IEnumerable<T> argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName ?? "enumerable");
            }

            if (!argument.Any())
            {
                var type = argument.GetType();
                throw new ArgumentException(string.Format(
                    "{0} must contain at least one {1}",
                    argumentName ?? "enumerable",
                    type.HasElementType ? type.GetElementType().Name : "item"));
            }
        }

        public static void LessThan(int argument1, int argument2, string argument1Name, string argument2Name = "")
        {
            if (argument1 >= argument2)
            {
                throw new ArgumentException(string.Format(
                    "{0} must be less than {1}.",
                    argument1Name ?? "the first argument",
                    string.IsNullOrWhiteSpace(argument2Name) ?
                        argument2.ToString() :
                        argument2Name));
            }
        }

        public static void LessThanOrEqualTo(int argument1, int argument2, string argument1Name, string argument2Name = "")
        {
            if (argument1 > argument2)
            {
                throw new ArgumentException(string.Format(
                    "{0} must be less than or equal to {1}.",
                    argument1Name ?? "the first argument",
                    string.IsNullOrWhiteSpace(argument2Name) ?
                        argument2.ToString() :
                        argument2Name));
            }
        }

        public static void GreaterThan(int argument1, int argument2, string argument1Name, string argument2Name = "")
        {
            if (argument1 <= argument2)
            {
                throw new ArgumentException(string.Format(
                    "{0} must be greater than {1}.",
                    argument1Name ?? "the first argument",
                    string.IsNullOrWhiteSpace(argument2Name) ?
                        argument2.ToString() :
                        argument2Name));
            }
        }

        public static void GreaterThanOrEqualTo(int argument1, int argument2, string argument1Name, string argument2Name = "")
        {
            if (argument1 < argument2)
            {
                throw new ArgumentException(string.Format(
                    "{0} must be greater than or equal to {1}.",
                    argument1Name ?? "the first argument",
                    string.IsNullOrWhiteSpace(argument2Name) ?
                        argument2.ToString() :
                        argument2Name));
            }
        }

        public static void GreaterThanZero(int argument, string argumentName)
        {
            if (argument < 1)
            {
                throw new ArgumentException(string.Format(
                    "{0} must be greater than zero.",
                    argumentName ?? "value"));
            }
        }
    }
}
