using System;
using System.Collections.Generic;

namespace MimsApi.Core.models
{
    public class PackagingHeader
    {
        public int Id { get; set; }
        public string PackagingName { get; set; }
        public string PackagingType { get; set; }
        public int ProductId { get; set; }
        public Products Product { get; set; }
        public int? ParentPackagingId { get; set; }
        public PackagingHeader ParentPackaging { get; set; }
        public ICollection<PackagingHeader> ChildPackagings { get; set; } = new List<PackagingHeader>();
        public ICollection<PackagingDetail> PackagingDetails { get; set; } = new List<PackagingDetail>();
    }
}
