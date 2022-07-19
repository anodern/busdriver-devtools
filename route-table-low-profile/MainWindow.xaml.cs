﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace route_table_low_profile {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow:Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void routeGrid_Loaded(object sender, RoutedEventArgs e) {
            for(int i = 0; i<6; i++) {
                for(int j = 0; j<6; j++) {
                    string lbName = "label_"+i+"_"+j;
                    Label label = new Label {
                        Name = lbName,
                        Margin = new Thickness(2),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(2),
                        FontSize = 13
                    };
                    label.MouseLeftButtonDown += new MouseButtonEventHandler(Label_MouseLeftButtonDown);

                    routeGrid.Children.Add(label);
                    routeGrid.RegisterName(lbName, label);   //注册控件
                    Grid.SetColumn(label, i);
                    Grid.SetRow(label, j);
                    routeLabelTable[i, j] = label;
                }
            }
        }

        private const byte DEFAULT_TIER = 10;
        private const byte DEFAULT_RANK = 10;
        private const byte MAX_TIER = 6;
        private const byte MAX_RANK = 6;

        private List<RouteData> dataList = new List<RouteData>();
        private RouteData[,] routeDataTable = new RouteData[MAX_TIER, MAX_RANK];
        private readonly Label[,] routeLabelTable = new Label[MAX_TIER, MAX_RANK];
        private RouteData selectedData;

        private string randomGuid;

        private int CountChar(string source, char chr) {
            int count = 0;
            foreach(char c in source) {
                if(c == chr) count++;
            }
            return count;
        }

        private void btn_load_Click(object sender, RoutedEventArgs e) {
            if(!File.Exists("base.scs")) {
                MessageBox.Show("找不到base.scs");
                return;
            }
            dataList.Clear();
            routeDataTable = new RouteData[MAX_TIER, MAX_RANK];
            selectedData = null;
            routeList.Items.Clear();
            combo_vehicle.Items.Clear();

            List<string> routesPath = new List<string>();
            List<string> vehiclePath = new List<string>();

            unzip();

            foreach(FileSystemInfo file in new DirectoryInfo("./tmp/def/route").GetFileSystemInfos()) {
                if(".sii".Equals(file.Extension, StringComparison.OrdinalIgnoreCase)) {
                    routesPath.Add(file.Name);
                }
            }

            foreach(FileSystemInfo file in new DirectoryInfo("./tmp/vehicle/driveable").GetFileSystemInfos()) {
                if(".sii".Equals(file.Extension, StringComparison.OrdinalIgnoreCase)) {
                    vehiclePath.Add(file.Name);
                }
            }


            Match match;
            string all;
            foreach(string s in vehiclePath) {
                using(StreamReader sr = new StreamReader("./tmp/vehicle/driveable/" + s)) {
                    all = sr.ReadToEnd();
                }
                //正则匹配车型
                match = Regex.Match(all, @"^[\s\S]+driveable_vehicle_data\s*:\s*vehicle.(\S+)\s*{[\s\S]+$");
                if(!match.Success) {
                    MessageBox.Show("车型'"+s+"'可能格式存在问题");
                    continue;
                }
                combo_vehicle.Items.Add(match.Groups[1].Value.ToLower());
            }

            foreach(string s in routesPath) {
                using(StreamReader sr = new StreamReader("./tmp/def/route/" + s)) {
                    all = sr.ReadToEnd();
                }
                //正则匹配线路
                match = Regex.Match(all, @"^[\s\S]+mission\s*:\s*mission.(\S+)\s*{[\s\S]+tier\s*:\s*(\d+)[\s\S]+rank\s*:\s*(\d+)[\s\S]+vehicle_data\s*:\s*vehicle.(\S+)[\s\S]+$");
                if(!match.Success) {
                    MessageBox.Show("线路'"+s+"'可能格式存在问题");
                    continue;
                }
                if(!byte.TryParse(match.Groups[2].Value, out byte tier)) {
                    tier = 10;
                }
                if(!byte.TryParse(match.Groups[3].Value, out byte rank)) {
                    rank = 10;
                }

                RouteData data = new RouteData(s, match.Groups[1].Value, tier, rank, match.Groups[4].Value.ToLower());
                dataList.Add(data);
                //Debug.WriteLine(data);

                if(tier>=6 || rank>=6) {
                    routeList.Items.Add(data);
                    continue;
                }
                if(routeDataTable[tier, rank]!=null) {
                    //位置重复
                    MessageBox.Show(string.Format("{0} 与 {1} 位置重复", data, routeDataTable[tier, rank]));
                    data.Tier=10;
                    data.Rank=10;
                    routeList.Items.Add(data);
                    continue;
                }
                routeDataTable[tier, rank] = data;
                routeLabelTable[tier, rank].Content = data.GetLabel();
            }

            btn_save.IsEnabled = true;
            label_info.Content = "加载完成";
        }
        private void btn_save_Click(object sender, RoutedEventArgs e) {
            btn_start.IsEnabled = false;
            foreach(RouteData data in dataList) {
                //跳过未修改的线路
                if(data.Tier == data.TierP && data.Rank == data.RankP && data.VehicleP.Equals(data.Vehicle)) continue;

                string routeContext;
                using(StreamReader sr = new StreamReader("./tmp/def/route/"+data.FullName)) {
                    routeContext = sr.ReadToEnd();
                }
                routeContext = Regex.Replace(routeContext,
                    @"^([\s\S]+tier\s*:\s*)\d+([\s\S]+rank\s*:\s*)\d+([\s\S]+vehicle_data\s*:\s*vehicle.)\S+([\s\S]+)$",
                    "${1}"+data.Tier+"${2}"+data.Rank+"${3}"+data.Vehicle+"${4}");


                using(StreamWriter writer = new StreamWriter("./tmp/def/route/"+data.FullName)) {
                    writer.BaseStream.Seek(0, SeekOrigin.Begin);
                    writer.BaseStream.SetLength(0);
                    writer.Write(routeContext);
                }
                

                data.ResetP();
            }
            zip();
            label_info.Content = "已保存";
            btn_start.IsEnabled = true;
        }


        private void unzip() {
            btn_start.IsEnabled = false;
            if(Directory.Exists("./tmp/def/route") || Directory.Exists("./tmp/vehicle/driveable")) {
                deleteTemp();
            }

            if(!Directory.Exists("./tmp")) {
                Directory.CreateDirectory("./tmp");
            }

            ProcessStartInfo p = new ProcessStartInfo {
                FileName = "7za.exe",
                Arguments = @"x base.scs -o.\tmp def\route\*.sii vehicle\driveable\*.sii",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process x = Process.Start(p);
            x.WaitForExit();
            btn_start.IsEnabled = true;
        }

        private void zip() {
            ProcessStartInfo p = new ProcessStartInfo {
                FileName = "7za.exe",
                Arguments = @"a base.scs .\tmp\* -tzip",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            
            Process x = Process.Start(p);
            x.WaitForExit();
        }

        private void deleteTemp() {
            string srcPath = @"./tmp";
            try {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
                foreach(FileSystemInfo i in fileinfo) {
                    if(i is DirectoryInfo){
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);
                    } else {
                        File.Delete(i.FullName);
                    }
                }
            } catch(Exception) {
                MessageBox.Show("清理临时文件出现错误，建议手动删除tmp目录");
            }
        }

        private void SetSelectedData() {
            if(selectedData == null) {
                label_name.Content = string.Empty;
                floatLabel.Content = string.Empty;
                combo_vehicle.SelectedIndex=-1;
                return;
            }
            if(combo_vehicle.Items.Contains(selectedData.Vehicle)) {
                combo_vehicle.SelectedItem = selectedData.Vehicle;
            } else {
                MessageBox.Show(string.Format("线路'{0}'使用了不支持的车型'{1}'", selectedData.Name, selectedData.Vehicle), "提示");
            }
            label_name.Content = selectedData.Name;
            floatLabel.Content = selectedData.Name;
        }

        void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Label b = sender as Label;
            string[] sp = b.Name.Split('_');

            byte tier = byte.Parse(sp[1]);
            byte rank = byte.Parse(sp[2]);

            //Debug.WriteLine("点击了表格 tier:"+tier+" rank:"+rank);
            combo_vehicle.IsEnabled=true;  //点击表格区，启用车型

            selectedData = routeDataTable[tier, rank];
            SetSelectedData();
            if(selectedData==null) return;


            //启用拖动
            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            floatLabel.Visibility = Visibility.Visible;
            Point mousePos = Mouse.GetPosition(canvas);
            floatLabel.CaptureMouse();
            floatLabel.SetValue(Canvas.LeftProperty, mousePos.X-(floatLabel.ActualWidth/2));
            floatLabel.SetValue(Canvas.TopProperty, mousePos.Y-(floatLabel.ActualHeight/2));
            floatLabel.Cursor = Cursors.Hand;
        }

        private class RouteData {
            public string FullName { get; }
            public string Name { get; }
            public byte Tier { get; set; }
            public byte Rank { get; set; }
            public string Vehicle { get; set; }
            public byte TierP { get; private set; }
            public byte RankP { get; private set; }
            public string VehicleP { get; private set; }

            public RouteData(string fna, string na, byte tier, byte rank, string veh) {
                FullName = fna;
                Name = na;
                Tier = tier;
                Rank = rank;
                Vehicle = veh;
                TierP = tier;
                RankP = rank;
                VehicleP = veh;
            }

            public string GetLabel() {
                return Name +"\n"+ Vehicle;
                //return string.Format("{0}\n{1}\n({2},{3})", Name, Vehicle, Tier, Rank);
            }

            public void ResetP() {
                TierP = Tier;
                RankP = Rank;
                VehicleP = Vehicle;
            }

            public override string ToString() {
                return string.Format("{0},{1},({2},{3})", Name, Vehicle, Tier, Rank);
            }
        }

        private void routeList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(listMouseIn) {
                if(routeList.SelectedItem == null) return;
                else selectedData = (RouteData)routeList.SelectedItem;


                combo_vehicle.IsEnabled=false;    //点击候选区，禁用车型
                SetSelectedData();

                //将label放在鼠标下
                floatLabel.Visibility = Visibility.Visible;

                isDragDropInEffect = true;
                Point mousePos = Mouse.GetPosition(canvas);
                floatLabel.CaptureMouse();
                floatLabel.SetValue(Canvas.LeftProperty, mousePos.X-(floatLabel.ActualWidth/2));
                floatLabel.SetValue(Canvas.TopProperty, mousePos.Y-(floatLabel.ActualHeight/2));
                floatLabel.Cursor = Cursors.Hand;
            }
        }

        private void vehicleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(selectedData != null) {
                selectedData.Vehicle = (string)combo_vehicle.SelectedItem;
                RefreshLabel(selectedData.Tier, selectedData.Rank);
                label_info.Content = "修改未保存";
            }
        }

        private void RefreshLabel(byte tier, byte rank) {
            if(tier>=MAX_TIER || rank>=MAX_RANK) {
                routeList.Items.Refresh();
            } else {
                routeLabelTable[tier, rank].Content = routeDataTable[tier, rank].GetLabel();
            }
        }

        bool isDragDropInEffect = false;
        Point pos = new Point();

        private void floatLabel_MouseMove(object sender, MouseEventArgs e) {
            if(isDragDropInEffect) {
                double xPos = e.GetPosition(null).X - pos.X + (double)floatLabel.GetValue(Canvas.LeftProperty);
                double yPos = e.GetPosition(null).Y - pos.Y + (double)floatLabel.GetValue(Canvas.TopProperty);
                floatLabel.SetValue(Canvas.LeftProperty, xPos);
                floatLabel.SetValue(Canvas.TopProperty, yPos);
                pos = e.GetPosition(null);
            }
        }

        private void floatLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if(isDragDropInEffect) {
                isDragDropInEffect = false;
                floatLabel.Visibility = Visibility.Hidden;
                floatLabel.ReleaseMouseCapture();

                Point pO = routeGrid.TranslatePoint(new Point(0, 0), canvas);


                if(pos.X<pO.X || pos.Y<pO.Y || pos.X > pO.X+routeGrid.ActualWidth || pos.Y > pO.Y+routeGrid.ActualHeight) {
                    //落在外围
                    //Debug.WriteLine("在表格外");
                    //重置位置
                    if(selectedData == routeList.SelectedItem) {
                        //候选区，又拖回候选区
                        return;
                    } else {
                        //表格区，拖入候选区
                        routeList.Items.Add(routeDataTable[selectedData.Tier, selectedData.Rank]);
                        routeLabelTable[selectedData.Tier, selectedData.Rank].Content = string.Empty;
                        routeDataTable[selectedData.Tier, selectedData.Rank] = null;

                        selectedData.Tier = 10;
                        selectedData.Rank = 10;
                    }


                } else {
                    //拖入的位置
                    byte tier = (byte)((int)(pos.X-pO.X) / (int)(routeGrid.ActualWidth/6));
                    byte rank = (byte)((int)(pos.Y-pO.Y) / (int)(routeGrid.ActualHeight/6));

                    if(selectedData != null) {
                        if(selectedData == routeList.SelectedItem) {
                            selectedData.Tier = tier;
                            selectedData.Rank = rank;
                            //候选区，拖到表格区
                            if(routeDataTable[tier, rank] == null) {
                                //表格处为空
                                routeDataTable[tier, rank] = selectedData;
                                routeLabelTable[tier, rank].Content = selectedData.GetLabel();
                                routeList.Items.Remove(selectedData);
                            } else {
                                //将表格原有数据替换到候选区
                                routeDataTable[tier, rank].Tier = 10;
                                routeDataTable[tier, rank].Rank = 10;
                                routeList.Items.Add(routeDataTable[tier, rank]);

                                routeDataTable[tier, rank] = selectedData;
                                routeLabelTable[tier, rank].Content = selectedData.GetLabel();
                                routeList.Items.Remove(selectedData);
                            }
                        } else {
                            //交换到表格区
                            byte fromTier = selectedData.Tier;
                            byte fromRank = selectedData.Rank;

                            if(routeDataTable[tier, rank] == null) {
                                //交换到的为空

                                routeDataTable[fromTier, fromRank] = null;
                                routeDataTable[tier, rank] = selectedData;
                                routeDataTable[tier, rank].Tier = tier;
                                routeDataTable[tier, rank].Rank = rank;

                                routeLabelTable[fromTier, fromRank].Content = string.Empty;
                                RefreshLabel(tier, rank);

                            } else {

                                routeDataTable[fromTier, fromRank] = routeDataTable[tier, rank];
                                routeDataTable[fromTier, fromRank].Tier = fromTier;
                                routeDataTable[fromTier, fromRank].Rank = fromRank;
                                routeDataTable[tier, rank] = selectedData;
                                routeDataTable[tier, rank].Tier = tier;
                                routeDataTable[tier, rank].Rank = rank;

                                RefreshLabel(fromTier, fromRank);
                                RefreshLabel(tier, rank);
                            }
                        }
                    }
                }
                label_info.Content = "修改未保存";
                floatLabel.Content=string.Empty;
                //selectedData=null;
                //Debug.WriteLine(string.Format("在({0},{1})", tier, rank));
            }
        }

        bool listMouseIn = false;
        private void routeList_MouseEnter(object sender, MouseEventArgs e) {
            listMouseIn = true;
        }

        private void routeList_MouseLeave(object sender, MouseEventArgs e) {
            listMouseIn = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            releaseBinary();
            randomGuid = Guid.NewGuid().ToString("N");
        }

        private void releaseBinary() {
            if(File.Exists("./7za.exe")) return;

            FileStream fs = new FileStream("./7za.exe", FileMode.OpenOrCreate);
            BinaryReader br = new BinaryReader(new MemoryStream(Properties.Resources._7za));
            int len = (int)br.BaseStream.Length;
            fs.Write(br.ReadBytes(len),0,len);
            br.Close();
            fs.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            deleteTemp();
        }

        private void btn_start_Click(object sender, RoutedEventArgs e) {
            if(File.Exists("./bin/win_x86/busdriver.exe")) {
                Process.Start(Environment.CurrentDirectory + @"\bin\win_x86\busdriver.exe");
            } else if(File.Exists("./busdriver.exe")) {
                Process.Start(Environment.CurrentDirectory + @"\busdriver.exe");
            } else if(File.Exists("./bin/win_x86/开始游戏.exe")) {
                Process.Start(Environment.CurrentDirectory + @"\bin\win_x86\开始游戏.exe");
            } else if(File.Exists("./开始游戏.exe")) {
                Process.Start(Environment.CurrentDirectory + @"\开始游戏.exe");
            } 
            Close();
        }
    }
}
