using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        private decimal _usdAmount;
        private decimal _brlAmount;
        private decimal _currentRate;
        private string _lastUpdated;
        private string _statusMessage; // Nova propriedade para feedback
        private bool _isLoading;
        
        // Flags para evitar loop infinito de atualização cruzada
        private bool _isUpdatingFromUsd;
        private bool _isUpdatingFromBrl;

        public event PropertyChangedEventHandler PropertyChanged;

        // Comando para o botão de atualizar
        public ICommand RefreshCommand { get; }

        public MainViewModel()
        {
            _currencyService = new CurrencyService();
            _usdAmount = 1.00m; 
            _statusMessage = "Pronto";

            // Inicializa o comando
            RefreshCommand = new RelayCommand(async _ => await LoadRateAsync());
            
            InitializeTimer();
            // Dispara a carga inicial
            _ = LoadRateAsync(); 
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
    if (IsLoading) return;

    try 
    {
        IsLoading = true;
        StatusMessage = "Buscando cotação comercial...";
        
        // 1. Tenta API Comercial (Rápida e Recente)
        var rate = await _currencyService.GetCurrentRateAsync();

        if (rate != null && decimal.TryParse(rate.Bid, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal bidValue))
        {
            CurrentRate = bidValue;
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            StatusMessage = $"Mercado: R$ {CurrentRate:N2}"; // Feedback visual da fonte
            RecalculateFromUsd();
        }
        else
        {
            // 2. FALLBACK: Se falhar, tenta Banco Central
            StatusMessage = "Falha no mercado. Tentando BCB...";
            var bcbRate = await _currencyService.GetOfficialBcbRateAsync();

            if (bcbRate.HasValue)
            {
                CurrentRate = bcbRate.Value;
                LastUpdated = "PTAX (Oficial)";
                StatusMessage = $"Fonte Oficial (BCB): R$ {CurrentRate:N4}";
                RecalculateFromUsd();
            }
            else
            {
                StatusMessage = "Sem conexão com APIs.";
            }
        }
    }
    catch (Exception ex)
    {
        StatusMessage = $"Erro crítico: {ex.Message}";
    }
    finally
    {
        IsLoading = false;
    }
}

        // --- Propriedades ---

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
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
                    if (!_isUpdatingFromBrl) RecalculateFromUsd();
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
                    if (!_isUpdatingFromUsd) RecalculateFromBrl();
                }
            }
        }

        public decimal CurrentRate
        {
            get => _currentRate;
            set { _currentRate = value; OnPropertyChanged(); }
        }

        public string LastUpdated
        {
            get => _lastUpdated;
            set { _lastUpdated = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // --- Lógica de Conversão ---

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

    // Mini-implementação de ICommand para não precisarmos de libs externas agora
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}