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
                InfoString = s_defInfoStr;
            };
        }

        ~Information()
        {
            m_timer?.Dispose();
        }

        public string InfoString
        {
            get => m_infoString;
            set
            {
                m_infoString = value;
                Notify();

                m_timer.Start();
            }
        }
    }
}
