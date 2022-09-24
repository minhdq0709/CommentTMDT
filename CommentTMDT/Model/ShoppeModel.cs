using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentTMDT.Model
{
	class ShoppeModel
	{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Data
    {
      public List<Rating> ratings { get; set; }
      public ItemRatingSummary item_rating_summary { get; set; }
      public bool is_sip_item { get; set; }
      public string rcmd_algo { get; set; }
      public bool downgrade_switch { get; set; }
      public bool has_more { get; set; }
      public bool show_local_review { get; set; }
      public string browsing_ui { get; set; }
      public bool enable_buyer_gallery_media { get; set; }
    }

    public class DetailedRating
    {
      public int product_quality { get; set; }
      public int? seller_service { get; set; }
      public int? delivery_service { get; set; }
    }

    public class ItemRatingReply
    {
      public object orderid { get; set; }
      public object itemid { get; set; }
      public object cmtid { get; set; }
      public int ctime { get; set; }
      public object rating { get; set; }
      public int userid { get; set; }
      public object shopid { get; set; }
      public string comment { get; set; }
      public object rating_star { get; set; }
      public object status { get; set; }
      public int mtime { get; set; }
      public object editable { get; set; }
      public object opt { get; set; }
      public object filter { get; set; }
      public object mentioned { get; set; }
      public bool is_hidden { get; set; }
    }

    public class ItemRatingSummary
    {
      public int rating_total { get; set; }
      public List<int> rating_count { get; set; }
      public int rcount_with_context { get; set; }
      public int rcount_with_image { get; set; }
      public int rcount_with_media { get; set; }
      public int rcount_local_review { get; set; }
      public int rcount_repeat_purchase { get; set; }
      public int rcount_overall_fit_small { get; set; }
      public int rcount_overall_fit_fit { get; set; }
      public int rcount_overall_fit_large { get; set; }
      public int rcount_oversea_review { get; set; }
    }

    public class ProductItem
    {
      public object itemid { get; set; }
      public int shopid { get; set; }
      public string name { get; set; }
      public string image { get; set; }
      public int is_snapshot { get; set; }
      public object snapshotid { get; set; }
      public object modelid { get; set; }
      public string model_name { get; set; }
      public List<string> options { get; set; }
    }

    public class Rating
    {
      public object orderid { get; set; }
      public object itemid { get; set; }
      public ulong cmtid { get; set; }
      public int ctime { get; set; }
      public int rating { get; set; }
      public int userid { get; set; }
      public int shopid { get; set; }
      public string comment { get; set; }
      public int rating_star { get; set; }
      public int status { get; set; }
      public int mtime { get; set; }
      public int editable { get; set; }
      public int opt { get; set; }
      public int filter { get; set; }
      public List<object> mentioned { get; set; }
      public bool is_hidden { get; set; }
      public string author_username { get; set; }
      public string author_portrait { get; set; }
      public int author_shopid { get; set; }
      public bool anonymous { get; set; }
      public List<string> images { get; set; }
      public List<Video> videos { get; set; }
      public List<ProductItem> product_items { get; set; }
      public object delete_reason { get; set; }
      public object delete_operator { get; set; }
      public ItemRatingReply ItemRatingReply { get; set; }
      public List<Tag> tags { get; set; }
      public int? editable_date { get; set; }
      public object show_reply { get; set; }
      public int? like_count { get; set; }
      public object liked { get; set; }
      public bool sync_to_social { get; set; }
      public DetailedRating detailed_rating { get; set; }
      public bool exclude_scoring_due_low_logistic { get; set; }
      public object loyalty_info { get; set; }
      public List<string> template_tags { get; set; }
      public bool has_template_tag { get; set; }
      public object sync_to_social_toggle { get; set; }
      public SipInfo sip_info { get; set; }
      public bool is_repeated_purchase { get; set; }
      public object display_variation_filter { get; set; }
      public int overall_fit { get; set; }
      public bool is_normal_item { get; set; }
      public object viewed { get; set; }
      public object show_view { get; set; }
      public object sync_to_social_detail { get; set; }
    }

    public class Root
    {
      public Data data { get; set; }
      public int error { get; set; }
      public object error_msg { get; set; }
    }

    public class SipInfo
    {
      public bool is_oversea { get; set; }
      public string origin_region { get; set; }
    }

    public class Tag
    {
      public int tag_id { get; set; }
      public string tag_description { get; set; }
    }

    public class Video
    {
      public string id { get; set; }
      public string cover { get; set; }
      public string url { get; set; }
      public int duration { get; set; }
      public object upload_time { get; set; }
      public string mms_ext { get; set; }
    }
  }
}
