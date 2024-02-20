using Microsoft.EntityFrameworkCore;

namespace student_crud.Models
{
    public class StudentContext : DbContext
    {
        public StudentContext(DbContextOptions<StudentContext> options) : base(options)
        {
            
        }
        public DbSet<Student> Students { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }

        public DbSet<StudentDAO> StudentDAOs { get; set; }
    }
}
