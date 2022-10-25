namespace OIDC_ArtifactServer.Models
{
    public class ArtifactBranch
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<ArtifactEntry> Artifacts { get; set; } = new List<ArtifactEntry>();
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }
}
