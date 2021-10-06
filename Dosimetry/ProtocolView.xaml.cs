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

namespace Dosimetry
{
    /// <summary>
    /// Interaction logic for ProtocolView.xaml
    /// </summary>
    public partial class ProtocolView : Window
    {
        public ProtocolView(List<MainWindow.DataGridProtocolView> datagridProtocol, string protocolName)
        {

            // Prevent too large windows:
            this.MaxWidth = System.Windows.SystemParameters.FullPrimaryScreenWidth;
            this.MaxHeight = System.Windows.SystemParameters.FullPrimaryScreenHeight;
            InitializeComponent();

            ListCollectionView collectionView1 = new ListCollectionView(datagridProtocol);
            collectionView1.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
            this.DataGridProtocol.ItemsSource = collectionView1;

            this.Title = protocolName;
        }
    }
}
