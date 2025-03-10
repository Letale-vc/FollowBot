using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FollowBot.Class
{
    public class DefensiveSkillsClass : INotifyPropertyChanged
    {
        private readonly Stopwatch delaySw = Stopwatch.StartNew();

        public DefensiveSkillsClass()
        {
        }

        public DefensiveSkillsClass(bool enabled, string name, bool useEs, int threshold, double sleepSeconds,
            bool castOnLeader)
        {
            Enabled = enabled;
            Name = name;
            UseEs = useEs;
            Threshold = threshold;
            SleepSeconds = sleepSeconds;
            CastOnLeader = castOnLeader;
        }

        public DefensiveSkillsClass(bool enabled, string name, bool useEs, int threshold, double sleepSeconds,
            bool castOnLeader, string linkWhitelist)
        {
            Enabled = enabled;
            Name = name;
            UseEs = useEs;
            Threshold = threshold;
            SleepSeconds = sleepSeconds;
            CastOnLeader = castOnLeader;
            LinkWhitelist = linkWhitelist;
        }

        private string _name { get; set; }
        private bool _enabled { get; set; }
        private bool _useEs { get; set; }
        private int _threshold { get; set; }
        private double _sleepSeconds { get; set; }
        private bool _castOnLeader { get; set; }
        private string _linkWhitelist { get; set; } = "";

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
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

        public int Threshold
        {
            get => _threshold;
            set
            {
                _threshold = value;
                NotifyPropertyChanged();
            }
        }

        public double SleepSeconds
        {
            get => _sleepSeconds;
            set
            {
                _sleepSeconds = Math.Round(value, 1, MidpointRounding.AwayFromZero);
                NotifyPropertyChanged();
            }
        }

        public bool CastOnLeader
        {
            get => _castOnLeader;
            set
            {
                _castOnLeader = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsReadyToCast => delaySw.ElapsedMilliseconds > SleepSeconds * 1000;

        public string LinkWhitelist
        {
            get => _linkWhitelist;
            set
            {
                _linkWhitelist = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Casted()
        {
            delaySw.Restart();
        }

        [NotifyPropertyChangedInvocator]
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}