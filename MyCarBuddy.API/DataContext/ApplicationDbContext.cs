using Braintree;
using Microsoft.EntityFrameworkCore;
using MyCarBuddy.API.Models;

namespace MyCarBuddy.API.DataContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<TechniciansModel> Technicians { get; set; }
<<<<<<< HEAD
        public DbSet<BookingInsertDTO> Bookings { get; set; }
=======
        public DbSet<BookingInsertDTO> BookingInsertDTO { get; set; }
>>>>>>> 473ed5a9cfd9e10fc0e00481349b67c9f1ce3d3e
        public DbSet<CustomerDetails> Customers { get; set; }
        public DbSet<StateModel>States { get; set; }
        public DbSet<CityModel>Cities { get; set; }
    }
}
