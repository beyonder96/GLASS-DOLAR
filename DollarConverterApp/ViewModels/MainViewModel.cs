using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using DollarConverterApp.Models;
using DollarConverterApp.Services;

namespace DollarConverterApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly CurrencyService _currencyService;
        private DispatcherTimer _timer;

        private decimal _usdAmount;
        private decimal _brlAmount;
        private decimal _currentRate;
        private string _lastUpdated;
        private bool _isLoading;
        private bool _isUpdatingFromUsd;
        private bool _isUpdatingFromBrl;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            _currencyService = new CurrencyService();
            _usdAmount = 1.00m; // Default start
            
            InitializeTimer();
            LoadRateAsync();
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += async (s, e) => await LoadRateAsync();
            _timer.Start();
        }

        private async Task LoadRateAsync()
        {
            IsLoading = true;
            var rate = await _currencyService.GetCurrentRateAsync();
            IsLoading = false;

            if (rate != null && decimal.TryParse(rate.Bid, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal bidValue))
            {
                CurrentRate = bidValue;
                LastUpdated = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                
                // Refresh conversion
                RecalculateFromUsd();
            }
        }

        public decimal UsdAmount
        {
            get => _usdAmount;
            set
            {
                if (_usdAmount != value)
                {
                    _usdAmount = value;
                    OnPropertyChanged();
                    if (!_isUpdatingFromBrl)
                    {
                        RecalculateFromUsd();
                    }
                }
            }
        }

        public decimal BrlAmount
        {
            get => _brlAmount;
            set
            {
                if (_brlAmount != value)
                {
                    _brlAmount = value;
                    OnPropertyChanged();
                    if (!_isUpdatingFromUsd)
                    {
                        RecalculateFromBrl();
                    }
                }
            }
        }

        public decimal CurrentRate
        {
            get => _currentRate;
            set
            {
                if (_currentRate != value)
                {
                    _currentRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastUpdated
        {
            get => _lastUpdated;
            set
            {
                if (_lastUpdated != value)
                {
                    _lastUpdated = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        private void RecalculateFromUsd()
        {
            if (CurrentRate <= 0) return;
            
            _isUpdatingFromUsd = true;
            BrlAmount = Math.Round(UsdAmount * CurrentRate, 2);
            _isUpdatingFromUsd = false;
        }

        private void RecalculateFromBrl()
        {
            if (CurrentRate <= 0) return;

            _isUpdatingFromBrl = true;
            UsdAmount = Math.Round(BrlAmount / CurrentRate, 2);
            _isUpdatingFromBrl = false;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
