using Microsoft.Win32;
using System;
using System.Windows;

namespace Buses
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {      
        private PathAnalysis m_PathAnalysis { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            TimeStart.Text = DateTime.Now.ToString("HH:mm");
        }


        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                m_PathAnalysis = new PathAnalysis(openFileDialog.FileName);
                DataContext = m_PathAnalysis;
            }
        }

        private void CheapButton_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
                if (m_PathAnalysis == null)
                {
                    throw new Exception("Не выбран файл");
                }

                int start = (int)Start.SelectedItem;
                int end = (int)End.SelectedItem;
                DateTime time = Convert.ToDateTime(TimeStart.Text);

                m_PathAnalysis.FindCheap(start, end, time);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void FastButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_PathAnalysis == null)
                {
                    throw new Exception("Не выбран файл");
                }

                int start = (int)Start.SelectedItem;
                int end = (int)End.SelectedItem;
                DateTime time = Convert.ToDateTime(TimeStart.Text);

                m_PathAnalysis.FindFast(start, end, time);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}