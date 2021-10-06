using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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
using System.Xml;
using System.Xml.Schema;

namespace Dosimetry
{
    /// <summary>
    /// Interaction logic for EditXML.xaml
    /// </summary>
    public partial class EditXML : Window
    {
        public string filename;
        public List<int> SearchedLineList = new List<int>() { };
        public int CurrentLine = 0;
        public int NumberOfLines;
        public string XMLtype;

        private string XMLValidationString = "";

        public EditXML(string filename, string xmltype)
        {
            this.filename = filename;
            this.XMLtype = xmltype;
            InitializeComponent();

            this.MaxWidth = System.Windows.SystemParameters.FullPrimaryScreenWidth;
            this.MaxHeight = System.Windows.SystemParameters.FullPrimaryScreenHeight;
            EditXMLtextbox.Text = File.ReadAllText(filename);
            this.Background = Brushes.AliceBlue;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ValidateXML();
            if (XMLValidationString != "")
            {
                MessageBox.Show("XML syntax not correct!\n" + XMLValidationString, "Error");
                XMLValidationString = "";
                return;
            }

            EditXMLSavedLabelOK.Content = "";
            EditXMLSavedLabelOK.Foreground = Brushes.Green;
            EditXMLPassword passWindow = new EditXMLPassword();
            passWindow.ShowDialog();

            String passCorrect = passWindow.passwordCorrect;

            if (passCorrect == "true")
            {
                try
                {
                    File.WriteAllText(this.filename, EditXMLtextbox.Text);
                    EditXMLSavedLabelOK.Content = "Saved!";
                }
                catch
                {
                    EditXMLSavedLabelOK.Content = "Error while saving!";
                    EditXMLSavedLabelOK.Foreground = Brushes.Red;
                }
                // Read from file again
                EditXMLtextbox.Text = File.ReadAllText(this.filename);
            }
        }


        private void ValidateXML()
        {
            if (this.XMLtype == "Protocols")
            {
                var validation = new XmlProtocolValidation(EditXMLtextbox.Text, System.IO.Path.ChangeExtension(this.filename, ".xsd"));
                validation.Validate();
                this.XMLValidationString = validation.XMLValidationString;
            }
            else if (this.XMLtype == "ProtocolGroups")
            {
                var validation = new XmlProtocolGroupsValidation(EditXMLtextbox.Text, System.IO.Path.ChangeExtension(this.filename, ".xsd"));
                validation.Validate();
                this.XMLValidationString = validation.XMLValidationString;
            }
            else if (this.XMLtype == "StructureMapping")
            {
                var validation = new XmlStructureMappingValidation(EditXMLtextbox.Text, System.IO.Path.ChangeExtension(this.filename, ".xsd"));
                validation.Validate();
                this.XMLValidationString = validation.XMLValidationString;
            }
            else if (this.XMLtype == "MachineSettings")
            {
                var validation = new MachineSettings(this.filename);
                validation.Validate(EditXMLtextbox.Text, System.IO.Path.ChangeExtension(this.filename, ".xsd"));
                this.XMLValidationString = validation.XMLValidationString;
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (CurrentLine < SearchedLineList.Count() - 1)
            {
                ++CurrentLine;
                EditXMLtextbox.Select(EditXMLtextbox.GetCharacterIndexFromLineIndex(SearchedLineList.ElementAt(CurrentLine)), 0);
                EditXMLtextbox.ScrollToLine(SearchedLineList.ElementAt(CurrentLine));
                changeButton4Label();
                EditXMLtextbox.Focus();
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (CurrentLine > 0)
            {
                --CurrentLine;
                EditXMLtextbox.Select(EditXMLtextbox.GetCharacterIndexFromLineIndex(SearchedLineList.ElementAt(CurrentLine)), 0);
                EditXMLtextbox.ScrollToLine(SearchedLineList.ElementAt(CurrentLine));
                changeButton4Label();
                EditXMLtextbox.Focus();
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            NumberOfLines = EditXMLtextbox.LineCount;
            List<int> LineList = new List<int>() { };
            for (int line = 0; line < NumberOfLines; line++)
            {
                if (EditXMLtextbox.GetLineText(line).IndexOf(EditXMLSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    LineList.Add(line);
                }
            }
            SearchedLineList = LineList;
            CurrentLine = 0;

            if (SearchedLineList.Count() > 0)
            {
                EditXMLtextbox.Select(EditXMLtextbox.GetCharacterIndexFromLineIndex(SearchedLineList.ElementAt(0)), 0);
                EditXMLtextbox.ScrollToLine(SearchedLineList.ElementAtOrDefault(0));
                EditXMLtextbox.Focus();
            }
            changeButton4Label();
        }

        private void changeButton4Label()
        {
            int CurrentLineNum = 0;
            if (SearchedLineList.Count() > 0)
            {
                CurrentLineNum = SearchedLineList.ElementAt(CurrentLine);
            }
            Button4.Content = "Search (" + CurrentLineNum.ToString() + "/" + SearchedLineList.Count().ToString() + ")";
        }
    }
}
