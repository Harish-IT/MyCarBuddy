using Braintree;
using Microsoft.EntityFrameworkCore;
using MyCarBuddy.API.Models;

namespace MyCarBuddy.API.DataContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<TechniciansModel> Technicians { get; set; }
        public DbSet<BookingInsertDTO> Bookings { get; set; }
        public DbSet<CustomerDetails> Customers { get; set; }
        public DbSet<StateModel>States { get; set; }
        public DbSet<CityModel>Cities { get; set; }
    }
}
