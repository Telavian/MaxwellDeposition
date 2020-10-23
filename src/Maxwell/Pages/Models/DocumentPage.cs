using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maxwell.Pages.Models
{
    public class DocumentPage
    {
        public int PageNumber { get; set; }        
        public Dictionary<int, PageLine> LineLookup { get; } = new Dictionary<int, PageLine>();
    }
}
