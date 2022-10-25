namespace OIDC_ArtifactServer.Models
{
    public class ArtifactEntry
    {
        public int Id { get; set; }
        public string EntryName { get; set; }
        public virtual ICollection<ArtifactFile> Files { get; set; } = new List<ArtifactFile>();
        public DateTime TimeAdded { get; set; } = DateTime.UtcNow;
    }
}
