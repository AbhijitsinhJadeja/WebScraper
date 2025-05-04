using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Season
    {
        public string SeasonTitle { get; set; }
        public List<Episode> Episodes { get; set; } = new List<Episode>();
    }
}
