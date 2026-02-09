using System.Windows;
using DollarConverterApp.ViewModels;

namespace DollarConverterApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}