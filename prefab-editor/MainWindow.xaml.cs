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
using BusDriverFile;

namespace prefab_editor {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow:Window {
        public MainWindow() {
            InitializeComponent();
        }

        //PddFile[] pdds;
        Dictionary<string, PddFile> pdds = new Dictionary<string, PddFile>();

        private void btn_load_Click(object sender, RoutedEventArgs e) {
            using(ZipArchive zip = ZipFile.Open("base.scs", ZipArchiveMode.Read)) {
                List<string> routesPath = new List<string>();
                List<string> vehiclePath = new List<string>();

                ZipArchiveEntry defEntry = zip.GetEntry("def/world/prefab.def");
                if(defEntry == null) {
                    defEntry = zip.GetEntry("/def/world/prefab.def");
                    if(defEntry == null) {
                        MessageBox.Show("找不到base.scs/def/world/prefab.def");
                        return;
                    }
                }

                DefReader prefabReader = new DefReader(defEntry.Open(),Encoding.GetEncoding(936));
                prefabReader.keys.TryGetValue("prefab_count", out string temp);
                uint prefabCount = Convert.ToUInt32(temp);

                //pdds=new PddFile[prefabCount];
                for(uint i = 0; i<prefabCount; i++) {
                    if(prefabReader.keys.TryGetValue("prefab"+i, out temp)) {
                        if(temp.Equals("\"\"")) continue;
                        string path2 = temp.Substring(2, temp.Length-3).Replace(".pmd", ".pdd");

                        ZipArchiveEntry pddEntry = zip.GetEntry(path2);
                        if(pddEntry == null) {
                            pddEntry = zip.GetEntry("/" + path2);
                            if(pddEntry == null) {
                                MessageBox.Show("找不到base.scs"+path2);
                                return;
                            }
                        }

                        //pdds[i]=new PddFile(zip.GetEntry(path2).Open());
                        //list_prefab.Items.Add(path2.Replace("prefab/", ""));

                        pdds.Add(path2, new PddFile(zip.GetEntry(path2).Open()));
                        list_prefab.Items.Add(path2);
                    }
                }
            }
        }

        private void btn_open_Click(object sender, RoutedEventArgs e) {
            canvas.Children.Clear();
            pdds.TryGetValue((string)list_prefab.SelectedItem, out PddFile pdd);
            //MessageBox.Show(pdd.ToString());

            Debug.WriteLine("node个数：" + pdd.node.Length);

            StreamGeometry nodeGeo = new StreamGeometry();
            nodeGeo.FillRule = FillRule.EvenOdd;
            using(StreamGeometryContext ctx = nodeGeo.Open()) {
                for(int i = 0; i<pdd.node.Length; i++) {
                    PddFile.PNode node = pdd.node[i];
                    double x = (node.pos.x + 35)*8;
                    double y = (node.pos.z + 35)*8;

                    ctx.BeginFigure(new Point(x, y), false, false);
                    ctx.LineTo(new Point(x + node.dir.x*10, y + node.dir.z*10), true, false);

                    ctx.BeginFigure(new Point(x-3, y-3), true, true);
                    ctx.LineTo(new Point(x+3,y-3), true, false);
                    ctx.LineTo(new Point(x+3,y+3), true, false);
                    ctx.LineTo(new Point(x-3,y+3), true, false);

                }
            }
            nodeGeo.Freeze();
            canvas.Children.Add(new System.Windows.Shapes.Path {
                Stroke = Brushes.Green,
                StrokeThickness = 1,
                Fill = Brushes.Red,
                Data = nodeGeo
            });



            StreamGeometry lightGeo = new StreamGeometry();
            lightGeo.FillRule = FillRule.EvenOdd;
            using(StreamGeometryContext ctx = lightGeo.Open()) {
                for(int i = 0; i<pdd.light.Length; i++) {
                    PddFile.Light light = pdd.light[i];
                    double x = (light.pos.x + 35)*8;
                    double y = (light.pos.z + 35)*8;

                    ctx.BeginFigure(new Point(x, y), false, false);
                    ctx.LineTo(new Point(x + light.dir.x*10, y + light.dir.z*10), true, false);

                    ctx.BeginFigure(new Point(x-3, y-3), true, true);
                    ctx.LineTo(new Point(x+3, y-3), true, false);
                    ctx.LineTo(new Point(x+3, y+3), true, false);
                    ctx.LineTo(new Point(x-3, y+3), true, false);
                    //MessageBox.Show(light.dir.ToString());
                }
            }
            lightGeo.Freeze();
            canvas.Children.Add(new System.Windows.Shapes.Path {
                Stroke = Brushes.Green,
                StrokeThickness = 1,
                Fill = Brushes.Pink,
                Data = lightGeo
            });


            Debug.WriteLine("curve个数：" + pdd.curve.Length);
            for(int i=0;i < pdd.curve.Length;i++) {
                PddFile.Curve curve = pdd.curve[i];
                //MessageBox.Show(string.Format("({0},{1})to({2},{3})",x1,y1,x2,y2));


                double x1 = (curve.startPos.x + 35)*8;
                double y1 = (curve.startPos.z + 35)*8;
                double x2 = (curve.endPos.x + 35)*8;
                double y2 = (curve.endPos.z + 35)*8;
                double xDir1 = curve.startDir.x*30;
                double yDir1 = curve.startDir.z*30;
                double xDir2 = curve.endDir.x*30;
                double yDir2 = curve.endDir.z*30;

                StreamGeometry geometry = new StreamGeometry();
                using(StreamGeometryContext ctx = geometry.Open()) {
                    ctx.BeginFigure(new Point(x1, y1) , false, false);
                    ctx.BezierTo(new Point(x1+xDir1, y1+yDir1), new Point(x2-xDir2, y2-yDir2), new Point(x2, y2), true, false);
                }
                geometry.Freeze();
                canvas.Children.Add(new System.Windows.Shapes.Path {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Data = geometry
                });


                //MessageBox.Show("");

                /*StreamGeometry dirGeometry1 = new StreamGeometry();
                using(StreamGeometryContext ctx = dirGeometry1.Open()) {
                    ctx.BeginFigure(new Point(x1, y1) , false, false);
                    ctx.LineTo(new Point(x1+xDir1, y1+yDir1), true, false );
                }
                dirGeometry1.Freeze();
                canvas.Children.Add(new System.Windows.Shapes.Path {
                    Stroke = Brushes.Yellow,
                    StrokeThickness = 2,
                    Data = dirGeometry1
                });


                StreamGeometry dirGeometry2 = new StreamGeometry();
                using(StreamGeometryContext ctx = dirGeometry2.Open()) {
                    ctx.BeginFigure(new Point(x2, y2), false, false);
                    ctx.LineTo(new Point(x2+xDir2, y2+yDir2), true, false);
                }
                dirGeometry2.Freeze();
                canvas.Children.Add(new System.Windows.Shapes.Path {
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2,
                    Data = dirGeometry2
                });*/

            }
        }

