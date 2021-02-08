using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

namespace base_patch {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow:Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void btn_add_Click(object sender, RoutedEventArgs e) {
            using(System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog()) {
                dialog.Multiselect = true;
                if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    foreach(var item in dialog.FileNames) {
                        AddFile(item);
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            if(!File.Exists("base.scs")) {
                MessageBox.Show("请将补丁安装工具放置在base.scs同级目录中");
                Environment.Exit(0);
                return;
            }

            DirectoryInfo theFolder = new DirectoryInfo(Environment.CurrentDirectory);
            FileInfo[] fileInfo = theFolder.GetFiles();
            foreach(FileInfo NextFile in fileInfo) {
                if(NextFile.Extension == ".patch") {
                    AddFile(NextFile.Name);
                }
            }
        }

        private void AddFile(string path) {
            foreach(FileItem item in list_file.Items) {
                if(item.Path.Equals(path)) return;
            }
            FileType type;
            try {
                using(BinaryReader br = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read))) {
                    uint head = br.ReadUInt32();
                    if(head == 0x04034B50) {
                        type = FileType.ZIP;
                    } else if(head == 0x21726152) {
                        type = FileType.RAR;
                    } else if(head == 0xAFBC7A37) {
                        type = FileType.Z7;
                    } else if(head == 0x08088B1F) {
                        type = FileType.GZ;
                    } else {
                        MessageBox.Show("补丁包格式错误");
                        return;
                    }
                }
                list_file.Items.Add(new FileItem(path, type));
            }catch(IOException ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void list_file_Drop(object sender, DragEventArgs e) {
            string msg = "Drop";
            if(e.Data.GetDataPresent(DataFormats.FileDrop)) {
                msg = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            }
            AddFile(msg);
        }

        private void list_file_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            StringBuilder sb = new StringBuilder();
            foreach(FileItem item in list_file.SelectedItems) {
                if(item.Type == FileType.ZIP) {
                    sb.AppendLine(item.Path+"：");
                    using(ZipArchive zip = ZipFile.Open(item.Path, ZipArchiveMode.Read)) {
                        ZipArchiveEntry entry = zip.GetEntry("readme.txt");

                        if(entry!=null) {
                            using(StreamReader sr = new StreamReader(entry.Open())) {
                                sb.AppendLine(sr.ReadToEnd());
                            }
                        } else {
                            sb.AppendLine("无补丁信息");
                        }
                    }
                    sb.AppendLine("==================");
                } else {
                    MessageBox.Show("不支持该补丁格式");
                }
            }
            text_info.Text = sb.ToString();
        }

        private class FileItem {
            public string Path { get; }
            public FileType Type { get; }
            public FileItem(string path,FileType type) {
                Path = path;
                Type = type;
            }
            public override string ToString() => Path;
        }

        private enum FileType { ZIP, RAR, Z7, GZ};

        private void btn_patch_Click(object sender, RoutedEventArgs e) {
            if(!File.Exists("base.scs")) {
                MessageBox.Show("base.scs缺失");
                return;
            }
            using(ZipArchive baseFile = ZipFile.Open("base.scs", ZipArchiveMode.Update)) {
                foreach(FileItem item in list_file.SelectedItems) {
                    using(ZipArchive zip = ZipFile.Open(item.Path, ZipArchiveMode.Read)) {
                        foreach(ZipArchiveEntry entry in zip.Entries) {
                            if(entry.FullName.Equals("readme.txt")) continue;

                            ZipArchiveEntry baseEntry = baseFile.GetEntry(entry.FullName);
                            if(baseEntry == null) baseEntry = baseFile.CreateEntry(entry.FullName, CompressionLevel.NoCompression);

                            Stream sr = entry.Open();
                            Stream sw = baseEntry.Open();
                            sw.SetLength(0);
                            byte[] buffer = new byte[2048];
                            int len;
                            while(true) {
                                len = sr.Read(buffer, 0, buffer.Length);
                                sw.Write(buffer, 0, len);
                                if(len==0) break;
                            }
                            sw.Close();
                            sr.Close();
                        }
                    }
                }
            }
            MessageBox.Show("安装完成");
        }

        private void btn_del_Click(object sender, RoutedEventArgs e) {
            List<FileItem> delList = new List<FileItem>();
            foreach(FileItem item in list_file.SelectedItems) {
                delList.Add(item);
            }
            foreach(FileItem item in delList) {
                list_file.Items.Remove(item);
            }
        }
    }
}
