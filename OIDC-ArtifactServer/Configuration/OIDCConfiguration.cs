namespace OIDC_ArtifactServer.Configuration
{
    public class OIDCConfiguration
    {
        public string Authority { get; set; } = String.Empty;
        public string MetadataAddress { get; set; } = String.Empty;
        public string ClientId { get; set; } = String.Empty;
        public string ClientSecret { get; set; } = String.Empty;
    }
}