        private void aa() {
            StreamGeometry geometry = new StreamGeometry();
            geometry.FillRule = FillRule.EvenOdd;

            using(StreamGeometryContext ctx = geometry.Open()) {
                ctx.BeginFigure(new Point(10, 100), true /* is filled */, true /* is closed */);
                ctx.LineTo(new Point(100, 100), true /* is stroked */, false /* is smooth join */);
                ctx.LineTo(new Point(100, 50), true, false);

            }
            geometry.Freeze();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            StreamGeometry geometry = new StreamGeometry();
            using(StreamGeometryContext ctx = geometry.Open()) {
                ctx.BeginFigure(new Point(10, 210), false, false);
                ctx.BezierTo(new Point(100, 100), new Point(200, 100), new Point(200, 200), true, false);
            }
            geometry.Freeze();
            System.Windows.Shapes.Path myPath = new System.Windows.Shapes.Path {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Data = geometry
            };
            canvas.Children.Add(myPath);
        }


        private BezierSegment GetBezierSegment(Point currentPt, Point lastPt, Point nextPt1, Point nextPt2) {
            //计算中点
            var lastC = GetCenterPoint(lastPt, currentPt);
            var nextC1 = GetCenterPoint(currentPt, nextPt1); //贝塞尔控制点
            var nextC2 = GetCenterPoint(nextPt1, nextPt2);
            //计算“相邻中点”的中点
            var c1 = GetCenterPoint(lastC, nextC1);
            var c2 = GetCenterPoint(nextC1, nextC2);
            //计算【"中点"的中点】需要的点位移
            var controlPtOffset1 = currentPt - c1;
            var controlPtOffset2 = nextPt1 - c2;
            //移动控制点
            var controlPt1 = nextC1 + controlPtOffset1;
            var controlPt2 = nextC1 + controlPtOffset2;
            //如果觉得曲线幅度太大，可以将控制点向当前点靠近一定的系数。
            controlPt1 = controlPt1 + 0 * (currentPt - controlPt1);
            controlPt2 = controlPt2 + 0 * (nextPt1 - controlPt2);
            var bzs = new BezierSegment(controlPt1, controlPt2, nextPt1, true);
            return bzs;
        }

        private Point GetCenterPoint(Point p1, Point p2) {
            return new Point((p1.X+p2.X)/2, (p1.Y+p2.Y)/2);
        }
    }
}
