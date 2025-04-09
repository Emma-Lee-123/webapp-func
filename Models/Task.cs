using System;

namespace Company.Function
{
    public class Task
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public bool? Completed {get; set;}
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public int UserId { get; set; }

        public User User { get; set; }
    }
}