namespace OIDC_ArtifactServer.Project
{
    public class ProjectEventEmitter
    {
        private readonly ILogger<ProjectEventEmitter> _logger;
        public ProjectEventEmitter(ILogger<ProjectEventEmitter> logger)
        {
            _logger = logger;
        }

        //new items
        public delegate void OnArtifactUploadHandler(object sender, string project, string branch, string artifact);
        public event OnArtifactUploadHandler OnArtifactUpload;
        public delegate void OnNewBranchHandler(object sender, string project, string branch);
        public event OnNewBranchHandler OnNewBranch;

        public void OnArtifactUploadInvoke(string project, string branch, string artifact)
        {
            OnArtifactUpload?.Invoke(this, project, branch, artifact);
        }

        public void OnNewBranchInvoke(string project, string branch)
        {
            OnNewBranch?.Invoke(this, project, branch);
        }

        //deleted items
        public delegate void OnArtifactDeleteHandler(object sender, string project, string branch, string artifact);
        public event OnArtifactUploadHandler OnArtifactDelete;
        public delegate void OnDeletedBranchHandler(object sender, string project, string branch);
        public event OnDeletedBranchHandler OnDeletedBranch;
        public delegate void OnDeletedProjectHandler(object sender, string project);
        public event OnDeletedProjectHandler OnDeletedProject;

        public void OnArtifactDeleteInvoke(string project, string branch, string artifact)
        {
            OnArtifactDelete?.Invoke(this, project, branch, artifact);
        }

        public void OnDeletedBranchInvoke(string project, string branch)
        {
            OnDeletedBranch?.Invoke(this, project, branch);
        }

        public void OnDeletedProjectInvoke(string project)
        {
            OnDeletedProject?.Invoke(this, project);
        }
    }
}
