using System;
using Manafont.Db.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Manafont.Db
{
    public class ManafontDbContext : IdentityUserContext<ManafontUser>
    {
        public DbSet<ManafontCharacter> Characters { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseMySql("Server=localhost;User Id=root;Database=manafont", mySqlOpts => mySqlOpts
                .ServerVersion(new Version(10, 5, 5), ServerType.MariaDb));
        }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            builder.UseOpenIddict();
        }
    }
}