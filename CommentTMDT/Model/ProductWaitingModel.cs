using System;

namespace CommentTMDT.Model
{
	class ProductWaitingModel
	{
		public int Id { set; get; }
		public int SiteId { set; get; }
		public string Domain { set; get; }
		public int CateId { set; get; }
		public string Url { set; get; }
		public string Subject { set; get; }
		public string Summary { set; get; }
		public string KeySearch { set; get; }
		public int IsCrawled { set; get; }
		public DateTime PostDate { set; get; }
		public int TotalComment { set; get; }
		public int Vote { set; get; }
		public int IsLock { set; get; }
		public DateTime LastCommentUpdate { set; get; }
		public string UrlToGetComment { set; get; }
	}
}
