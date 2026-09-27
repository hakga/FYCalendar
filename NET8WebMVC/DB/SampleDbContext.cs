using Microsoft.EntityFrameworkCore;
using NET8WebMVC.Domain.Entities;
using System.Collections.Generic;

namespace NET8WebMVC.DB {
    public class SampleDbContext : DbContext {
        public SampleDbContext(DbContextOptions options): base(options) {
        }

        public DbSet<Domain.Entities.Member> Members { get; set; }
    }
}
