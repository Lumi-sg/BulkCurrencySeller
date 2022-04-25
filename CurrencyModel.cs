using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkCurrencySeller
{
    public class Root
    {
        public List<Line> lines { get; set; }
    }
    public class Line
    {
        public string currencyTypeName { get; set; }

        public double chaosEquivalent { get; set; }
    }
}
