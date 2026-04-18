using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Domain.Entities
{
    public class User
    {
        public  int Id { get; set; }

        public string UserName { get; set; } = string.Empty;
    
        public string Email { get; set; } = string.Empty;

        public string PasswordHash   { get; set; }= string.Empty;

        public Cart ? Cart { set; get; }
         
        public ICollection<Order> Orders { get; set; }  =new List<Order>();
    
    }
}
