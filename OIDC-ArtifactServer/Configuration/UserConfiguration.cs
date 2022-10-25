namespace OIDC_ArtifactServer.Configuration
{
    public class UserConfiguration
    {
        public string GroupClaimName { get; set; } = "groups";
        public GroupPolicyMapping GroupPolicies { get; set; } = new GroupPolicyMapping();
        public IList<string> Scopes { get; set; } = new List<string>();
    }

    /// <summary>
    /// specify how to map a group to a policy
    /// </summary>
    public class GroupPolicyMapping
    {
        public string AdminGroup { get; set; } = "/admin";
        public string DeveloperGroup { get; set; } = "/developer";
        public string UserGroup { get; set; } = "/user";
    }
}
