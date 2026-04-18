using E_Commerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Domain.Entities
{
    public class Order
    {

        public int Id { get; set; }

        public int UserId {  get; set; }    
        public User User { get; set; } = null!;

        public DateTime CreatedAt { set; get; } = DateTime.UtcNow;

        public decimal TotalAmount { set; get; }

        public OrderStatus OrderStatus { set; get; } = OrderStatus.Pending;


        public ICollection<OrderItem> Items { get; set; } =new HashSet<OrderItem>();

    }
}
