using Microsoft.Win32;
using NE4ZUGFeRD.Logic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace NE4ZUGFeRD.Views
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Title += string.Format(" v.{0}", fvi.FileVersion);
        }

        private void BtnAddZUGFeRD_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnShowZUGFeRD_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "PDF Dateien (*.pdf)|*.pdf";
            ofd.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            
            if (ofd.ShowDialog() == true)
            {
                string message = string.Empty;

                NE4ZUGFeRDConverter ne4zc = new NE4ZUGFeRDConverter();
                if (File.Exists(ofd.FileName))
                {
                    Exception ex = ne4zc.ShowZUGFeRD(ofd.FileName, out message);
                    if (ex != null)
                        MessageBox.Show(ex.Message);

                    MessageBox.Show(message);
                }
                else
                    MessageBox.Show("Keine Datei gefunden!");
            }
        }

        private void BtnAddZUGFeRDExample_Click(object sender, RoutedEventArgs e)
        {
            string inputPDF = string.Format(@"{0}\Templates\Template.Rechnung.v.1.0.pdf", Environment.CurrentDirectory);
            string outputPDF = string.Format(@"{0}\{1}_{2}.pdf",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ZUGFeRD.konforme.Basic.pdf",
                DateTime.Now.ToString("yyyyMMddHHmmss"));
            byte[] zugferdData;

            //make a copy from source pdf
            File.Copy(inputPDF, outputPDF, true);

            //create ZUGFeRD and attach it to pdf
            NE4ZUGFeRDConverter ne4zc = new NE4ZUGFeRDConverter();
            if (ne4zc.CreateSampleZugferdXML(out zugferdData) == null)
            {
                Exception ex = ne4zc.AttachZUGFeRD(outputPDF, zugferdData);
                if (ex == null)
                    MessageBox.Show("ZUGFeRD eingebettet!");
                else
                    MessageBox.Show(ex.Message);
            }
        }
    }
}
