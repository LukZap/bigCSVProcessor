using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace CsvConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CancellationTokenSource cts;

        bool fileSelected = false;
        string fileToProcess;
        List<Control> afterFileSelectControls;
        IEnumerable<UserInfo> usersPagedList;

        int pageNumber;
        int pageSize = 9;
        int pageCount = 0;

        CsvProcessor processor;

        public MainWindow()
        {
            InitializeComponent();
            afterFileSelectControls = new List<Control>
            {
                processBtn
            };
            
            usersPagedList = new List<UserInfo>();
            processor = new CsvProcessor();
        }

        private void FileSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv|Txt Files (*.txt)|*.txt"
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                fileToProcess = dlg.FileName;
                string filename = dlg.FileName.Split('\\').Last();
                fileNameTextBox.Text = filename;
                fileSelected = true;
            }
            else
            {
                fileToProcess = null;
                fileNameTextBox.Text = "(wybierz plik)";
                fileSelected = false;
            }

            AfterFileDialog();
        }

        private void AfterFileDialog()
        {
            foreach (var control in afterFileSelectControls)
            {
                control.Visibility = fileSelected ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private async void Process_Click(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();

            try
            {
                processor.ProgressUpdate += UpdateProgressBar;
                await Task.Run(() => processor.Process(fileToProcess, cts.Token));
            }
            catch (FileFormatException)
            {
                MessageBox.Show("Niewłaściwa struktura pliku .csv");
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Przerwano procesowanie pliku");
            }
            finally
            {
                processor.ProgressUpdate -= UpdateProgressBar;
                int listCount = processor.FilteredList.Count();
                pageCount = (int)Math.Ceiling(listCount/(double)pageSize);
                UpdateProgressBar(null, new ProgressEventArgs { Progress = 0 });
                cts.Dispose();
            }
            
            GetPage(1);
        }
        
        public void UpdateProgressBar(object obj, ProgressEventArgs args)
        {
            progressBarCtrl.Dispatcher.Invoke(() => progressBarCtrl.Value = args.Progress);
        }

        private void GetPage(int pageNr)
        {
            pageNumber = pageNr;
            infoLbl.Content = $"Strona: {pageNumber}";
            usersPagedList = processor.Page(pageNumber, pageSize);
            listBox.Dispatcher.Invoke(() => listBox.ItemsSource = usersPagedList);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            int pageNr = pageNumber > 1 ? pageNumber - 1 : pageNumber;
            GetPage(pageNr);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            int pageNr = pageNumber < pageCount ? pageNumber + 1 : pageNumber;
            GetPage(pageNr);
        }

        private void First_Click(object sender, RoutedEventArgs e)
        {
            GetPage(1);
        }

        private void Last_Click(object sender, RoutedEventArgs e)
        {
            GetPage(pageCount);
        }
    }
}
