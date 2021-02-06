using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Path = System.IO.Path;
using System.Text.RegularExpressions;

namespace route_table {
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

        private void btn_load_Click(object sender, RoutedEventArgs e) {
            if(!File.Exists("base.scs")) {
                MessageBox.Show("找不到base.scs");
                return;
            }
            dataList.Clear();
            routeDataTable = new RouteData[MAX_TIER, MAX_RANK];
            selectedData = null;
            routeList.Items.Clear();

            using(ZipArchive zip = ZipFile.Open("base.scs", ZipArchiveMode.Read)) {
                List<string> routesPath = new List<string>();

                foreach(ZipArchiveEntry entry in zip.Entries) {
                    if(entry.FullName[0].Equals('/')) {
                        if(entry.FullName.StartsWith("/def/route/", StringComparison.OrdinalIgnoreCase)) {
                            if(entry.FullName.EndsWith(".sii", StringComparison.OrdinalIgnoreCase)) {
                                routesPath.Add(entry.FullName);
                            }
                        }
                    } else {
                        if(entry.FullName.StartsWith("def/route/", StringComparison.OrdinalIgnoreCase)) {
                            if(entry.FullName.EndsWith(".sii", StringComparison.OrdinalIgnoreCase)) {
                                routesPath.Add(entry.FullName);
                            }
                        }
                    }
                }

                Match match;
                string all;
                foreach(string s in routesPath) {
                    using(StreamReader sr = new StreamReader(zip.GetEntry(s).Open())) {
                        all = sr.ReadToEnd();
                    }
                    //正则匹配线路
                    match = Regex.Match(all, @"^[\s\S]+mission\s*:\s*mission.(\S+)\s*{[\s\S]+tier:\s*(\d+)[\s\S]+rank:\s*(\d+)[\s\S]+vehicle_data:\s*vehicle.(\S+)[\s\S]+$");
                    if(!match.Success) {
                        //Debug.WriteLine("Match failed:"+s);
                        MessageBox.Show("线路'"+s+"'可能存在问题");
                        continue;
                    }
                    if(!byte.TryParse(match.Groups[2].Value, out byte tier)) {
                        tier = 10;
                    }
                    if(!byte.TryParse(match.Groups[3].Value, out byte rank)) {
                        rank = 10;
                    }

                    RouteData data = new RouteData(s, match.Groups[1].Value, tier, rank, match.Groups[4].Value);
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
            }
            btn_save.IsEnabled = true;
        }
        private void btn_save_Click(object sender, RoutedEventArgs e) {
            //string dirName = DateTime.Now.ToString("yyyyMMddHHmmss");
            //Directory.CreateDirectory(dirName);

            using(ZipArchive archive = ZipFile.Open("base.scs", ZipArchiveMode.Update,Encoding.GetEncoding(936))) {
                foreach(RouteData data in dataList) {
                    //跳过未修改的线路
                    if(data.Tier == data.TierP && data.Rank == data.RankP) continue;

                    string routeContext;
                    ZipArchiveEntry entry = archive.GetEntry(data.FullName);
                    using(StreamReader sr = new StreamReader(entry.Open())) {
                        routeContext = sr.ReadToEnd();
                    }
                    routeContext = Regex.Replace(routeContext,
                        @"^([\s\S]+tier:\s*)\d+([\s\S]+rank:\s*)\d+([\s\S]+)$",
                        "${1}"+data.Tier+"${2}"+data.Rank+"${3}");

                    /*using(StreamWriter sw = new StreamWriter(dirName+data.FullName.Substring(9),false,Encoding.GetEncoding(936))) {
                        sw.Write(routeContext);
                    }*/

                    using(StreamWriter writer = new StreamWriter(entry.Open())) {
                        writer.BaseStream.Seek(0, SeekOrigin.Begin);
                        writer.BaseStream.SetLength(0);
                        writer.Write(routeContext);
                    }
                    entry.LastWriteTime = DateTimeOffset.UtcNow.LocalDateTime;
                }
            }
            MessageBox.Show("已保存到base.scs");
            
            //Debug.WriteLine("Done");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {

        }

        void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Label b = sender as Label;
            string[] sp = b.Name.Split('_');

            byte tier = byte.Parse(sp[1]);
            byte rank = byte.Parse(sp[2]);

            //Debug.WriteLine("点击了表格 tier:"+tier+" rank:"+rank);

            selectedData = routeDataTable[tier, rank];
            if(selectedData==null) return;

            //启用拖动
            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            floatLabel.Visibility = Visibility.Visible;
            Point mousePos = Mouse.GetPosition(canvas);
            floatLabel.Content = selectedData.Name;
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
            public byte TierP { get; }
            public byte RankP { get; }

            public RouteData(string fna,string na,byte tier,byte rank,string veh) {
                FullName = fna;
                Name = na;
                Tier = tier;
                Rank = rank;
                Vehicle = veh;
                TierP = tier;
                RankP = rank;
            }

            public string GetLabel() {
                return Name +"\n"+ Vehicle;
                //return string.Format("{0}\n{1}\n({2},{3})", Name, Vehicle, Tier, Rank);
            }

            public override string ToString() {
                return string.Format("{0},{1},({2},{3})", Name, Vehicle, Tier,Rank); 
            }
        }

        private void routeList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            selectedData = (RouteData)routeList.SelectedItem;
            if(selectedData == null) return;
            floatLabel.Content = selectedData.Name;

            if(listMouseIn) {
                //将label放在鼠标下
                floatLabel.Visibility = Visibility.Visible;

                isDragDropInEffect = true;
                Point mousePos = Mouse.GetPosition(canvas);
                floatLabel.Content = selectedData.Name;
                floatLabel.CaptureMouse();
                floatLabel.SetValue(Canvas.LeftProperty, mousePos.X-(floatLabel.ActualWidth/2));
                floatLabel.SetValue(Canvas.TopProperty, mousePos.Y-(floatLabel.ActualHeight/2));
                floatLabel.Cursor = Cursors.Hand;
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

        private void floatLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if(selectedData==null) return;
            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            floatLabel.CaptureMouse();
            floatLabel.Cursor = Cursors.Hand;
        }

        private void floatLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if(isDragDropInEffect) {
                isDragDropInEffect = false;
                floatLabel.Visibility = Visibility.Hidden;
                floatLabel.ReleaseMouseCapture();

                Point pO = routeGrid.TranslatePoint(new Point(0, 0), canvas);

                floatLabel.SetValue(Canvas.LeftProperty, 103.0);
                floatLabel.SetValue(Canvas.TopProperty, 12.0);

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
                                routeLabelTable[tier, rank].Content = routeDataTable[tier, rank].GetLabel();

                            } else {

                                routeDataTable[fromTier, fromRank] = routeDataTable[tier, rank];
                                routeDataTable[fromTier, fromRank].Tier = fromTier;
                                routeDataTable[fromTier, fromRank].Rank = fromRank;
                                routeDataTable[tier, rank] = selectedData;
                                routeDataTable[tier, rank].Tier = tier;
                                routeDataTable[tier, rank].Rank = rank;

                                routeLabelTable[fromTier, fromRank].Content = routeDataTable[fromTier, fromRank].GetLabel();
                                routeLabelTable[tier, rank].Content = routeDataTable[tier, rank].GetLabel();
                            }
                        }
                    }
                }
                floatLabel.Content=string.Empty;
                selectedData=null;
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
    }
}
