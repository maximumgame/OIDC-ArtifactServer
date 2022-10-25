using Microsoft.Extensions.Options;
using OIDC_ArtifactServer.Configuration;
using OIDC_ArtifactServer.Models;
using OIDC_ArtifactServer.Extensions;

namespace OIDC_ArtifactServer.Project
{
    public class ProjectManagement
    {
        private readonly ArtifactDb db;
        private readonly ILogger<ProjectManagement> logger;
        private readonly string BaseFilePath;

        /// <summary>
        /// Responsible for deleting projects and resources from disk and database
        /// </summary>
        /// <param name="db"></param>
        /// <param name="logger"></param>
        /// <param name="fileStorageOptions"></param>
        public ProjectManagement(ArtifactDb db, ILogger<ProjectManagement> logger, IOptions<FileStorage> fileStorageOptions)
        {
            this.db = db;
            this.logger = logger;
            BaseFilePath = fileStorageOptions.Value.Path;
        }

        public async Task<bool> DeleteArtifactFile(string projectName, string branchName, string entryName, string file)
        {
            var projectInstance = db.Projects.FirstOrDefault(x => x.Name == projectName);
            if (projectInstance == null)
                return false;
            var branchInstance = projectInstance.Branches.FirstOrDefault(x => x.Name == branchName);
            if (branchInstance == null)
                return false;
            var entryInstance = branchInstance.Artifacts.FirstOrDefault(x => x.EntryName == entryName);
            if (entryInstance == null)
                return false;
            var fileInstance = entryInstance.Files.FirstOrDefault(x => x.FileName == file);
            if (fileInstance == null)
                return false;

            var filePath = Path.Combine(new[] { BaseFilePath, projectName, branchName, entryName, fileInstance.FileName });
            if (!filePath.IsSubPathOf(BaseFilePath))
                return false;

            try
            {
                File.Delete(filePath);
                entryInstance.Files.Remove(fileInstance);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return false;
            }

            if(entryInstance.Files.Count == 0) //empty lets delete the entry
            {
                branchInstance.Artifacts.Remove(entryInstance);
                //and delete directory
                var dirPath = Path.Combine(new[] { BaseFilePath, projectName, branchName, entryName });
                try
                {
                    Directory.Delete(dirPath, true);
                }
                catch(Exception e)
                {
                    logger.LogError(e.Message);
                }
                await db.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> DeleteArtifactEntry(string projectName, string branchName, string entryName)
        {
            var projectInstance = db.Projects.FirstOrDefault(x => x.Name == projectName);
            if (projectInstance == null)
                return false;
            var branchInstance = projectInstance.Branches.FirstOrDefault(x => x.Name == branchName);
            if (branchInstance == null)
                return false;
            var entryInstance = branchInstance.Artifacts.FirstOrDefault(x => x.EntryName == entryName);
            if (entryInstance == null)
                return false;

            var dirPath = Path.Combine(new[] { BaseFilePath, projectName, branchName, entryName });
            if (!dirPath.IsSubPathOf(BaseFilePath))
                return false;

            try
            {
                Directory.Delete(dirPath, true);
                branchInstance.Artifacts.Remove(entryInstance);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return false;
            }

            return true;
        }

        public async Task<bool> DeleteBranch(string projectName, string branchName)
        {
            var projectInstance = db.Projects.FirstOrDefault(x => x.Name == projectName);
            if (projectInstance == null)
                return false;
            var branchInstance = projectInstance.Branches.FirstOrDefault(x => x.Name == branchName);
            if (branchInstance == null)
                return false;

            var dirPath = Path.Combine(new[] { BaseFilePath, projectName, branchName });
            if (!dirPath.IsSubPathOf(BaseFilePath))
                return false;

            try
            {
                Directory.Delete(dirPath, true);
                projectInstance.Branches.Remove(branchInstance);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return false;
            }

            return true;
        }

        public async Task<bool> DeleteProject(string projectName)
        {
            var projectInstance = db.Projects.FirstOrDefault(x => x.Name == projectName);
            if (projectInstance == null)
                return false;

            var dirPath = Path.Combine(new[] { BaseFilePath, projectName });
            if(!dirPath.IsSubPathOf(BaseFilePath))
                return false;

            try
            {
                Directory.Delete(dirPath, true);
                db.Projects.Remove(projectInstance);
                await db.SaveChangesAsync();
            }
            catch(Exception e)
            {
                logger.LogError(e.Message);
                return false;
            }

            return true;


            //Get all branches
            var branches = projectInstance.Branches.ToList();
            foreach (var branch in branches)
            {
                //get all artifacts
                var artifactEntries = branch.Artifacts.ToList();
                foreach (var artifactEntry in artifactEntries)
                {
                    //get each artifact
                    var artifacts = artifactEntry.Files.ToList();
                    foreach (var file in artifacts)
                    {
                        //construct file path
                        var filePath = Path.Combine(new[] { BaseFilePath, projectInstance.Name, branch.Name, artifactEntry.EntryName, file.FileName });

                        //make sure we are still in base path
                        if (!filePath.IsSubPathOf(BaseFilePath))
                        {
                            await db.SaveChangesAsync();
                            return false;
                        }

                        //delete each file
                        try
                        {
                            File.Delete(filePath);
                            artifactEntry.Files.Remove(file);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.Message);
                            await db.SaveChangesAsync();
                            return false;
                        }
                    }
                    artifactEntries.Remove(artifactEntry);
                }
                branches.Remove(branch);
            }
            db.Projects.Remove(projectInstance);

            await db.SaveChangesAsync();

            return true;
        }
    }
}
