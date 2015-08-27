namespace JonUtility.WPF
{
    using System;
    using System.Diagnostics;
    using System.Windows.Input;

    /// <summary>
    ///     Provides an implementation of the ICommand interface that allows
    ///     Execute/CanExecute delegates to optionally contain object
    ///     parameters.
    /// </summary>
    [DebuggerStepThrough]
    public class Command : ICommand
    {
        /// <summary>
        ///     The parameter-less execution predicate to invoke on ICommand.CanExecute(object parameter).
        /// </summary>
        private readonly Func<bool> canExecute;

        /// <summary>
        ///     The parameter-containing execution predicate to invoke on ICommand.CanExecute(object parameter).
        /// </summary>
        private readonly Func<object, bool> canExecuteWithArgument;

        /// <summary>
        ///     The parameter-less execution delegate to invoke on ICommand.Execute(object parameter).
        /// </summary>
        private readonly Action execute;

        /// <summary>
        ///     The parameter-containing execution delegate to invoke on ICommand.Execute(object parameter).
        /// </summary>
        private readonly Action<object> executeWithArgument;

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution delegate.
        ///     The Execution parameter is ignored.
        ///     The CanExecute(object parameter) method always returns true.
        /// </summary>
        /// <param name="executionDelegate">The delegate to execute when ICommand.Execute(object parameter) is called.</param>
        public Command(Action executionDelegate)
        {
            if (executionDelegate == null)
            {
                throw new ArgumentNullException(nameof(executionDelegate));
            }

            this.execute = executionDelegate;
        }

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution delegate.
        /// </summary>
        /// <remarks>
        ///     <para>The Execution parameter is passed to the delegate.</para>
        ///     <para>The CanExecute(object parameter) method always returns true.</para>
        /// </remarks>
        /// <param name="executionDelegate">The delegate to execute when ICommand.Execute(object parameter) is called.</param>
        public Command(Action<object> executionDelegate)
        {
            if (executionDelegate == null)
            {
                throw new ArgumentNullException(nameof(executionDelegate));
            }

            this.executeWithArgument = executionDelegate;
        }

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution and CanExecute delegates.
        /// </summary>
        /// <remarks>
        ///     The Execution/CanExecute parameters are ignored.
        /// </remarks>
        /// <param name="executionDelegate">The delegate to execute when ICommand.Execute(object parameter) is called.</param>
        /// <param name="canExecuteDelegate">The delegate to execute when ICommand.CanExecute(object parameter) is called.</param>
        public Command(Action executionDelegate, Func<bool> canExecuteDelegate)
            : this(executionDelegate)
        {
            this.canExecute = canExecuteDelegate;
        }

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution and CanExecute delegates.
        /// </summary>
        /// <remarks>
        ///     <para>The Execution parameter is ignored.</para>
        ///     <para>The CanExecute parameter is passed to the delegate.</para>
        /// </remarks>
        /// <param name="executionDelegate">The delegate to execute when ICommand.Execute(object parameter) is called.</param>
        /// <param name="canExecuteDelegate">The delegate to execute when ICommand.CanExecute(object parameter) is called.</param>
        public Command(Action executionDelegate, Func<object, bool> canExecuteDelegate)
            : this(executionDelegate)
        {
            this.canExecuteWithArgument = canExecuteDelegate;
        }

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution and CanExecute delegates.
        /// </summary>
        /// <remarks>
        ///     <para>The Execution parameter is passed to the delegate.</para>
        ///     <para>The CanExecute parameter is ignored.</para>
        /// </remarks>
        /// <param name="executionDelegate">The delegate to execute when ICommand.Execute(object parameter) is called.</param>
        /// <param name="canExecuteDelegate">The delegate to execute when ICommand.CanExecute(object parameter) is called.</param>
        public Command(Action<object> executionDelegate, Func<bool> canExecuteDelegate)
            : this(executionDelegate)
        {
            this.canExecute = canExecuteDelegate;
        }

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution and CanExecute delegates.
        /// </summary>
        /// <remarks>
        ///     The Execution/CanExecute parameters are passed to the delegates.
        /// </remarks>
        /// <param name="executionDelegate">The delegate to execute when ICommand.Execute(object parameter) is called.</param>
        /// <param name="canExecuteDelegate">The delegate to execute when ICommand.CanExecute(object parameter) is called.</param>
        public Command(Action<object> executionDelegate, Func<object, bool> canExecuteDelegate)
            : this(executionDelegate)
        {
            this.canExecuteWithArgument = canExecuteDelegate;
        }

        /// <summary>
        ///     Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        event EventHandler ICommand.CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        ///     Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">
        ///     Data used by the command. If the command does not require data to be passed, this object can be
        ///     set to null.
        /// </param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            if (this.canExecute == null)
            {
                if (this.canExecuteWithArgument == null)
                {
                    return true;
                }
                return this.canExecuteWithArgument(parameter);
            }
            return this.canExecute();
        }

        /// <summary>
        ///     The method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">
        ///     Data used by the command. If the command does not require data to be passed, this object can be
        ///     set to null.
        /// </param>
        void ICommand.Execute(object parameter)
        {
            if (this.execute == null)
            {
                this.executeWithArgument(parameter);
            }
            else
            {
                this.execute();
            }
        }
    }

    /// <summary>
    ///     Provides a generic implementation of the ICommand interface that allows Execute/CanExecute delegates to optionally
    ///     contain generic parameters.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the argument to be used when calling either the ICommand.Execute(parameter) or
    ///     ICommand.CanExecute(parameter) methods.
    /// </typeparam>
    [DebuggerStepThrough]
    public class Command<T> : ICommand
    {
        /// <summary>
        ///     The non-generic execution predicate to invoke on ICommand.CanExecute(object parameter).
        /// </summary>
        private readonly Func<bool> canExecute;

        /// <summary>
        ///     The generic execution predicate to invoke on ICommand.CanExecute(object parameter).
        /// </summary>
        private readonly Func<T, bool> canExecuteWithArgument;

        /// <summary>
        ///     The non-generic execution delegate to invoke on ICommand.Execute(object parameter).
        /// </summary>
        private readonly Action execute;

        /// <summary>
        ///     The generic execution delegate to invoke on ICommand.Execute(object parameter).
        /// </summary>
        private readonly Action<T> executeWithArgument;

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution delegate.
        /// </summary>
        /// <remarks>
        ///     <para>The Execution parameter is passed to the delegate as the generic type.</para>
        ///     <para>The CanExecute(parameter) method always returns true.</para>
        /// </remarks>
        /// <param name="executionDelegate">The generic delegate to execute when ICommand.Execute(object parameter) is called.</param>
        public Command(Action<T> executionDelegate)
        {
            if (executionDelegate == null)
            {
                throw new ArgumentNullException(nameof(executionDelegate));
            }

            this.executeWithArgument = executionDelegate;
        }

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution and CanExecute delegates.
        /// </summary>
        /// <remarks>
        ///     <para>The Execution parameter is ignored.</para>
        ///     <para>The CanExecute parameter is passed to the delegate as the generic type.</para>
        /// </remarks>
        /// <param name="executionDelegate">The non-generic delegate to execute when ICommand.Execute(object parameter) is called.</param>
        /// <param name="canExecuteDelegate">The generic delegate to execute when ICommand.CanExecute(object parameter) is called.</param>
        public Command(Action executionDelegate, Func<T, bool> canExecuteDelegate)
            : this(executionDelegate)
        {
            this.canExecuteWithArgument = canExecuteDelegate;
        }

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution and CanExecute delegates.
        /// </summary>
        /// <remarks>
        ///     <para>The Execution parameter is passed to the delegate as the generic type. </para>
        ///     <para>The CanExecute parameter is ignored.</para>
        /// </remarks>
        /// <param name="executionDelegate">The non-generic delegate to execute when ICommand.Execute(object parameter) is called.</param>
        /// <param name="canExecuteDelegate">The generic delegate to execute when ICommand.CanExecute(object parameter) is called.</param>
        public Command(Action<T> executionDelegate, Func<bool> canExecuteDelegate)
            : this(executionDelegate)
        {
            this.canExecute = canExecuteDelegate;
        }

        /// <summary>
        ///     Initializes a new instance of the Command class with the specified Execution and CanExecute delegates.
        /// </summary>
        /// <remarks>
        ///     The Execution parameter is passed to the delegate as the generic type.
        ///     The CanExecute parameters is passed to the delegate as the generic type.
        /// </remarks>
        /// <param name="executionDelegate">The generic delegate to execute when ICommand.Execute(object parameter) is called.</param>
        /// <param name="canExecuteDelegate">The generic delegate to execute when ICommand.CanExecute(object parameter) is called.</param>
        public Command(Action<T> executionDelegate, Func<T, bool> canExecuteDelegate)
            : this(executionDelegate)
        {
            this.canExecuteWithArgument = canExecuteDelegate;
        }

        /// <summary>
        ///     Initializes a new instance of the Command class with a non-generic execution delegate.
        /// </summary>
        /// <remarks>
        ///     The non-generic constructor is here only in the case that the constructor contains only a generic CanExecute
        ///     delegate and a non-generic Execute delegate.
        ///     This constructor is not intended to be invoked directly.
        /// </remarks>
        /// <param name="executionDelegate">The non-generic delegate to execute when ICommand.Execute(object parameter) is called.</param>
        private Command(Action executionDelegate)
        {
            if (executionDelegate == null)
            {
                throw new ArgumentNullException(nameof(executionDelegate));
            }

            this.execute = executionDelegate;
        }

        /// <summary>
        ///     Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        event EventHandler ICommand.CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        ///     Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">
        ///     Data used by the command. If the command does not require data to be passed, this object can be
        ///     set to null.
        /// </param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            if (this.canExecute == null)
            {
                if (this.canExecuteWithArgument == null)
                {
                    return true;
                }
                return this.canExecuteWithArgument((T)parameter);
            }
            return this.canExecute();
        }

        /// <summary>
        ///     The method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">
        ///     Data used by the command. If the command does not require data to be passed, this object can be
        ///     set to null.
        /// </param>
        void ICommand.Execute(object parameter)
        {
            if (this.execute == null)
            {
                this.executeWithArgument((T)parameter);
            }
            else
            {
                this.execute();
            }
        }
    }
}
