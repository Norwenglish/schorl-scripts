using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Disc
{
    public partial class SelectiveHealName
    {
        private string SHRaidMemberName;
        public SelectiveHealName(string _ListItem)
        {
            ListItem = _ListItem;
        }
        public string ListItem
        {
            get { return SHRaidMemberName; }
            set { SHRaidMemberName = value; }
        }
    }
}
