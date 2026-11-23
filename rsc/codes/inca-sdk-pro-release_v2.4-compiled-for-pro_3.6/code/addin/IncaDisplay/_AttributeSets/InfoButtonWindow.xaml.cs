using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IncaDisplay._AttributeSets
{
    /// <summary>
    /// Logique d'interaction pour InfoButtonWindow.xaml
    /// </summary>
    public partial class InfoButtonWindow : Window
    {
        public InfoButtonWindow()
        {
            InitializeComponent();
        }
        public InfoButtonWindow(AttributeSetsRule rule)
        {
            InitializeComponent();
            DataContext = new InfoButtonWindowViewModel(this, rule);  // la règle devient le DataContext
        }
    }
}
