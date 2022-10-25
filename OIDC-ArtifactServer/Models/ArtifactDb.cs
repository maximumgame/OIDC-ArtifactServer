using Microsoft.EntityFrameworkCore;

namespace OIDC_ArtifactServer.Models
{
    public class ArtifactDb : DbContext
    {
        public ArtifactDb(DbContextOptions<ArtifactDb> opt) : base(opt)
        {

        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<AccessToken> AccessTokens { get; set; }

        public DbSet<ArtifactCulling> ArtifactCullings { get; set; }
    }
}
