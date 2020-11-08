using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MandelbrotSet
{
    public class Information : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string m_infoString= string.Empty;

        public string InfoString
        {
            get => m_infoString;
            set
            {
                m_infoString = value;
                Notify();
            }
        }
    }
}
