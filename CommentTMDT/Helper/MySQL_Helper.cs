using CommentTMDT.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;

namespace CommentTMDT.Helper
{
	class MySQL_Helper : IDisposable
	{
		private readonly MySqlConnection _conn;

		public MySQL_Helper(string connection)
		{
			_conn = new MySqlConnection(connection);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_conn.State == System.Data.ConnectionState.Open)
				{
					_conn.Close();
					_conn.Dispose();
				}
				else
				{
					_conn.Dispose();
				}
			}
		}

		public async Task<List<(string, string, DateTime)>> GetLinkProductByDomain(string domain, uint start, uint end)
		{
			List<(string, string, DateTime)> data = new List<(string, string, DateTime)>();
			string query = $"SELECT id, url, CommentUpdate FROM EcommerceDb.products where domain = '{domain}' limit {start}, {end};";

			try
			{
				using (MySqlCommand cmd = new MySqlCommand(query, _conn))
				{
					await _conn.OpenAsync();
					using (DbDataReader reader = await cmd.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							string timeStr = string.IsNullOrEmpty(reader["CommentUpdate"].ToString()) == true ? $"{DateTime.Now.Year}/01/01" : reader["CommentUpdate"].ToString();

							data.Add(
								(
									reader["id"].ToString(),
									reader["url"].ToString(),
									Convert.ToDateTime(timeStr)
								)
							);
						}
					}
				}
			}
			catch (Exception ex)
			{
			}

			await _conn.CloseAsync();

			return data;
		}

		public List<ProductWaitingModel> GetLinkProductPriorityByDomain(string domain, uint start, uint end)
		{
			List<ProductWaitingModel> data = new List<ProductWaitingModel>();
			string query = $"SELECT * FROM EcommerceDb.productwaiting where Domain = '{domain}' and KeySearch like '%vi sinh' and  IsCrawled = 1 limit {start}, {end};"; //and KeySearch = 'men ống vi sinh' and  IsCrawled = 1limit {start}, {end};

			try
			{
				using (MySqlCommand cmd = new MySqlCommand(query, _conn))
				{
					_conn.Open();
					using (DbDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							string timeStr = string.IsNullOrEmpty(reader["LastCommentUpdate"].ToString()) ? $"{DateTime.Now.Year}/01/01" : reader["LastCommentUpdate"].ToString();

							data.Add(new ProductWaitingModel
							{
								Id = Convert.ToInt32(reader["Id"].ToString()),
								SiteId = Convert.ToInt32(reader["SiteId"].ToString()),
								Url = reader["Url"].ToString(),
								LastCommentUpdate = Convert.ToDateTime(timeStr),
								UrlToGetComment = reader["UrlToGetComment"].ToString()
							}
							);
						}
					}
				}
			}
			catch (Exception ex) { }

			_conn.Close();

			return data;
		}

		public async Task<int> UpdateTimeGetComment(string id)
		{
			int row = 0;

			try
			{
				await _conn.OpenAsync();

				string query = "UPDATE EcommerceDb.products SET CommentUpdate = @CommentUpdate WHERE id = @id";

				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = _conn;
				cmd.CommandText = query;

				cmd.Parameters.Add("@CommentUpdate", MySqlDbType.Date).Value = DateTime.Now.ToString("yyyy-MM-dd");
				cmd.Parameters.Add("@id", MySqlDbType.String).Value = id;

				row = await cmd.ExecuteNonQueryAsync();
			}
			catch (Exception) { }

			if (_conn != null)
			{
				await _conn.CloseAsync();
			}

			return row;
		}

		public async Task<int> UpdateTimeGetCommentPriority(int id, DateTime lastDateComment, uint totalComment)
		{
			int row = 0;

			try
			{
				await _conn.OpenAsync();

				string query = "UPDATE EcommerceDb.productwaiting SET LastCommentUpdate = @LastCommentUpdate, TotalComment = @TotalComment WHERE Id = @id";

				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = _conn;
				cmd.CommandText = query;

				cmd.Parameters.Add("@LastCommentUpdate", MySqlDbType.Date).Value = lastDateComment.ToString("yyyy-MM-dd");
				cmd.Parameters.Add("@TotalComment", MySqlDbType.UInt32).Value = totalComment;
				cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

				row = await cmd.ExecuteNonQueryAsync();
			}
			catch (Exception ex)
			{
				File.AppendAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Check/err2.txt", "UpdateTimeGetCommentPriority" + ex.ToString() + "\n");
			}

			if (_conn != null)
			{
				await _conn.CloseAsync();
			}

			return row;
		}

		public async Task<int> InsertHistoryProduct(string domain, int siteId, string sourceLink, uint totalComment, int productId = 0)
		{
			int row = 0;

			try
			{
				await _conn.OpenAsync();

				string query = "Insert into EcommerceDb.HistoryProducts(SiteId, Domain, ProductId, Link, PostDate, TotalComment) values(@SiteId, @Domain, @ProductId, @Link, @PostDate, @TotalComment);";

				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = _conn;
				cmd.CommandText = query;

				cmd.Parameters.Add("@SiteId", MySqlDbType.Int32).Value = siteId;
				cmd.Parameters.Add("@Domain", MySqlDbType.String).Value = domain;
				cmd.Parameters.Add("@Link", MySqlDbType.String).Value = sourceLink;
				cmd.Parameters.Add("@PostDate", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd");
				cmd.Parameters.Add("@TotalComment", MySqlDbType.UInt32).Value = totalComment;
				cmd.Parameters.Add("@ProductId", MySqlDbType.Int32).Value = productId;

				row = await cmd.ExecuteNonQueryAsync();
			}
			catch (Exception ex) 
			{ 
				File.AppendAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Check/err2.txt", "InsertHistoryProduct" + ex.ToString() + "\n"); 
			}

			if (_conn != null)
			{
				await _conn.CloseAsync();
			}

			return row;
		}

		public async Task<int> InsertToTableReportDaily(string domain, uint count)
		{
			int row = 0;

			try
			{
				await _conn.OpenAsync();

				string query = "Insert into comment_eco.report_daily(domain, total, create_time) values(@domain, @count, @create_time);";

				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = _conn;
				cmd.CommandText = query;

				cmd.Parameters.Add("@create_time", MySqlDbType.Date).Value = DateTime.Now.ToString("yyyy-MM-dd");
				cmd.Parameters.Add("@domain", MySqlDbType.Text).Value = domain;
				cmd.Parameters.Add("@count", MySqlDbType.Int32).Value = count;

				row = await cmd.ExecuteNonQueryAsync();
			}
			catch (Exception) { }

			if (_conn != null)
			{
				await _conn.CloseAsync();
			}

			return row;
		}
	}
}
