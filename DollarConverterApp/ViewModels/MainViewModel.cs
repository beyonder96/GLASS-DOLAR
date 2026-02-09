using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices; // <--- O erro estava na falta desta linha
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DollarConverterApp.Models;
using DollarConverterApp.Services;

namespace DollarConverterApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly CurrencyService _currencyService;
        private DispatcherTimer _timer;

        private string _usdText;
        private string _brlText;
        
        private decimal _currentRate;
        private string _lastUpdated;
        private string _statusMessage;
        private bool _isLoading;
        
        private bool _isCalculating;
        private DateTime _selectedDate;

        public event PropertyChangedEventHandler? PropertyChanged;
        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }

        public MainViewModel()
        {
            _currencyService = new CurrencyService();
            _statusMessage = "Iniciando...";
            _usdText = "1.00";
            _brlText = "0,00";
            _lastUpdated = "-";
            
            SelectedDate = DateTime.Today;

            RefreshCommand = new RelayCommand(async _ => await LoadRateAsync());
            CloseCommand = new RelayCommand(_ => Application.Current.Shutdown());

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += async (s, e) => await LoadRateAsync();
            _timer.Start();

            _ = LoadRateAsync();
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged();
                    _ = LoadRateAsync();
                }
            }
        }

        private async Task LoadRateAsync()
        {
            if (IsLoading) return;

            try 
            {
                IsLoading = true;
                StatusMessage = "Buscando...";
                
                decimal? rate = null;
                bool isToday = SelectedDate.Date >= DateTime.Today;
                
                if (isToday)
                {
                    rate = await _currencyService.GetRealTimeRateAsync();
                    if (rate.HasValue) 
                        StatusMessage = "Mercado (Ao Vivo)";
                    else 
                        StatusMessage = "Erro API (Tentando Fallback)";
                }
                else
                {
                    StatusMessage = $"Histórico ({SelectedDate:dd/MM})...";
                    rate = await _currencyService.GetHistoricalRateAsync(SelectedDate);
                }

                if (rate.HasValue)
                {
                    CurrentRate = rate.Value;
                    LastUpdated = DateTime.Now.ToString("HH:mm");
                }
                else if (CurrentRate == 0)
                {
                    CurrentRate = 5.80m;
                    StatusMessage = "⚠️ Offline (Taxa Fixa)";
                }

                CalculateFromUsd();
            }
            catch
            {
                StatusMessage = "Erro Crítico";
                if (CurrentRate == 0) CurrentRate = 5.80m;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public string UsdText
        {
            get => _usdText;
            set
            {
                if (_usdText != value)
                {
                    _usdText = value;
                    OnPropertyChanged();
                    if (!_isCalculating) CalculateFromUsd();
                }
            }
        }

        public string BrlText
        {
            get => _brlText;
            set
            {
                if (_brlText != value)
                {
                    _brlText = value;
                    OnPropertyChanged();
                    if (!_isCalculating) CalculateFromBrl();
                }
            }
        }

        private void CalculateFromUsd()
        {
            if (CurrentRate <= 0) return;
            if (decimal.TryParse(UsdText.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal usdVal))
            {
                _isCalculating = true;
                BrlText = (usdVal * CurrentRate).ToString("N2", new CultureInfo("pt-BR"));
                _isCalculating = false;
            }
        }

        private void CalculateFromBrl()
        {
            if (CurrentRate <= 0) return;
            string cleanInput = BrlText.Replace(".", ""); 
            if (decimal.TryParse(cleanInput, NumberStyles.Any, new CultureInfo("pt-BR"), out decimal brlVal))
            {
                _isCalculating = true;
                UsdText = (brlVal / CurrentRate).ToString("N2", CultureInfo.InvariantCulture);
                _isCalculating = false;
            }
        }

        public decimal CurrentRate { get => _currentRate; set { _currentRate = value; OnPropertyChanged(); } }
        public string LastUpdated { get => _lastUpdated; set { _lastUpdated = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
        
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}