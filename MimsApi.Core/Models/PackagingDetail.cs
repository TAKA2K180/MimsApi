using System;
using System.Collections.Generic;

namespace MimsApi.Core.models
{
    public class PackagingDetail
    {
        public int Id { get; set; }
        public int PackagingHeaderId { get; set; }
        public PackagingHeader PackagingHeader { get; set; }
        
        // Foreign key for items
        public int ItemsId { get; set; }
        public Items Items { get; set; }
        
        // Quantity of items in this packaging detail
        public int Quantity { get; set; }
    }
}
