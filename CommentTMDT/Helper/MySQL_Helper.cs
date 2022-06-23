using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
            catch (Exception)
            {
            }

            await _conn.CloseAsync();

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
