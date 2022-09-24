using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentTMDT.Model
{
    public class LazadaModel
    {
        public class Reply
        {
            public object reviewRateId { get; set; }
            public string reviewTime { get; set; }
            public string reviewContent { get; set; }
            public string reviewerName { get; set; }
            public object reviewerBadge { get; set; }
            public bool? isFollowUp { get; set; }
            public object isMeLike { get; set; }
            public int likeCount { get; set; }
            public object helpful { get; set; }
            public bool? canReply { get; set; }
            public bool? rejected { get; set; }
            public object rejectionReason { get; set; }
            public object resubmissionTooltip { get; set; }
            public object resubmissionMessage { get; set; }
            public string reviewStatus { get; set; }
        }

        public class Image
        {
            public string title { get; set; }
            public string url { get; set; }
            public int sort { get; set; }
            public object uploadTime { get; set; }
            public bool? legal { get; set; }
            public string status { get; set; }
            public bool? checkedByPeople { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int size { get; set; }
        }

        public class SubmitInfo
        {
            public string os { get; set; }
            public string source { get; set; }
            public string terminal { get; set; }
            public string network { get; set; }
        }

        public class Item
        {
            public ulong reviewRateId { get; set; }
            public string reviewType { get; set; }
            public ulong buyerId { get; set; }
            public string buyerName { get; set; }
            public object buyerEmail { get; set; }
            public object realBuyerName { get; set; }
            public bool? isAnonymous { get; set; }
            public object tradeOrderId { get; set; }
            public object source { get; set; }
            public string clientType { get; set; }
            public int rating { get; set; }
            public object originalRating { get; set; }
            public object reviewTitle { get; set; }
            public string reviewContent { get; set; }
            public string reviewTime { get; set; }
            public object zonedReviewTime { get; set; }
            public int likeCount { get; set; }
            public object likeText { get; set; }
            public object isMeLike { get; set; }
            public bool? helpful { get; set; }
            public bool? rejected { get; set; }
            public object rejectionReason { get; set; }
            public object resubmissionTooltip { get; set; }
            public object resubmissionMessage { get; set; }
            public object qcReasonId { get; set; }
            public object qcDate { get; set; }
            public bool? isPurchased { get; set; }
            public bool? isGuest { get; set; }
            public object sellerId { get; set; }
            public object sellerName { get; set; }
            public string boughtDate { get; set; }
            public object zonedBoughtDate { get; set; }
            public string reviewStatus { get; set; }
            public object qcTag { get; set; }
            public object qcScore { get; set; }
            public bool? isQced { get; set; }
            public int itemId { get; set; }
            public string itemPic { get; set; }
            public string itemTitle { get; set; }
            public string itemUrl { get; set; }
            public object originPrice { get; set; }
            public object salesPrice { get; set; }
            public object promotion { get; set; }
            public string skuInfo { get; set; }
            public object tags { get; set; }
            public dynamic skuId { get; set; }
            public string configSku { get; set; }
            public string lastReviewer { get; set; }
            public int isReportable { get; set; }
            public object reportStatus { get; set; }
            public object reportDate { get; set; }
            public int reportTimes { get; set; }
            public int upVotes { get; set; }
            public int downVotes { get; set; }
            public int isExternal { get; set; }
            public List<Reply> replies { get; set; }
            public bool? canEdit { get; set; }
            public List<Image> images { get; set; }
            public string imageQCStatus { get; set; }
            public SubmitInfo submitInfo { get; set; }
            public string imageHost { get; set; }
            public bool? showUploader { get; set; }
            public bool? showVideoUploader { get; set; }
            public double relevanceScore { get; set; }
            public int isCompleted { get; set; }
            public long? videoId { get; set; }
            public bool? isVideoReady { get; set; }
            public string videoCoverUrl { get; set; }
            public string avatar { get; set; }
            public object canDownloadImage { get; set; }
            public int targetId { get; set; }
            public string targetType { get; set; }
        }

        public class Paging1
        {
            public int totalItems { get; set; }
            public int totalPages { get; set; }
            public int currentPage { get; set; }
        }

        public class Item2
        {
            public object sellerId { get; set; }
            public int itemId { get; set; }
            public long skuId { get; set; }
            public string configSku { get; set; }
            public object skuInfo { get; set; }
            public string itemPic { get; set; }
            public string itemTitle { get; set; }
            public string itemUrl { get; set; }
            public object categoryId { get; set; }
            public object categoryName { get; set; }
            public object rootCategoryId { get; set; }
            public object rootCategoryName { get; set; }
            public object subSellerId { get; set; }
            public object subSellerName { get; set; }
            public object subShopUrl { get; set; }
        }

        public class Ratings
        {
            public double average { get; set; }
            public int rateCount { get; set; }
            public int reviewCount { get; set; }
            public int imagesCount { get; set; }
            public int videosCount { get; set; }
            public int withImageCount { get; set; }
            public int withVideoCount { get; set; }
            public int withMediaCount { get; set; }
            public List<int> scores { get; set; }
        }

        public class Model
        {
            public List<Item> items { get; set; }
            public Paging1 paging { get; set; }
            public Item item { get; set; }
            public Ratings ratings { get; set; }
        }

        public class Headers
        {
        }

        public class Root
        {
            public bool? success { get; set; }
            public Model model { get; set; }
            public object msgCode { get; set; }
            public int httpStatusCode { get; set; }
            public object msgInfo { get; set; }
            public object mappingCode { get; set; }
            public Headers headers { get; set; }
            public object bizExtMap { get; set; }
        }
    }
}
