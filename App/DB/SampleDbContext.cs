using Microsoft.EntityFrameworkCore;
using App.Domain.Entities;
using System.Collections.Generic;

namespace App.DB {
    public class SampleDbContext : DbContext {
        public SampleDbContext(DbContextOptions options): base(options) {
        }

        public DbSet<Domain.Entities.Member> Members { get; set; }
    }
}
