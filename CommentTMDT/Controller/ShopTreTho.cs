using CommentTMDT.Helper;
using CommentTMDT.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace CommentTMDT.Controller
{
	class ShopTreTho
	{
		private ConcurrentQueue<ProductWaitingModel> _myQueue = new ConcurrentQueue<ProductWaitingModel>();
    private uint _start = 0;
    private const string _urlHome = @"https://shoptretho.com.vn";
    private readonly Telegram_Helper tgl;
    private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();

    public ShopTreTho()
    {
      tgl = new Telegram_Helper(Config_System.KEY_BOT);
    }
    public async Task Crawl()
		{
      /* Push data to queue */
      MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
      List<ProductWaitingModel> dataUrl = msql.GetLinkProductPriorityByDomain(_urlHome, _start, 100);
      msql.Dispose();

      if (!dataUrl.Any())
      {
        _start = 0;
        return;
      }

      _start += 100;

      foreach (ProductWaitingModel item in dataUrl)
      {
        _myQueue.Enqueue(item);
      }

      Task<uint> task1 = Handle();

      await Task.WhenAll(task1);

      uint count = task1.Result;
      if (count > 0)
      {
        MySQL_Helper msql1 = new MySQL_Helper(Config_System.ConnectionToTableReportDaily);
        await msql1.InsertToTableReportDaily(_urlHome, count);
        msql1.Dispose();
      }
    }

    private async Task<uint> Handle()
    {
      ProductWaitingModel obj;
      uint count = 0;

      while (_myQueue.TryDequeue(out obj))
      {
        count += await GetComment(obj);
      }

      await tgl.SendMessageToChannel($"Done {count} comment of ShopTreTho", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
      return count;
    }

    private async Task<uint> GetComment(ProductWaitingModel obj)
		{
      DateTime lastDateQuery = obj.LastCommentUpdate;
      uint count = 0;
      ushort page = 1;

      string productId = await GetProductId(obj.Url);
      if(string.IsNullOrEmpty(productId))
			{
        return count;
			}
      /* Build param for get request */
      UriBuilder builder = new UriBuilder($@"https://shoptretho.com.vn/Product/ProductReviewList/{productId}");
      NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);

      query["commentProductId"] = productId;
      builder.Query = query.ToString();

      while (true)
			{
        query["page"] = $"{page++}";
        builder.Query = query.ToString();

        string html = await HttpClient_Helper.GetData(builder.ToString());
        if(string.IsNullOrEmpty(html))
				{
          break;
				}

        _document.LoadHtml(html);
        html = null;

        #region Comment
        HtmlNodeCollection lstDiv = _document.DocumentNode.SelectNodes("//div[@class='cmt_item']");
        if(lstDiv.Any())
				{
          foreach(HtmlNode item in lstDiv)
					{
            DateTime checkDate = GetDate(item.SelectSingleNode(".//span[@class='comment_item_time']")?.InnerText);
            if(checkDate.Date < lastDateQuery.Date)
						{
              break;
						}

            lastDateQuery = checkDate;
            CommentModel cmtJson = new CommentModel();

            cmtJson.UrlProduct = obj.Url;
            cmtJson.Domain = _urlHome;
            cmtJson.ProductId = productId;
            cmtJson.UserComment = Util.RemoveSpecialCharacter(item.SelectSingleNode(".//div[@class='comment_item_author_name']")?.InnerText);
            cmtJson.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(".//div[@class='comment_item_content']")?.InnerText);
            cmtJson.PostDate = DateTime.Now;
            cmtJson.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(DateTime.Now);
            cmtJson.Id = Util.ConvertStringtoMD5(obj.Url + cmtJson.IdComment.ToString());
            cmtJson.CommentDate = checkDate;
            cmtJson.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmtJson.CommentDate);

            ++count;
          }
				}
        #endregion
      }

      using (MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct))
      {
        await msql.UpdateTimeGetCommentPriority(obj.Id, lastDateQuery, count);
        await msql.InsertHistoryProduct(_urlHome, obj.SiteId, obj.Url, count, obj.Id);

        msql.Dispose();
      }
      await Task.Delay(1_000);

      return count;
		}

    private DateTime GetDate(string dateStr)
		{
      if(string.IsNullOrEmpty(dateStr))
			{
        try
        {
          int day = Int32.Parse(Regex.Match(dateStr, @"(\d+)(?=.ng&#224;y)").Value);
          return DateTime.Now.AddDays(-day);
        }
        catch (Exception ex) { }
      }

      return DateTime.Now;
    }

    private async Task<string> GetProductId(string url)
		{
      string html = await HttpClient_Helper.GetData(url);
      if(string.IsNullOrEmpty(html))
			{
        return string.Empty;
			}

      _document.LoadHtml(html);
      html = null;

      return _document.DocumentNode.SelectSingleNode("//input[@id='hidOrderProductId']").Attributes["value"].Value;
		}
  }
}
