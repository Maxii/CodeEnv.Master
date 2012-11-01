using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
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

namespace Program {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>    
    public partial class MainWindow : Window {

        PictureList picList = null;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            picList = new PictureList();
            picList.Init();
            string picName = picList.peek();
            DisplayPicture(picName);
        }


        private void PrevButton_Click(object sender, RoutedEventArgs e) {
            string prevPicName = picList.Previous();
            DisplayPicture(prevPicName);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) {
            string nextPicName = picList.Next();
            DisplayPicture(nextPicName);
        }

        private void DisplayPicture(string picName) {
            BitmapImage bImage = new BitmapImage(new Uri(picName));
            imagePicture.Source = bImage;
            labelPath.Content = picName;
        }

    }
}
