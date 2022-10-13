using CommentTMDT.Helper;
using CommentTMDT.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CommentTMDT.Controller
{
	class Shoppe
	{
		private const string _urlHome = @"https://shopee.vn";
		private ConcurrentQueue<ProductWaitingModel> _myQueue = new ConcurrentQueue<ProductWaitingModel>();
		private uint _start = 0;
		private readonly Telegram_Helper tgl;

		public Shoppe()
		{
			tgl = new Telegram_Helper(Config_System.KEY_BOT);
		}

		public async Task Crawl()
		{
			uint count = 0;

			if(!SetDataToQueue())
			{
				_start = 0;
				return;
			}

			_start += 100;

			Task<uint> task1 = Run();
			Task<uint> task2 = Run();

			await Task.WhenAll(task1, task2);

			count += task1.Result + task2.Result;
			if (count > 0)
			{
				using (MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableReportDaily))
				{
					await msql.InsertToTableReportDaily(_urlHome, count);
					msql.Dispose();
				}
			}

			await tgl.SendMessageToChannel($"Done {count} comment of shoppe", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
		}

		private bool SetDataToQueue()
		{
			using (MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct))
			{
				List<ProductWaitingModel> dataUrl = msql.GetLinkProductPriorityByDomain(_urlHome, _start, 100); 
				if(!dataUrl.Any())
				{
					return false;
				}

				foreach (ProductWaitingModel item in dataUrl)
				{
					_myQueue.Enqueue(item);
				}

				msql.Dispose();
				return true;
			}
		}

		private async Task<uint> Run()
		{
			ProductWaitingModel obj;
			uint count = 0;

			while (_myQueue.TryDequeue(out obj))
			{
				count += await Handle(obj);
				await Task.Delay(1_000);
			}

			return count;
		}

		private async Task<uint> Handle(ProductWaitingModel obj)
		{
			uint count = 0;
			(string idItem, string idShop) idProduct = GetIdProduct(obj.UrlToGetComment);
			UriBuilder builder = new UriBuilder(@"https://shopee.vn/api/v2/item/get_ratings");
			NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
			uint offset = 0;
			HttpClient client = new HttpClient();
			DateTime lastDateQuery = obj.LastCommentUpdate;
			DateTime dateMax = obj.LastCommentUpdate;

			if (string.IsNullOrEmpty(idProduct.idItem) || string.IsNullOrEmpty(idProduct.idShop))
			{
				return count;
			}

			query["itemid"] = $"{idProduct.idItem}";
			query["shopid"] = $"{idProduct.idShop}";
			query["limit"] = "50";
			query["type"] = "0";

			while (true)
			{
				query["offset"] = $"{offset}";

				builder.Query = query.ToString();
				string url1 = builder.ToString();

				string json = null;
				using (CancellationTokenSource cts = new CancellationTokenSource(20_000))
				{
					try
					{
						json = await client.GetStringAsync(url1).ConfigureAwait(false);
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

				await Task.Delay(5_000);

				ShoppeModel.Root shpe = null;
				try
				{
					shpe = JsonSerializer.Deserialize<ShoppeModel.Root>(json);
				}
				catch (Exception ex) 
				{ 
					break; 
				}

				/* No data */
				if (!shpe?.data?.ratings?.Any() ?? true)
				{
					break;
				}

				foreach (ShoppeModel.Rating item in shpe.data.ratings)
				{
					DateTime checkDate = Util.UnixTimeStampToDateTime(item.ctime);
					if (checkDate.Date < lastDateQuery.Date)
					{
						continue;
					}

					if(checkDate.Date > dateMax.Date)
					{
						dateMax = checkDate;
					}

					/* Set data send to kafka */
					try
					{
						CommentModel temp = new CommentModel();
						temp.IdComment = item.cmtid;

						temp.ProductId = idProduct.idItem;
						temp.Domain = _urlHome;
						temp.UrlProduct = obj.Url;

						temp.UserComment = item.author_username;
						temp.Comment = item.comment;

						temp.PostDate = DateTime.Now;
						temp.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(temp.PostDate);
						temp.CommentDate = checkDate;
						temp.CommentDateTimeStamp = item.ctime;

						temp.Id = Util.ConvertStringtoMD5(temp.UrlProduct + temp.IdComment);

						/* Send to kafka */
						string jsonObj = JsonSerializer.Serialize<CommentModel>(temp, Util.opt);
						Util.InsertPost(jsonObj);
					}
					catch (Exception) { }

					await Task.Delay(50);
					++count;
				}

				offset += 50;
				await Task.Delay(2_000);
			}

			using (MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct))
			{
				await msql.UpdateTimeGetCommentPriority(Convert.ToInt32(obj.Id), dateMax, count);
				await msql.InsertHistoryProduct(_urlHome, obj.SiteId, obj.Url, count, obj.Id);

				msql.Dispose();
			}

			return count;
		}

		private (string idItem, string idShop) GetIdProduct(string url)
		{
			string match = Regex.Match(url, @"(?<=spid=|-i.)[(\d).(\d)]+")?.Value;
			if (!string.IsNullOrEmpty(match))
			{
				string[] arrId = match.Split('.');
				return (arrId[1], arrId[0]);
			}

			return ("", "");
		}
	}
}
