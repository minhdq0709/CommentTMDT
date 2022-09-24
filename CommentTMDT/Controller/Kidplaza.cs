using CommentTMDT.Helper;
using CommentTMDT.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CommentTMDT.Controller
{
	class Kidplaza
	{
		private const string _urlHome = @"https://www.kidsplaza.vn";
		private const string proxyServer = "http://10.3.51.70:6210";
		private ConcurrentQueue<ProductWaitingModel> _myQueue = new ConcurrentQueue<ProductWaitingModel>();
		public static uint start = 0;
		private readonly Telegram_Helper tgl;

		public Kidplaza()
		{
			tgl = new Telegram_Helper(Config_System.KEY_BOT);
		}

		public async Task Crawl()
		{
			/* Push data to queue */
			MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
			List<ProductWaitingModel> dataUrl = msql.GetLinkProductPriorityByDomain(_urlHome, start, 200);
			msql.Dispose();

			if (!dataUrl.Any())
			{
				start = 0;
				return;
			}

			start += 200;

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

			await tgl.SendMessageToChannel($"Done {count} comment of Kidplaza", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
			return count;
		}

		private async Task<string> GetIdProduct(string url)
		{
			using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
			{
				try
				{
					HttpClient client = CreateHttp();
					string html = await client.GetStringAsync(url);

					HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
					document.LoadHtml(html);

					html = null;
					return document.DocumentNode.SelectSingleNode(@"//div[@class='product attribute sku']/div[@class='value']")?.InnerText ?? "";
				}
				catch (TaskCanceledException)
				{
					return "";
				}
			}
		}

		private async Task<uint> GetComment(ProductWaitingModel obj)
		{
			HttpClient client = CreateHttp();
			byte indexPage = 1;
			uint count = 0;
			DateTime lastCommentQuery = obj.LastCommentUpdate;
			string idProduct = await GetIdProduct(obj.Url);

			if (!string.IsNullOrEmpty(idProduct))
			{
				while (true)
				{
					string html = "";
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
					{
						try
						{
							string url = "";

							try
							{
								url = $"https://www.kidsplaza.vn/reply/ajax/GetReviewHtmlBySku/sku/{idProduct}/?currentPage={indexPage}&imageIncluded=0&pageSize=10000&star=0";

								using (HttpResponseMessage httpResponse = await client.GetAsync(url))
								{
									httpResponse.EnsureSuccessStatusCode();
									html = await httpResponse.Content.ReadAsStringAsync();
								}
							}
							catch (HttpRequestException) { }
							finally { }
						}
						catch (TaskCanceledException) { }
					}

					++indexPage;

					await Task.Delay(TimeSpan.FromSeconds(10));
					HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
					document.LoadHtml(html);

					html = null;
					HtmlNodeCollection divComment = document.DocumentNode.SelectNodes(@"//ol[@id='list_review']/li[@class='item review-item']");

					if (!divComment?.Any() ?? true)
					{
						break;
					}

					foreach (HtmlNode item in divComment)
					{
						DateTime dateComment = GetDate(item.SelectSingleNode(".//time[@class='review-details-value']")?.Attributes["datetime"]?.Value).Date;
						if (dateComment.Year == 1)
						{
							continue;
						}

						if (dateComment.Date < lastCommentQuery.Date)
						{
							break;
						}

						lastCommentQuery = dateComment;

						CommentModel cmtJson = new CommentModel();

						cmtJson.UrlProduct = obj.Url;
						cmtJson.Domain = _urlHome;
						cmtJson.ProductId = idProduct;
						cmtJson.UserComment = Util.RemoveSpecialCharacter(item.SelectSingleNode(".//strong[@class='review-details-value']")?.InnerText);
						cmtJson.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(".//div[@class='review-content']")?.InnerText);
						cmtJson.PostDate = DateTime.Now;
						cmtJson.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(DateTime.Now);
						cmtJson.IdComment = ((ulong)(String.IsNullOrEmpty(item.Attributes["data-id"]?.Value) ? -1 : Util.convertTextToNumber(item.Attributes["data-id"]?.Value)));
						cmtJson.Id = Util.ConvertStringtoMD5(obj.Url + cmtJson.IdComment.ToString());
						cmtJson.CommentDate = dateComment;
						cmtJson.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmtJson.CommentDate);

						/* Send to Kafka */
						string json = JsonSerializer.Serialize<CommentModel>(cmtJson, Util.opt);
						Util.InsertPost(json);

						await Task.Delay(50);
						++count;
					}
				}
			}

			using (MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct))
			{
				await msql.UpdateTimeGetCommentPriority(obj.Id, lastCommentQuery, count);
				await msql.InsertHistoryProduct(_urlHome, 555, obj.Url, count, obj.Id);

				msql.Dispose();
			}
			return count;
		}

		private DateTime GetDate(string strDate, string pattern = @"\d{2}/\d{2}/\d{4}", string formatTypeDate = @"dd/MM/yyyy")
		{
			if (!String.IsNullOrEmpty(strDate))
			{
				Regex rg = new Regex(pattern);
				if (rg.IsMatch(strDate))
				{
					MatchCollection matched = rg.Matches(strDate);
					try
					{
						return DateTime.ParseExact(matched[0].Value, formatTypeDate, CultureInfo.InvariantCulture);
					}
					catch (Exception) { }
				}
			}

			return new DateTime();
		}

		private HttpClient CreateHttp()
		{
			HttpClientHandler handler = new HttpClientHandler();

			handler = new HttpClientHandler
			{
				Proxy = new WebProxy(proxyServer),
				UseProxy = true
			};

			HttpClient client = new HttpClient(handler);
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

			return client;
		}
	}
}
