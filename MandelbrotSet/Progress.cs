using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MandelbrotSet
{
    public class Progress: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private int m_renderingProgress = -1;
        private int m_savingProgress = -1;

        public int RenderingProgress
        {
            get => m_renderingProgress;
            private set
            {
                m_renderingProgress = value;
                Notify();
                Notify(nameof(ShowRenderingProgress));
            }
        }
        public bool ShowRenderingProgress { get => m_renderingProgress >= 0; }
        public int SavingProgress
        {
            get => m_savingProgress;
            private set
            {
                m_savingProgress = value;
                Notify();
                Notify(nameof(ShowSavingProgress));
            }
        }
        public bool ShowSavingProgress { get => m_savingProgress >= 0; }

        public void UpdateProgress(int progress, bool isForRendering)
        {
            if (isForRendering) {
                RenderingProgress = progress;
            }
            else {
                SavingProgress = progress;
            }
        }
    }
}
