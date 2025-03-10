using JetBrains.Annotations;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FollowBot.Class
{
    public class FlasksClass : INotifyPropertyChanged
    {
        [JsonIgnore] public readonly Stopwatch PostUseDelay = Stopwatch.StartNew();

        private int _cooldown;
        private bool _enabled;
        private bool _ignoreEffect;
        private int _slot;
        private int _threshold;
        private bool _useEs;
        private bool _useMana;

        public FlasksClass()
        {
        }

        public FlasksClass(bool enabled, int slot, bool useEs, bool useMana, int threshold, int cooldown,
            bool ignoreEffect)
        {
            Enabled = enabled;
            Slot = slot;
            UseEs = useEs;
            UseMana = useMana;
            Threshold = threshold;
            Cooldown = cooldown;
            IgnoreEffect = ignoreEffect;
        }

        public int Slot
        {
            get => _slot;
            set
            {
                _slot = value;
                NotifyPropertyChanged();
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool UseEs
        {
            get => _useEs;
            set
            {
                _useEs = value;
                NotifyPropertyChanged();
            }
        }

        public bool UseMana
        {
            get => _useMana;
            set
            {
                _useMana = value;
                NotifyPropertyChanged();
            }
        }

        public int Threshold
        {
            get => _threshold;
            set
            {
                _threshold = value;
                NotifyPropertyChanged();
            }
        }

        public int Cooldown
        {
            get => _cooldown;
            set
            {
                _cooldown = value;
                NotifyPropertyChanged();
            }
        }

        public bool IgnoreEffect
        {
            get => _ignoreEffect;
            set
            {
                _ignoreEffect = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}