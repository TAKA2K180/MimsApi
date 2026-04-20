using System;
using System.Collections.Generic;

namespace MimsApi.Core.models
{
    public class Products
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public ICollection<PackagingHeader> PackagingHeaders { get; set; } = new List<PackagingHeader>();
    }
}
