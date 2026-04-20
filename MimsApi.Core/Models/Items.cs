using System;
using System.Collections.Generic;

namespace MimsApi.Core.models
{
    public class Items
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = "";
        public ICollection<PackagingDetail> PackagingDetails { get; set; } = new List<PackagingDetail>();
    }
}
