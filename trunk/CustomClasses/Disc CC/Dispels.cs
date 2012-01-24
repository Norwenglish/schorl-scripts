using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Disc
{
    public partial class Dispels
    {
        private string NastyDebuff;
        public Dispels(string _ListItem)
        {
            ListItem = _ListItem;
        }
        public string ListItem
        {
            get { return NastyDebuff; }
            set { NastyDebuff = value; }
        }
    }
}
