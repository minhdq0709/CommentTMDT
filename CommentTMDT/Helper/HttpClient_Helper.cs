using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommentTMDT.Helper
{
    public static class HttpClient_Helper
    {
        public static async Task<string> GetDataByPostMethod(HttpClient clien, string urlApiHome, Dictionary<string, string> parameters)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(20_000))
            {
                try
                {
                    FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(parameters);
                    HttpResponseMessage result = await clien.PostAsync(new Uri(urlApiHome), encodedContent);

                    return await result.Content.ReadAsStringAsync();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static async Task<string> GetData(HttpClient clien, string url)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(20_000))
            {
                try
                {
                    return await clien.GetStringAsync(url);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
