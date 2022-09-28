using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BamBooShop.Dto
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string CustomerCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public double? TotalAmount { get; set; }
        public int Status { get; set; }
        public string Note { get; set; }
        public DateTime Created { get; set; }
        public bool IsPaid { get; set; }
        public CustomerDto Customer { get; set; }
        public List<OrderDetailDto> OrderDetails { get; set; }
    }
}
