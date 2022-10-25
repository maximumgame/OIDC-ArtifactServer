using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OIDC_ArtifactServer.Authentication;
using OIDC_ArtifactServer.Configuration;
using OIDC_ArtifactServer.Extensions;
using OIDC_ArtifactServer.Models;
using OIDC_ArtifactServer.Project;

namespace OIDC_ArtifactServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArtifactController : ControllerBase
    {
        private readonly ILogger<ArtifactController> _logger;
        private readonly ArtifactDb _db;
        private readonly string BaseFilePath;
        private readonly ProjectEventEmitter _projectEvents;
        private readonly ProjectManagement _projectManagment;

        public ArtifactController(ArtifactDb db, ProjectEventEmitter projectEvents, ProjectManagement projectManagement, ILogger<ArtifactController> logger, IOptions<FileStorage> fileStorageOptions)
        {
            _logger = logger;
            _db = db;
            var fileStorage = fileStorageOptions.Value;
            _projectEvents = projectEvents;
            _projectManagment = projectManagement;
            BaseFilePath = fileStorage.Path;
        }

        [HttpGet("download-artifact")]
        [Authorize]
        public IActionResult DownloadBlob([FromQuery] string project, [FromQuery] string branch, [FromQuery] string entryname, [FromQuery] string artifact)
        {
            // TODO: verify this user's group can access this project/branch (unimplemented feature)

            // currently the assumption is that the file exists at a specific path
            // so we don't do a db lookup for it

            var path = Path.Combine(new[] { BaseFilePath, project, branch, entryname, artifact });
            if (!Path.IsPathFullyQualified(path))
                return Forbid();

            if (!path.IsSubPathOf(BaseFilePath)) //make sure we're not trying to look outside the artifacts directory
                return Forbid();

            return PhysicalFile(path, "application/octet-stream", fileDownloadName: artifact);
        }

        [HttpPost]
        [Authorize("developer")]
        public async Task<IActionResult> UploadArtifact([FromForm] string project, [FromForm] string artifact, [FromForm] string branch, IList<IFormFile> files)
        {
            var path = Path.Combine(BaseFilePath, project, branch, artifact);
            if (!Path.IsPathFullyQualified(path))
                return Forbid();

            if (!path.IsSubPathOf(BaseFilePath)) //make sure we're not trying to look outside the artifacts directory
                return Forbid();

            return Ok();
        }

        [HttpGet("test-get")]
        [Authorize(AuthenticationSchemes = AccessTokenBearerAuthentication.DefaultSchemeName)]
        public IActionResult TestGet()
        {
            return Ok();
        }

        [HttpPost("upload-artifact")]
        [Authorize(AuthenticationSchemes = AccessTokenBearerAuthentication.DefaultSchemeName)]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> UploadArtifact([FromForm] string project, [FromForm] string branch, [FromForm] string entryname)
        {
            var path = Path.Combine(BaseFilePath, project, branch, entryname);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            //setup our db
            var projectInstance = _db.Projects.FirstOrDefault(x => x.Name == project);
            if(projectInstance == null)
            {
                return NotFound("Prject was not found");
            }
            bool isNewBranch = false;
            var branchInstance = projectInstance.Branches.FirstOrDefault(x => x.Name == branch);
            if(branchInstance == null)
            {
                //create this branch
                var newBranch = new ArtifactBranch()
                {
                    Name = branch,
                    LastUpdate = DateTime.UtcNow
                };
                projectInstance.Branches.Add(newBranch);
                await _db.SaveChangesAsync();
                branchInstance = projectInstance.Branches.FirstOrDefault(x => x.Name == branch); //get our ef ref
                isNewBranch = true;
            }

            //really check that branch instance is not null (shouldn't happen)
            if(branchInstance == null)
            {
                return ValidationProblem();
            }

            //check that we aren't trying to overwrite an entry
            var entry = branchInstance.Artifacts.FirstOrDefault(x => x.EntryName == entryname);
            if(entry != null)
            {
                return Forbid("Artifact entry already exists");
            }

            //create our entry
            var newEntry = new ArtifactEntry()
            {
                EntryName = entryname,
                Files = new List<ArtifactFile>(),
                TimeAdded = DateTime.UtcNow
            };

            foreach (var file in HttpContext.Request.Form.Files)
            {
                var filepath = Path.Combine(path, file.FileName);
                if (!filepath.IsSubPathOf(BaseFilePath))
                    return Forbid();

                using(var stream = new FileStream(filepath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    newEntry.Files.Add(new ArtifactFile()
                    {
                        FileName = file.FileName
                    });
                }
            }

            branchInstance.Artifacts.Add(newEntry);

            //update last update times
            projectInstance.LastUpdate = DateTime.UtcNow;
            branchInstance.LastUpdate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (isNewBranch)
                _projectEvents.OnNewBranchInvoke(project, branch);
            _projectEvents.OnArtifactUploadInvoke(project, branch, entryname);

            //check if we have an artifact limit
            var artifactCull = _db.ArtifactCullings.FirstOrDefault(x => x.Branch.Id == branchInstance.Id);
            if(artifactCull != null) //we do, now test if we have exceeded our artifact limit
            {
                while(branchInstance.Artifacts.Count() > artifactCull.MaxArtifacts)
                {
                    var artifactToRemove = branchInstance.Artifacts.OrderBy(x => x.TimeAdded).First();
                    var files = artifactToRemove.Files.ToList();
                    foreach (var entryFile in files)
                    {
                        await _projectManagment.DeleteArtifactFile(projectInstance.Name, branchInstance.Name, artifactToRemove.EntryName, entryFile.FileName);
                    }
                }
            }

            return Ok(new
            {
                project,
                branch
            });
        }
    }
}
