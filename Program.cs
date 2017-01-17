using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;

namespace Daedulus.VSTS.BuildSystemStatus
{
    class Program
    {

        private static readonly Uri ServerUri = new Uri("https://zoodata.visualstudio.com");
        static void Main(string[] args)
        {
            var prDict = new Dictionary<Guid, Dictionary<int,GitPullRequest>>();
            var ccs = new VssClientCredentialStorage();
            var pullRequestBuildStatuses = new Dictionary<int, PullRequestBuildStatus>();
            VssCredentials cred = new VssClientCredentials(false);
            cred.Storage = ccs;
            cred.PromptType = CredentialPromptType.PromptIfNeeded;
            var conn = new VssConnection(ServerUri,cred);
            var buildServer = conn.GetClient<BuildHttpClient>();
            var gitClient = conn.GetClient<GitHttpClient>();
            var builds = buildServer.GetBuildsAsync("Zoodata Inspect").Result;

            foreach (var build in builds)
            {
                var projectId = build.Project.Id;
                if (build.Reason == BuildReason.ValidateShelveset)
                {
                    var type = build.Repository.Type;
                    if (type == "TfsGit")
                    {
                        if (!prDict.ContainsKey(projectId))
                        {
                            prDict.Add(build.Project.Id,new Dictionary<int,GitPullRequest>());
                            int offset = 0;
                            int resultCount;
                            do
                            {
                                var results = gitClient.GetPullRequestsByProjectAsync(projectId,
                                    new GitPullRequestSearchCriteria() { Status = PullRequestStatus.All }, skip: offset).Result;
                                foreach (var gitPullRequest in results)
                                {
                                    prDict[gitPullRequest.Repository.ProjectReference.Id][gitPullRequest.PullRequestId] =
                                        gitPullRequest;
                                }
                                resultCount = results.Count;
                                offset = offset + resultCount;

                            } while (resultCount > 0);
                        }
                        var prId = Convert.ToInt32(build.SourceBranch.Split('/')[2]);
                        var pullRequest = prDict[projectId][prId];
                        if (pullRequest == null)
                        {
                            pullRequest = gitClient.GetPullRequestAsync(projectId, build.Repository.Id, prId).Result;
                            prDict[projectId].Add(prId,pullRequest);
                        }

                        if (!pullRequestBuildStatuses.ContainsKey(prId))
                        {
                            pullRequestBuildStatuses.Add(prId,pullRequest.ToBuildStatus(build));
                        }
                    }
                }
            }

            foreach (var status in pullRequestBuildStatuses.Values.Where(pr => pr.Status == PullRequestStatus.Active).OrderBy(p => p.Id))
            {
                Console.WriteLine(status.DisplayString);
            }
            Console.ReadLine();
        }
    }

    public static class GitPullRequestExtensions
    {

        public static PullRequestBuildStatus ToBuildStatus(this GitPullRequest request, Build build)
        {
            PullRequestBuildStatus status = new PullRequestBuildStatus();

            status.Id = request.PullRequestId;
            status.Creator = request.CreatedBy.DisplayName;
            status.ProjectName = request.Repository.ProjectReference.Name;
            status.RepositoryName = request.Repository.Name;
            status.Title = request.Title;
            status.Status = request.Status;
            status.BuildId = build?.Id;
            status.BuildResult = build?.Result;
            status.BuildStatus = build?.Status;
            var finishTime = DateTime.UtcNow;
            if (build?.FinishTime != null)
            {
                finishTime = build.FinishTime.Value;
            }
            status.BuildTime = finishTime - build?.StartTime;
            return status;
        }
    }
}
