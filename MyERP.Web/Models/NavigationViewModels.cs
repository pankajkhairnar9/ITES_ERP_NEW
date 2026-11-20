using System.Collections.Generic;
using System.Linq;

namespace MyERP.Web.Models
{
    public sealed class NavigationGroupViewModel
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool IsActive { get; set; }
        public List<NavigationLinkViewModel> Items { get; set; } = new List<NavigationLinkViewModel>();
    }

    public sealed class NavigationLinkViewModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ApprovalNoticesViewModel
    {
        public List<ApprovalNoticeViewModel> Items { get; set; } = new List<ApprovalNoticeViewModel>();
        public int Total => Items.Sum(x => x.Count);
    }

    public sealed class ApprovalNoticeViewModel
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
