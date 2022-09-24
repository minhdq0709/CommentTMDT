using CommentTMDT.Helper;
using CommentTMDT.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace CommentTMDT.Controller
{
	class Lazada
	{
		private const string _urlHome = @"https://www.lazada.vn/";
		private const string _urlAPICommentHome = @"https://my.lazada.vn/pdp/review/getReviewList";
		private readonly static HttpClient _client = new HttpClient();
		private Label _lbError, _lbTotalComment;
		private string _cookie = string.Empty;
		private uint _lastIndex = 0;

		public Lazada(Label lbError, Label lbTotalComment)
		{
			_lbError = lbError;
			_lbTotalComment = lbTotalComment;
		}

		public async Task CrawlData(string cookie)
		{
			_cookie = cookie;
			List<ProductWaitingModel> listurl = new List<ProductWaitingModel>();
			using (MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct))
			{
				listurl = msql.GetLinkProductPriorityByDomain(_urlHome, _lastIndex, 100);
				msql.Dispose();
			}

			_lastIndex += 100;

			if(listurl.Any())
			{
				foreach(ProductWaitingModel item in listurl)
				{
					await GetCommentProduct(item);
					await Task.Delay(20_000);
				}
			}
		}

		private async Task GetCommentProduct(ProductWaitingModel product)
		{
			byte indexPage = 1;
			product.LastCommentUpdate = DateTime.Now.AddDays(-365);
			DateTime lastDateQuery = product.LastCommentUpdate;
			uint count = 0;

			try
			{
				UriBuilder builder = new UriBuilder(_urlAPICommentHome);
				NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);

				query["itemId"] = SplitIdParamToUrl(product.Url) ?? "0";
				query["filter"] = "0";
				query["sort"] = "1";

				while (true)
				{
					query["pageSize"] = "10";
					query["pageNo"] = $"{indexPage++}";

					builder.Query = query.ToString();
					string url1 = builder.ToString();

					HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url1);
					httpRequestMessage.Headers.Add("Cookie", _cookie);
					HttpResponseMessage httpResponseMessage = await _client.SendAsync(httpRequestMessage);

					HttpContent httpContent = httpResponseMessage.Content;
					string result = httpResponseMessage.Content.ReadAsStringAsync().Result;

					LazadaModel.Root data = result.ToObject<LazadaModel.Root>();
					if (data is null || (data.httpStatusCode != 0 && data.httpStatusCode != 200))
					{
						break;
					}

					if (!data.model?.items.Any() ?? true)
					{
						break;
					}

					foreach (LazadaModel.Item item in data.model.items)
					{
						DateTime checkDate = GetDate(item.reviewTime);

						if (checkDate.Year == 1)
						{
							continue;
						}

						if (checkDate.Date < lastDateQuery.Date)
						{
							break;
						}

						lastDateQuery = checkDate;

						/* Set value send to kafka */
						CommentModel temp = new CommentModel();
						temp.IdComment = item.reviewRateId;

						temp.ProductId = item.itemId.ToString();
						temp.Domain = "https://tiki.vn/";
						temp.UrlProduct = product.Url;

						temp.UserComment = item.buyerName;
						temp.Comment = item.reviewContent;

						temp.PostDate = DateTime.Now;
						temp.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(temp.PostDate);
						temp.CommentDate = checkDate;
						temp.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(checkDate);

						temp.Id = Util.ConvertStringtoMD5(temp.UrlProduct + temp.IdComment);

						string jsonObj = JsonSerializer.Serialize<CommentModel>(temp, Util.opt);
						Util.InsertPost(jsonObj);

						++count;
						await Task.Delay(500);
					}

					await Task.Delay(TimeSpan.FromMinutes(4));
				}
			}
			catch (Exception) { }

			using (MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct))
			{
				await msql.UpdateTimeGetCommentPriority(product.Id, lastDateQuery, count);
				await msql.InsertHistoryProduct(_urlHome, 555, product.Url, count, product.Id);

				msql.Dispose();
			}
		}

		private DateTime GetDate(string strDate, string formatTypeDate = @"dd-M-yyyy")
		{
			try
			{
				if (strDate.Contains("giờ trước") || strDate.Contains("phút trước") || strDate.Contains("hours ago"))
				{
					return DateTime.Now;
				}
				else if (strDate.Contains("Hôm qua"))
				{
					return DateTime.Today.AddDays(-1);
				}
				else if (strDate.Contains("ngày trước") || strDate.Contains("day ago"))
				{
					ushort numberDay = (ushort)Util.convertTextToNumber(strDate);
					return DateTime.Today.AddDays(-numberDay);
				}
				else if (strDate.Contains("weeks ago"))
				{
					ushort numberDay = (ushort)(Util.convertTextToNumber(strDate) * 7);
					return DateTime.Today.AddDays(-numberDay);
				}
				else if (strDate.Contains("tháng trước"))
				{
					ushort numberDay = (ushort)(Util.convertTextToNumber(strDate) * 30);
					return DateTime.Today.AddDays(-numberDay);
				}
				else
				{
					try
					{
						strDate = strDate.Replace("thg ", "").Replace(" ", "-");

						return DateTime.ParseExact(strDate, formatTypeDate, CultureInfo.InvariantCulture);
					}
					catch (Exception) { }
				}
			}
			catch (Exception) { }

			return new DateTime();
		}

		private string SplitIdParamToUrl(string url)
		{
			try
			{
				MatchCollection matches = Regex.Matches(url, @"(?<=-i)\d+");
				return matches[0].Value;
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
