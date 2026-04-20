using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Domain.Entities
{
    public class Inventory
    {


        public int Id { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }
         
        public Product Product { get; set; } = null!;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    }
}
