using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentTMDT.Model
{
    public class TikiModel
    {
        public class _1
        {
            public int count { get; set; }
            public int percent { get; set; }
        }

        public class _2
        {
            public int count { get; set; }
            public int percent { get; set; }
        }

        public class _3
        {
            public int count { get; set; }
            public int percent { get; set; }
        }

        public class _4
        {
            public int count { get; set; }
            public int percent { get; set; }
        }

        public class _5
        {
            public int count { get; set; }
            public int percent { get; set; }
        }

        public class Stars
        {
            public _1 _1 { get; set; }
            public _2 _2 { get; set; }
            public _3 _3 { get; set; }
            public _4 _4 { get; set; }
            public _5 _5 { get; set; }
        }

        public class ReviewPhoto
        {
            public int total { get; set; }
            public int total_photo { get; set; }
        }

        public class Summary
        {
            public string joined_time { get; set; }
            public int total_review { get; set; }
            public int total_thank { get; set; }
        }

        public class ContributeInfo
        {
            public uint? id { get; set; }
            public string name { get; set; }
            public string avatar { get; set; }
            public Summary summary { get; set; }
        }

        public class CreatedBy
        {
            public uint? id { get; set; }
            public string name { get; set; }
            public string full_name { get; set; }
            public object region { get; set; }
            public string avatar_url { get; set; }
            public string created_time { get; set; }
            public bool purchased { get; set; }
            public int? purchased_at { get; set; }
            public ContributeInfo contribute_info { get; set; }
        }

        public class Seller
        {
            public int id { get; set; }
            public string name { get; set; }
            public string logo { get; set; }
        }

        public class Timeline
        {
            public string review_created_date { get; set; }
            public string delivery_date { get; set; }
            public string current_date { get; set; }
            public string content { get; set; }
            public string explain { get; set; }
        }

        public class Datum
        {
            public int? id { get; set; }
            public string title { get; set; }
            public string content { get; set; }
            public string status { get; set; }
            public int thank_count { get; set; }
            public double score { get; set; }
            public double? new_score { get; set; }
            public int customer_id { get; set; }
            public int comment_count { get; set; }
            public int rating { get; set; }
            public List<object> images { get; set; }
            public bool thanked { get; set; }
            public int created_at { get; set; }
            public CreatedBy created_by { get; set; }
            public List<string> suggestions { get; set; }
            public List<string> attributes { get; set; }
            public List<object> product_attributes { get; set; }
            public int spid { get; set; }
            public bool is_photo { get; set; }
            public Seller seller { get; set; }
            public int product_id { get; set; }
            public Timeline timeline { get; set; }
            public List<object> comments { get; set; }
        }

        public class Paging
        {
            public int total { get; set; }
            public int per_page { get; set; }
            public int current_page { get; set; }
            public int last_page { get; set; }
            public int from { get; set; }
            public int to { get; set; }
        }

        public class Root
        {
            public List<Datum> data { get; set; }
            public Paging paging { get; set; }
        }
    }
}
