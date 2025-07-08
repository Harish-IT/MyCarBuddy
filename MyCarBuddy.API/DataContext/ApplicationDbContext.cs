using Braintree;
using Microsoft.EntityFrameworkCore;
using MyCarBuddy.API.Models;

namespace MyCarBuddy.API.DataContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<TechniciansDetails> Technicians { get; set; }
        public DbSet<Bookings> Bookings { get; set; }
        public DbSet<CustomerDetails> Customers { get; set; }
    }
}
