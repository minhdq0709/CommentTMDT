using System.Collections.Generic;

namespace CommentTMDT.Model
{
    class SendoModel
    {
        public class CartAttribute
        {
            public string search_key { get; set; }
            public int product_option_id { get; set; }
            public string value { get; set; }
            public string background { get; set; }
        }

        public class Datum1
        {
            public int rating_id { get; set; }
            public string user_name { get; set; }
            public string avatar { get; set; }
            public int customer_id { get; set; }
            public string comment { get; set; }
            public List<object> comments { get; set; }
            public string comment_title { get; set; }
            public int star { get; set; }
            public string status { get; set; }
            public int type { get; set; }
            public int like_count { get; set; }
            public int total_comment { get; set; }
            public int update_time { get; set; }
            public List<string> images { get; set; }
            public bool like_status { get; set; }
            public List<CartAttribute> cart_attributes { get; set; }
            public bool is_commentable { get; set; }
        }

        public class MetaData
        {
            public int total_count { get; set; }
            public int total_page { get; set; }
            public int current_page { get; set; }
            public int count_image { get; set; }
            public int count_star_1 { get; set; }
            public int count_star_2 { get; set; }
            public int count_star_3 { get; set; }
            public int count_star_4 { get; set; }
            public int count_star_5 { get; set; }
        }

        public class Root
        {
            public List<Datum1> data { get; set; }
            public MetaData meta_data { get; set; }
        }
    }
}
