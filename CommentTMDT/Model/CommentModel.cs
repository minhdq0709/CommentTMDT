using System;

namespace CommentTMDT.Model
{
    class CommentModel
    {
        /* Id: Url Product + (id comment ?? -1)  => MD5*/
        public string Id { set; get; }
        public string ProductId { get; set; }
        public string Domain { get; set; }
        public string UrlProduct { get; set; }
        public string UserComment { get; set; }
        public string Comment { get; set; }
        public DateTime PostDate { get; set; }
        public double PostDateTimeStamp { get; set; }
        public DateTime CommentDate { set; get; }
        public double CommentDateTimeStamp { set; get; }
        public ulong IdComment { set; get; }
    }
}
