using System;
using System.Windows.Input;

namespace MandelbrotSet
{
    public class GeneralCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private Func<bool> m_canExecutable;
        private Action m_execute;

        public GeneralCommand(Func<bool> canExecutable, Action execute)
        {
            m_canExecutable = canExecutable;
            m_execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return m_canExecutable?.Invoke() ?? false;
        }

        public void Execute(object parameter)
        {
            m_execute?.Invoke();
        }

        public void NotifyCanExecuteChagned()
        {
            CanExecuteChanged(this, EventArgs.Empty);
        }
    }
}
