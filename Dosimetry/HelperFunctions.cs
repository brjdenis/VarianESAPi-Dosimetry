using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Dosimetry
{
    public class HelperFunctions
    {
        public void ResetStyles(Window window)
        {
            // Restore original style (Optimizer changes some to black!)
            window.Resources.Add(typeof(Window), new Style(typeof(Window)));

            window.Resources.Add(typeof(Grid), new Style(typeof(Grid)));

            window.Resources.Add(typeof(DataGrid), new Style(typeof(DataGrid)));

            window.Resources.Add(typeof(Label), new Style(typeof(Label)));

            window.Resources.Add(typeof(Button), new Style(typeof(Button)));

            window.Resources.Add(typeof(ScrollViewer), new Style(typeof(ScrollViewer)));

            window.Resources.Add(typeof(ScrollBar), new Style(typeof(ScrollBar)));

            window.Resources.Add(typeof(StackPanel), new Style(typeof(StackPanel)));

            window.Resources.Add(typeof(Panel), new Style(typeof(Panel)));

            window.Resources.Add(typeof(Menu), new Style(typeof(Menu)));

            window.Resources.Add(typeof(MenuItem), new Style(typeof(MenuItem)));

            window.Resources.Add(typeof(ComboBox), new Style(typeof(ComboBox)));

            window.Resources.Add(typeof(ToolTip), new Style(typeof(ToolTip)));

            window.Resources.Add(typeof(TabControl), new Style(typeof(TabControl)));

            window.Resources.Add(typeof(TabItem), new Style(typeof(TabItem)));
        }
    }
}
