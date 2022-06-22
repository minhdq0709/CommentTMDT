using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentTMDT.Helper
{
    public static class Json_Helper
    {
        public static T ToObject<T>(this string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch(Exception ex)
            {
                return default;
            }
        }
    }
}
