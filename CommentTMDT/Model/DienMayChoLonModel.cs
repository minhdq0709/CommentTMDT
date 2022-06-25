using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentTMDT.Model
{
    class DienMayChoLonModel
    {
        public class Child
        {
            public string id_child { get; set; }
            public string name { get; set; }
            public string content { get; set; }
            public object cid_product { get; set; }
            public int type_user { get; set; }
            public object picture85 { get; set; }
            public object picture450 { get; set; }
            public string likes { get; set; }
            public int unlikes { get; set; }
            public object level { get; set; }
            public string showTime { get; set; }
        }

        public class Data
        {
            public int TotalItem { get; set; }
            public int Totalpage { get; set; }
            public List<Datum> data { get; set; }
        }

        public class Datum
        {
            public string id { get; set; }
            public string name { get; set; }
            public string content { get; set; }
            public int cid_product { get; set; }
            public int type_user { get; set; }
            public object picture85 { get; set; }
            public object picture450 { get; set; }
            public string likes { get; set; }
            public int unlikes { get; set; }
            public object level { get; set; }
            public int total_child { get; set; }
            public List<Child> child { get; set; }
            public string showTime { get; set; }
        }

        public class Root
        {
            public int status { get; set; }
            public string message { get; set; }
            public Data data { get; set; }
        }
    }
}
