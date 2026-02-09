using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
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

        // Usamos string para o Input ficar fluido na UI
        private string _usdText;
        private string _brlText;
        
        private decimal _currentRate;
        private string _lastUpdated;
        private string _statusMessage;
        private bool _isLoading;
        
        // Flags para evitar loop infinito de atualização
        private bool _isCalculating;

        public event PropertyChangedEventHandler PropertyChanged;
        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; } // Comando para fechar a janela

        public MainViewModel()
        {
            _currencyService = new CurrencyService();
            _statusMessage = "Inicializando...";
            _usdText = "1.00"; // Valor inicial
            
            RefreshCommand = new RelayCommand(async _ => await LoadRateAsync());
            CloseCommand = new RelayCommand(_ => Application.Current.Shutdown());

            InitializeTimer();
            // Dispara sem esperar (Fire and Forget seguro)
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
                StatusMessage = "Sincronizando...";
                
                // Tenta API Comercial
                var rate = await _currencyService.GetCurrentRateAsync();

                if (rate != null && decimal.TryParse(rate.Bid, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal bidValue))
                {
                    CurrentRate = bidValue;
                    LastUpdated = DateTime.Now.ToString("HH:mm");
                    StatusMessage = "Mercado (Tempo Real)";
                }
                else
                {
                    // Fallback: Banco Central
                    StatusMessage = "Tentando BCB...";
                    var bcbRate = await _currencyService.GetOfficialBcbRateAsync();

                    if (bcbRate.HasValue)
                    {
                        CurrentRate = bcbRate.Value;
                        LastUpdated = "PTAX Oficial";
                        StatusMessage = "Fonte Oficial (BCB)";
                    }
                    else
                    {
                        StatusMessage = "Offline (Verifique a rede)";
                    }
                }

                // Recalcula baseado no que já está digitado (prioridade USD)
                CalculateFromUsd();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // --- Propriedades de Texto (Inputs) ---

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

        // --- Lógica de Cálculo ---

        private void CalculateFromUsd()
        {
            if (CurrentRate <= 0) return;

            // Tenta converter o texto do usuário para número
            if (decimal.TryParse(UsdText.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal usdVal))
            {
                _isCalculating = true;
                // Formata o BRL para exibir na tela
                BrlText = (usdVal * CurrentRate).ToString("N2", new CultureInfo("pt-BR"));
                _isCalculating = false;
            }
        }

        private void CalculateFromBrl()
        {
            if (CurrentRate <= 0) return;

            // Tenta converter o texto do usuário para número
            // Aceita tanto ponto quanto vírgula
            if (decimal.TryParse(BrlText.Replace(".", "").Replace(",", "."), NumberStyles.Any, new CultureInfo("pt-BR"), out decimal brlVal))
            {
                _isCalculating = true;
                // Formata o USD para exibir na tela
                UsdText = (brlVal / CurrentRate).ToString("N2", CultureInfo.InvariantCulture);
                _isCalculating = false;
            }
        }

        // --- Boilerplate ---
        public decimal CurrentRate { get => _currentRate; set { _currentRate = value; OnPropertyChanged(); } }
        public string LastUpdated { get => _lastUpdated; set { _lastUpdated = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged;
    }
}