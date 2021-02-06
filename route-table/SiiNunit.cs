using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BusDriverFile {
    public class SiiNunit {
        public Dictionary<string,Unit> unit;
        public Dictionary<string,Unit> root;
        public Unit baseUnit;

        public SiiNunit(string path) {
            Load(new FileStream(path,FileMode.Open));
        }
        public SiiNunit(Stream stream) {
            Load(stream);
        }

        private void Load(Stream stream) {
            unit=new Dictionary<string, Unit>();
            StreamReader sr = new StreamReader(stream);
            string temp = sr.ReadLine();
            if(temp.Contains("SiiNunit")) {
                if(!temp.Contains("{")) {
                    temp=sr.ReadLine();
                    if(!temp.Contains("{")) {
                        throw new Exception("不是有效的SiiNunit");
                    }
                }
            } else {
                throw new Exception("不是有效的SiiNunit");
            }

            bool exit = false;
            while(!sr.EndOfStream) {
                int t;
                StringBuilder sbName = new StringBuilder();

                while((t=sr.Read())!='{') {
                    if(sr.EndOfStream) { exit=true; break; }
                    sbName.Append((char)t);
                }
                if(exit) break;

                string cName = sbName.ToString().Replace("\n", "").Replace("\r", "");

                string detail = null;
                if(t == '{') {
                    //开始unit
                    StringBuilder sb = new StringBuilder();
                    while((t=sr.Read())!='}') {
                        if(sr.EndOfStream) { exit=true; break; }
                        sb.Append((char)t);
                    }
                    if(exit) break;
                    detail=sb.ToString();
                }

                int ind = cName.LastIndexOf(':');
                string className = cName.Substring(0, ind).Trim();
                //Dictionary<string, string> dic = GetKeyValues(detail);
                //System.Diagnostics.Debug.WriteLine(className);
                Unit a;
                switch(className) {
                    case "ui::window": a = new UI_Window(); break;
                    case "ui::group": a = new UI_Group(); break;
                    case "ui::text_common": a = new UI_TextCommon(); break;
                    case "ui::text": a = new UI_Text(); break;
                    case "ui::frame_common": a = new UI_FrameCommon(); break;
                    case "ui::button": a = new UI_Button(); break;
                    case "ui::button_common": a = new UI_ButtonCommon(); break;
                    case "ui_blinking_button": a = new Ui_BlinkingButton(); break;
                    case "main_menu": a = new MainMenu(); break;
                    case "loading": a = new Loading(); break;
                    case "intro_menu": a = new IntroMenu(); break;
                    case "bug_report_hdl": a = new BugReport(); break;
                    case "options_menu": a = new OptionsMenu(); break;
                    case "shared_menu": a = new SharedMenu(); break;
                    case "mission_selection": a = new MissionSelection(); break;
                    case "mission": a = new Mission(); break;
                    case "high_score_data_table": a=new HighScoreDataTable(); break;
                    case "high_score_data": a=new HighScoreData(); break;
                    default: a= new CommonUnit(); break;
                };
                a.className=className;
                a.unitName=cName.Substring(ind+1, cName.Length-ind-1).Trim();
                a.item=GetKeyValues(detail);
                unit.Add(a.unitName, a);
            }

            sr.Close();
            try {
                LinkNodes();
            } catch(Exception e) { throw e; }
            if(root.Count==1) {
                foreach(KeyValuePair<string, Unit> kvp in root) {
                    baseUnit = kvp.Value;
                }
            }
        }

        private void LinkNodes() {
            foreach(KeyValuePair<string,Unit> kvp in unit) {
                try {
                    kvp.Value.Init(unit);
                } catch(Exception e) {
                    throw e;
                }
            }

            root=new Dictionary<string,Unit>();
            foreach(KeyValuePair<string,Unit> kvp in unit) {
                if(!kvp.Value.item.ContainsKey("my_parent")) {
                    root.Add(kvp.Key,kvp.Value);
                } else {
                    kvp.Value.item.TryGetValue("my_parent",out string na);
                    if(na.Equals("null")) root.Add(kvp.Key,kvp.Value);
                }
            }
        }

        private Dictionary<string,string> GetKeyValues(string total) {
            if(string.IsNullOrEmpty(total)) return null;
            string[] ts = total.Split('\n');
            string temp;
            Dictionary<string,string> keys = new Dictionary<string,string>();
            for(int i = 0;i<ts.Length;i++) {
                temp=ts[i];
                if(temp.Contains("#")) {
                    int index = temp.IndexOf('#');
                    if(index==0) continue;
                    temp=temp.Substring(0,index);
                    if(temp.Length<1) continue; //去注释
                }

                if(temp.Contains(":")) {
                    //属性行
                    int index = temp.IndexOf(':');
                    string keyName = temp.Substring(0,index).Trim();
                    string value = temp.Substring(index+1,temp.Length-index-1).Trim();
                    try {
                        if("icons[]".Equals(keyName)) {
                            keyName+="_" + Guid.NewGuid().GetHashCode();
                            Debug.WriteLine("icons[]重复,改为"+keyName);
                        }
                        keys.Add(keyName,value);
                    }catch(ArgumentException) { 
                        Debug.WriteLine(keyName+"重复"); 
                    }
                }
            }
            return keys;
        }

        public enum UnitType { ui_window, ui_group };


        public abstract class Unit {
            public string className;
            public string unitName;
            public Dictionary<string,string> item;

            public override string ToString() {
                return className+":"+unitName;
            }
            public abstract void Init(Dictionary<string,Unit> unit);
        }
        public class CommonUnit:Unit {
            public override void Init(Dictionary<string,Unit> unit) {
                //System.Diagnostics.Debug.WriteLine("未知类");
                throw new Exception("Unknown class");
            }
        }

        public abstract class UI:Unit {
            public int coords_b;
            public int coords_l;
            public int coords_r;
            public int coords_t;
            public override void Init(Dictionary<string,Unit> unit) {
                string temp;
                if(item.TryGetValue("coords_b",out temp)) coords_b=Util.HexToFloat(temp);
                if(item.TryGetValue("coords_l",out temp)) coords_l=Util.HexToFloat(temp);
                if(item.TryGetValue("coords_r",out temp)) coords_r=Util.HexToFloat(temp);
                if(item.TryGetValue("coords_t",out temp)) coords_t=Util.HexToFloat(temp);

            }


            protected Unit[] GetMyChildren(Dictionary<string,Unit> unit) {
                List<Unit> myChildren = new List<Unit>();
                int i = 0;
                while(item.TryGetValue("my_children["+i+"]",out string temp)) {
                    if(unit.TryGetValue(temp,out Unit aa)) {
                        myChildren.Add(aa);
                    } else {
                        //System.Diagnostics.Debug.WriteLine("缺失unit");
                        throw new Exception("Missing unit");
                    }
                    i++;
                }
                return myChildren.ToArray();
            }
        }


        public class UI_Window:UI {
            public new static readonly string className = "ui::window";
            public Unit[] children;
            public string name;
            public Unit window_handler;

            public override void Init(Dictionary<string,Unit> unit) {
                base.Init(unit);
                string temp;
                children=GetMyChildren(unit);
                if(item.TryGetValue("name",out temp)) name=temp;
                if(item.TryGetValue("window_handler",out temp)) {
                    unit.TryGetValue(temp,out window_handler);
                }
            }
        }

        public class UI_Group:UI {
            public new static readonly string className = "ui::group";
            public bool fitting;
            public int id;
            public int layer;
            public Unit[] children;
            public Unit parent;
            public string name;

            public override void Init(Dictionary<string,Unit> unit) {
                base.Init(unit);
                string temp;
                if(item.TryGetValue("fitting",out temp)) fitting=bool.Parse(temp);
                if(item.TryGetValue("id",out temp)) id=int.Parse(temp);
                if(item.TryGetValue("layer",out temp)) layer=int.Parse(temp);
                children=GetMyChildren(unit);
                if(item.TryGetValue("my_parent",out temp)) {
                    unit.TryGetValue(temp,out parent);
                }
                if(item.TryGetValue("name",out temp)) name=temp;
            }
        }

        public class UI_TextCommon:UI {
            public new static readonly string className = "ui::text_common";
            public int id;
            public int layer;
            public string look_template;
            public Unit parent;
            public string value;

            public override void Init(Dictionary<string,Unit> unit) {
                base.Init(unit);
                string temp;
                if(item.TryGetValue("id",out temp)) id=int.Parse(temp);
                if(item.TryGetValue("layer",out temp)) layer=int.Parse(temp);
                if(item.TryGetValue("look_template",out temp)) look_template=temp;

                if(item.TryGetValue("my_parent",out temp)) {
                    unit.TryGetValue(temp,out parent);
                }
                if(item.TryGetValue("value",out temp)) value=temp;
            }
        }

        public class UI_Text:UI {
            public new static readonly string className = "ui::text";
            public int layer;
            public Unit parent;
            public string name;
            public string text;

            public override void Init(Dictionary<string,Unit> unit) {
                base.Init(unit);
                string temp;
                if(item.TryGetValue("layer",out temp)) layer=int.Parse(temp);

                if(item.TryGetValue("my_parent",out temp)) {
                    unit.TryGetValue(temp,out parent);
                }
                if(item.TryGetValue("name",out temp)) name=temp;
                if(item.TryGetValue("text",out temp)) text=temp;
            }
        }

        public class UI_FrameCommon:UI {
            public new static readonly string className = "ui::frame_common";
            public int layer;
            public string lookTemplate;
            public Unit parent;

            public override void Init(Dictionary<string,Unit> unit) {
                base.Init(unit);
                string temp;
                if(item.TryGetValue("layer",out temp)) layer=int.Parse(temp);
                if(item.TryGetValue("look_template",out temp)) lookTemplate=temp;

                if(item.TryGetValue("my_parent",out temp)) {
                    unit.TryGetValue(temp,out parent);
                }
            }
        }

        public class UI_Button:UI {
            public new static readonly string className = "ui::button";
            public int id;
            public int layer;
            public Unit parent;
            public string name;

            public override void Init(Dictionary<string,Unit> unit) {
                base.Init(unit);
                string temp;
                if(item.TryGetValue("id",out temp)) id=int.Parse(temp);
                if(item.TryGetValue("layer",out temp)) layer=int.Parse(temp);

                if(item.TryGetValue("my_parent",out temp)) {
                    unit.TryGetValue(temp,out parent);
                }
                if(item.TryGetValue("name",out temp)) name=temp;
            }
        }
        public class UI_ButtonCommon:UI {
            public new static readonly string className = "ui::button_common";
            public int id;
            public int layer;
            public Unit parent;
            public string name;

            public override void Init(Dictionary<string,Unit> unit) {
                base.Init(unit);
                string temp;
                if(item.TryGetValue("id",out temp)) id=int.Parse(temp);
                if(item.TryGetValue("layer",out temp)) layer=int.Parse(temp);

                if(item.TryGetValue("my_parent",out temp)) {
                    unit.TryGetValue(temp,out parent);
                }
                if(item.TryGetValue("name",out temp)) name=temp;
            }
        }

        public class Ui_BlinkingButton:UI_Button {
            public new static readonly string className = "ui_blinking_button";
            public string value;
            public string lookTemplate;

            public override void Init(Dictionary<string,Unit> unit) {
                base.Init(unit);
                string temp;
                if(item.TryGetValue("value",out temp)) value=temp;
                if(item.TryGetValue("look_template",out temp)) lookTemplate=temp;
            }
        }

        public class MainMenu:Unit {
            public new static readonly string className = "main_menu";
            public Unit windowLink;

            public override void Init(Dictionary<string,Unit> unit) {
                if(item.TryGetValue("window_link",out string temp)) {
                    unit.TryGetValue(temp,out windowLink);
                }
            }
        }
        public class Loading:Unit {
            public new static readonly string className = "loading";
            public Unit windowLink;

            public override void Init(Dictionary<string,Unit> unit) {
                if(item.TryGetValue("window_link",out string temp)) {
                    unit.TryGetValue(temp,out windowLink);
                }
            }
        }
        public class IntroMenu:Unit {
            public new static readonly string className = "intro_menu";
            public Unit windowLink;

            public override void Init(Dictionary<string,Unit> unit) {
                if(item.TryGetValue("window_link",out string temp)) {
                    unit.TryGetValue(temp,out windowLink);
                }
            }
        }
        public class BugReport:Unit {
            public new static readonly string className = "bug_report_hdl";
            public Unit windowLink;

            public override void Init(Dictionary<string,Unit> unit) {
                if(item.TryGetValue("window_link",out string temp)) {
                    unit.TryGetValue(temp,out windowLink);
                }
            }
        }
        public class OptionsMenu:Unit {
            public new static readonly string className = "options_menu";
            public Unit windowLink;

            public override void Init(Dictionary<string,Unit> unit) {
                if(item.TryGetValue("window_link",out string temp)) {
                    unit.TryGetValue(temp,out windowLink);
                }
            }
        }
        public class SharedMenu:Unit {
            public new static readonly string className = "shared_menu";
            public Unit windowLink;

            public override void Init(Dictionary<string,Unit> unit) {
                if(item.TryGetValue("window_link",out string temp)) {
                    unit.TryGetValue(temp,out windowLink);
                }
            }
        }
        public class MissionSelection:Unit {
            public new static readonly string className = "mission_selection";
            public Unit windowLink;

            public override void Init(Dictionary<string,Unit> unit) {
                if(item.TryGetValue("window_link",out string temp)) {
                    unit.TryGetValue(temp,out windowLink);
                }
            }
        }

        public class HighScoreDataTable:Unit {
            public new static readonly string className = "high_score_data_table";

            public override void Init(Dictionary<string,Unit> unit) {

            }
        }

        public class HighScoreData:Unit {
            public new static readonly string className = "high_score_data";

            public override void Init(Dictionary<string,Unit> unit) {

            }
        }

        public class Mission:Unit {
            public new static readonly string className = "mission";
            public string controller_type; //{bus_lane_mission_ctrl,prison_mission_ctrl}
            public string short_desc;
            public string long_desc;
            public string map_name;
            public int time_limit;
            public int bonus_time_limit;
            public sbyte sun_idx;
            public sbyte[] sun_idxs;
            public sbyte sky_idx;
            public sbyte[] sky_idxs;
            public byte tier;
            public byte rank;
            public int overlay_offset_x;
            public int overlay_offset_y;
            public string vehicle_data;
            public string bus_number;
            public string time_table_name;
            public int bus_stop_camera;
            public string[] bus_stop_cameras;
            public int entering_passenger;
            public int[] entering_passengers;
            public int max_leaving_passenger;
            public int[] max_leaving_passengers;
            public int driving_time;
            public int[] driving_times;
            public int bus_stop_time;
            public int[] bus_stop_times;
            public int icon;
            public string[] icons;
            public int passenger;
            public string[] passengers;

            public override void Init(Dictionary<string,Unit> unit) {
                string temp;
                if(item.TryGetValue("short_desc",out temp)) short_desc=temp;
                if(item.TryGetValue("long_desc",out temp)) long_desc=temp;
                if(item.TryGetValue("tier",out temp)) tier=byte.Parse(temp);
                if(item.TryGetValue("rank",out temp)) rank=byte.Parse(temp);
                if(item.TryGetValue("overlay_offset_x",out temp)) overlay_offset_x=int.Parse(temp);
                if(item.TryGetValue("overlay_offset_y",out temp)) overlay_offset_y=int.Parse(temp);
                if(item.TryGetValue("vehicle_data",out temp)) vehicle_data=temp;
            }

            public void LoadAll() {
                string temp;
                if(item.TryGetValue("controller_type",out temp)) controller_type=temp;
                //if(item.TryGetValue("short_desc",out temp)) short_desc=temp;
                //if(item.TryGetValue("long_desc",out temp)) long_desc=temp;
                if(item.TryGetValue("map_name",out temp)) map_name=temp;
                if(item.TryGetValue("time_limit",out temp)) time_limit=int.Parse(temp);
                if(item.TryGetValue("bonus_time_limit",out temp)) bonus_time_limit=int.Parse(temp);

                if(item.TryGetValue("sun_idx",out temp)) sun_idx=sbyte.Parse(temp);
                sun_idxs = new sbyte[sun_idx];
                for(int i = 0;i<sun_idx;i++) {
                    if(item.TryGetValue("sun_idx["+i+"]",out temp)) sun_idxs[i]=sbyte.Parse(temp);
                }

                if(item.TryGetValue("sky_idx",out temp)) sky_idx=sbyte.Parse(temp);
                sky_idxs = new sbyte[sky_idx];
                for(int i = 0;i<sky_idx;i++) {
                    if(item.TryGetValue("sky_idx["+i+"]",out temp)) sky_idxs[i]=sbyte.Parse(temp);
                }

                //if(item.TryGetValue("tier",out temp)) tier=int.Parse(temp);
                //if(item.TryGetValue("rank",out temp)) rank=int.Parse(temp);
                //if(item.TryGetValue("overlay_offset_x",out temp)) overlay_offset_x=int.Parse(temp);
                //if(item.TryGetValue("overlay_offset_y",out temp)) overlay_offset_y=int.Parse(temp);
                //if(item.TryGetValue("vehicle_data",out temp)) vehicle_data=temp;
                if(item.TryGetValue("bus_number",out temp)) bus_number=temp;
                if(item.TryGetValue("time_table_name",out temp)) time_table_name=temp;

                if(item.TryGetValue("bus_stop_cameras",out temp)) bus_stop_camera=int.Parse(temp);
                bus_stop_cameras = new string[bus_stop_camera];
                for(int i = 0;i<bus_stop_camera;i++) {
                    if(item.TryGetValue("bus_stop_cameras["+i+"]",out temp)) bus_stop_cameras[i]=temp;
                }

                if(item.TryGetValue("entering_passengers",out temp)) entering_passenger=int.Parse(temp);
                entering_passengers = new int[entering_passenger];
                for(int i = 0;i<entering_passenger;i++) {
                    if(item.TryGetValue("entering_passengers["+i+"]",out temp)) entering_passengers[i]=int.Parse(temp);
                }

                if(item.TryGetValue("max_leaving_passengers",out temp)) max_leaving_passenger=int.Parse(temp);
                max_leaving_passengers = new int[max_leaving_passenger];
                for(int i = 0;i<max_leaving_passenger;i++) {
                    if(item.TryGetValue("max_leaving_passengers["+i+"]",out temp)) max_leaving_passengers[i]=int.Parse(temp);
                }

                if(item.TryGetValue("driving_times",out temp)) driving_time=int.Parse(temp);
                driving_times = new int[driving_time];
                for(int i = 0;i<driving_time;i++) {
                    if(item.TryGetValue("driving_times["+i+"]",out temp)) driving_times[i]=int.Parse(temp);
                }

                if(item.TryGetValue("bus_stop_times",out temp)) bus_stop_time=int.Parse(temp);
                bus_stop_times = new int[bus_stop_time];
                for(int i = 0;i<bus_stop_time;i++) {
                    if(item.TryGetValue("bus_stop_times["+i+"]",out temp)) bus_stop_times[i]=int.Parse(temp);
                }

                if(item.TryGetValue("passengers",out temp)) passenger=int.Parse(temp);
                passengers = new string[passenger];
                for(int i = 0;i<passenger;i++) {
                    if(item.TryGetValue("passengers["+i+"]",out temp)) passengers[i]=temp;
                }
            }
            
            public void WriteAll(Stream stream) {
                StreamWriter sw = new StreamWriter(stream);

                sw.WriteLine(className+" : "+unitName+" {");
                sw.WriteLine("controller_type: "+controller_type);
                sw.WriteLine("short_desc: \""+short_desc+"\"");
                sw.WriteLine("long_desc: \""+long_desc+"\"");
                sw.WriteLine("map_name: " + map_name);
                sw.WriteLine("time_limit: "+time_limit);
                sw.WriteLine("bonus_time_limit: "+bonus_time_limit);
                sw.WriteLine("sun_idx: "+sun_idx);
                for(int i = 0; i<sun_idxs.Length; i++) {
                    sw.WriteLine("sun_idx["+i+"]: "+ sun_idxs[i]);
                }
                sw.WriteLine("sun_idx: "+sky_idx);
                for(int i = 0; i<sky_idxs.Length; i++) {
                    sw.WriteLine("sky_idx["+i+"]: "+ sky_idxs[i]);
                }
                sw.WriteLine("tier: "+tier);
                sw.WriteLine("rank: "+rank);
                sw.WriteLine("overlay_offset_x: "+overlay_offset_x);
                sw.WriteLine("overlay_offset_y: "+overlay_offset_y);
                sw.WriteLine("vehicle_data: "+vehicle_data);
                sw.WriteLine("bus_number: \""+bus_number+"\"");
                sw.WriteLine("time_table_name: \""+time_table_name+"\"");
                sw.WriteLine("bus_stop_cameras: "+bus_stop_camera);
                for(int i = 0; i<bus_stop_cameras.Length; i++) {
                    sw.WriteLine("bus_stop_cameras["+i+"]: " + bus_stop_cameras[i]);
                }

                sw.WriteLine("entering_passengers: "+entering_passenger);
                for(int i = 0; i<entering_passengers.Length; i++) {
                    sw.WriteLine("entering_passengers["+i+"]: "+ entering_passengers[i]);
                }

                sw.WriteLine("max_leaving_passengers: "+max_leaving_passenger);
                for(int i = 0; i<max_leaving_passengers.Length; i++) {
                    sw.WriteLine("max_leaving_passengers["+i+"]: "+ max_leaving_passengers[i]);
                }

                sw.WriteLine("driving_times: "+driving_time);
                for(int i = 0; i<driving_times.Length; i++) {
                    sw.WriteLine("driving_times["+i+"]: "+ driving_times[i]);
                }
                sw.WriteLine("bus_stop_times: "+bus_stop_time);
                for(int i = 0; i<bus_stop_times.Length; i++) {
                    sw.WriteLine("bus_stop_times["+i+"]: "+ bus_stop_times[i]);
                }
                sw.WriteLine("icons: "+icon);
                for(int i = 0; i<icons.Length; i++) {
                    sw.WriteLine("icons["+i+"]: "+ icons[i]);
                }

                sw.WriteLine("passengers: "+passenger);
                for(int i = 0; i<passengers.Length; i++) {
                    sw.WriteLine("passengers["+i+"]: "+ passengers[i]);
                }

                sw.WriteLine("}");

                sw.Close();
            }
        }
    }

}
