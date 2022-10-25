using System.ComponentModel.DataAnnotations;

namespace OIDC_ArtifactServer.Models
{
    public class ArtifactCulling
    {
        public int Id { get; set; }
        public virtual ArtifactBranch Branch { get; set; }
        public int MaxArtifacts { get; set; }
    }
}
