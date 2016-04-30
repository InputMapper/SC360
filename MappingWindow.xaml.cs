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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ODIF;
namespace SC360
{
    /// <summary>
    /// Interaction logic for DeviceList.xaml
    /// </summary>
    public partial class MappingWindow : Page
    {
        public MappingWindow()
        {
            InitializeComponent();
            
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Click");
        }
    }
}
