using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BamBooShop.Dto
{
    public class ReportHighlight
    {
        public int TotalNewOrder { get; set; }
        public double DailySales { get; set; }
        public int TotalOrder { get; set; }
        public double SalesRevenue { get; set; }

        public List<int> OrderQtyByStatus { get; set; }
        public List<int> OrderQty { get; set; }
        public List<double> Revenues { get; set; }
        public List<List<int>> OrderQtyByStatusInYear { get; set; }
    }
    public class Notification
    {
        public int WipOrders { get; set; }
        public int WipReviews{ get; set; }
        public int Orders { get; set; }


    }
}
