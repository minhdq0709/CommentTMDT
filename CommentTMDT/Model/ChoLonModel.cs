using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentTMDT.Model
{
    class ChoLonModel
    {
        public class Child
        {
            public string name { get; set; }
            public string likes { get; set; }
            public int unlikes { get; set; }
            public string showTime { get; set; }
            public string is_admin { get; set; }
            public string id_child { get; set; }
            public string content { get; set; }
            public object cid_product { get; set; }
            public int type_user { get; set; }
        }

        public class Datum
        {
            public string id { get; set; }
            public string name { get; set; }
            public string likes { get; set; }
            public string content { get; set; }
            public string showTime { get; set; }
            public string is_admin { get; set; }
            public int countchild { get; set; }
            public List<Child> child { get; set; }
        }

        public class Data
        {
            public string totalitem { get; set; }
            public int totalpage { get; set; }
            public string id_product { get; set; }
            public string name_product { get; set; }
            public List<Datum> data { get; set; }
        }

        public class Root
        {
            public int status { get; set; }
            public Data data { get; set; }
            public string messages { get; set; }
        }
    }
}
