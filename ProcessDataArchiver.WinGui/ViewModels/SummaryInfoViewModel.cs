using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.WinGui.ViewModels
{
    public class SummaryInfoViewModel
    {
        public string Action { get; set; }
        public string TagName { get; set; }
        public string ArchiveName { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }
}
