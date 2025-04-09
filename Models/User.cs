using System.Collections.Generic;

namespace Company.Function
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public List<Task> Tasks { get; set; } = new List<Task>();
    }
}