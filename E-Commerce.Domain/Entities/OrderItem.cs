using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Domain.Entities
{
    public class OrderItem
    {

        public int Id { get; set; }

        public int OrderId { set; get; }

        public Order Order { set; get; } = null!;

        public int ProductId { set; get; }

        public Product Product { set; get; } = null!;

        public int Quantity { set; get; }

        public decimal UnitPrice { set; get; }
    }
}
