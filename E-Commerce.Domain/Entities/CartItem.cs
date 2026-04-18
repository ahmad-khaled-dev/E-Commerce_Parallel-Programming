using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Domain.Entities
{
    public class CartItem
    {

        public int Id { get; set; }

        public int CartId { get; set; }

        public Cart Cart { get; set; } = null!;

        public int ProductId { set; get; }

        public Product Product { get; set; } = null!;

        public int Quantity { set; get; }   
    }
}
