using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace chinese_translate {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow:Window {
        public MainWindow() {
            InitializeComponent();
        }
        Dictionary<char, string> encord = new Dictionary<char, string>();

        private void btnOK_Click(object sender, RoutedEventArgs e) {
            string inputText = textInput.Text;
            inputText = inputText.Replace("\r", "");
            StringBuilder sb = new StringBuilder("<align "+inputText.Replace("\n", "")+"></align>");
            string temp;
            for(int i = 0; i<inputText.Length; i++) {
                if(encord.TryGetValue(inputText[i], out temp)) {
                    sb.Append(temp);
                } else {
                    switch(inputText[i]) {
                        case '\n': sb.Append("<br>"); break;
                        case ' ': sb.Append(" "); break;
                        default:
                            MessageBox.Show("字库不支持：'"+ inputText[i] +"'");
                            label1.Content = "字库不支持：'"+ inputText[i] +"'";
                            break;
                    }
                }
            }
            textOutput.Text = sb.ToString();
            label1.Content = "转换完成";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            StreamReader sr = new StreamReader(new MemoryStream(Properties.Resources.encord));
            string temp;
            do {
                temp = sr.ReadLine();
                if(temp.Length<2) continue;
                encord.Add(temp[0], temp.Substring(2));
            } while(!sr.EndOfStream);
            sr.Close();
            label1.Content = "字库中共有" + encord.Count + "个字";
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e) {
            Clipboard.SetData(DataFormats.Text, textOutput.Text);
        }

        private void btnClear_Click(object sender, RoutedEventArgs e) {
            textInput.Clear();
            textOutput.Clear();
            label1.Content = "";
        }
    }
}
