namespace OIDC_ArtifactServer.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<ArtifactBranch> Branches { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
