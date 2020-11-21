using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;

namespace MandelbrotSet
{
    public class Information : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private static readonly string s_defInfoStr = "No Error";
        private string m_infoString= string.Empty;

        private Timer m_timer = new Timer(5000);

        public Information()
        {
            m_timer.AutoReset = false;
            m_timer.Elapsed += (sending, arg) =>
            {
                SetString(s_defInfoStr);
            };
        }

        ~Information()
        {
            m_timer?.Dispose();
        }

        public void SetString(string value)
        {
            m_infoString = value;
            Notify();

            m_timer.Start();
        }
    }
}
