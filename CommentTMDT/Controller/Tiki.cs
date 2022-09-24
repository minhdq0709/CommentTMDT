using CommentTMDT.Helper;
using CommentTMDT.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CommentTMDT.Controller
{
	class Tiki
	{
		private HttpClient _client = new HttpClient();
		private const string _urlHome = @"https://tiki.vn";
		private const string _urlApiHome = @"https://tiki.vn/api/v2/reviews";
		private ConcurrentQueue<ProductWaitingModel> _myQueue = new ConcurrentQueue<ProductWaitingModel>();
		private uint start = 0;
		private readonly Telegram_Helper tgl;

		public Tiki()
		{
			tgl = new Telegram_Helper(Config_System.KEY_BOT);
		}

		public async Task CrawlData()
		{
			uint count = 0;

			MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
			//List<(string, string, DateTime)> dataUrl = await msql.GetLinkProductByDomain(_urlHome, start, 100);
			List<ProductWaitingModel> dataUrl = msql.GetLinkProductPriorityByDomain(_urlHome, start, 100); // priority

			msql.Dispose();

			if (!dataUrl.Any())
			{
				start = 0;
				return;
			}

			foreach (ProductWaitingModel item in dataUrl)
			{
				_myQueue.Enqueue(item);
			}

			/* Next data */
			start += 100;

			Task<uint> task1 = GetCommentProduct(1_000);
			Task<uint> task2 = GetCommentProduct(2_000);
			Task<uint> task3 = GetCommentProduct(3_000);
			Task<uint> task4 = GetCommentProduct(4_000);

			await Task.WhenAll(task1, task2, task3, task4); //, task2, task3, task4

			count = task1.Result + task2.Result + task3.Result + task4.Result;
			if (count > 0)
			{
				MySQL_Helper msql1 = new MySQL_Helper(Config_System.ConnectionToTableReportDaily);
				await msql1.InsertToTableReportDaily(_urlHome, count);
				msql1.Dispose();
			}

			await tgl.SendMessageToChannel($"Done {count} comment of tiki", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
		}

		public string GetRegexMatchValue(string input, string regex, RegexOptions options = RegexOptions.IgnoreCase, bool isGetSingleLine = false)
		{
			try
			{
				if (isGetSingleLine)
				{
					options = options | RegexOptions.Singleline;
				}

				return Regex.Match(input, regex, options).Value;
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}

		private async Task<uint> GetCommentProduct(ushort delay)
		{
			ProductWaitingModel obj = new ProductWaitingModel();
			UriBuilder builder = new UriBuilder(_urlApiHome);
			NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
			MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
			uint count = 0;

			query["limit"] = "20";

			while (_myQueue.TryDequeue(out obj))
			{
				DateTime lastDateComment = obj.LastCommentUpdate.Date;
				ushort indexPage = 0;
				(string idProduct, string spid) dataId = SplitIdParamToUrl(obj.UrlToGetComment);

				uint count1 = 0;

				query["spid"] = dataId.spid ?? "0";
				query["product_id"] = dataId.idProduct ?? "0";

				while (true)
				{
					query["page"] = $"{++indexPage}";

					builder.Query = query.ToString();
					string url1 = builder.ToString();

					string json = null;
					using (CancellationTokenSource cts = new CancellationTokenSource(20_000))
					{
						try
						{
							json = await _client.GetStringAsync(url1).ConfigureAwait(false);
						}
						catch (Exception)
						{
							break;
						}
					}

					if (String.IsNullOrEmpty(json))
					{
						break;
					}

					await Task.Delay(delay);

					TikiModel.Root tiki = null;
					try
					{
						tiki = JsonSerializer.Deserialize<TikiModel.Root>(json);
					}
					catch (Exception) { }

					if (!tiki?.data.Any() ?? true)
					{
						break;
					}

					foreach (TikiModel.Datum item in tiki.data)
					{
						if (!string.IsNullOrEmpty(item.timeline?.review_created_date))
						{
							DateTime createComment = GetDate(item.timeline.review_created_date).Date;

							if (createComment < lastDateComment.Date)
							{
								break;
							}

							/* Last date comment */
							lastDateComment = createComment;
							if (!String.IsNullOrEmpty(item.content))
							{
								CommentModel temp = new CommentModel();
								temp.IdComment = (ulong)(item.id ?? 0);

								temp.ProductId = dataId.idProduct;
								temp.Domain = "https://tiki.vn/";
								temp.UrlProduct = obj.Url;

								temp.UserComment = item.created_by?.full_name;
								temp.Comment = item.content;

								temp.PostDate = DateTime.Now;
								temp.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(temp.PostDate);
								temp.CommentDate = createComment;
								temp.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(createComment);

								temp.Id = Util.ConvertStringtoMD5(temp.UrlProduct + temp.IdComment);

								string jsonObj = JsonSerializer.Serialize<CommentModel>(temp, Util.opt);
								Util.InsertPost(jsonObj);

								await Task.Delay(50);
								++count;
								++count1;
							}
						}
					}
				}

				await msql.UpdateTimeGetCommentPriority(Convert.ToInt32(obj.Id), lastDateComment, count1);
				await msql.InsertHistoryProduct(_urlHome, obj.SiteId, obj.Url, count1, obj.Id);

				await Task.Delay(1_000);
			}

			msql.Dispose();

			return count;
		}

		private (string idProduct, string spid) SplitIdParamToUrl(string url)
		{
			try
			{
				MatchCollection matches = Regex.Matches(url, @"\d+(?=.html)|(?<=spid=)\d+");
				if (matches.Count == 2)
				{
					/*[0]: product_id, [1]: spid */
					return (matches[0].Value, matches[1].Value);
				}
			}
			catch (Exception) { }

			return (null, null);
		}

		private DateTime GetDate(string strDate)
		{
			return DateTime.ParseExact(strDate, "yyyy-MM-dd HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None);
		}
	}
}
