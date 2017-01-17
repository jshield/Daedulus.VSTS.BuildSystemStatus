using System;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Daedulus.VSTS.BuildSystemStatus
{
    public class PullRequestBuildStatus
    {
        public string ProjectName { get; set; }
        public int Id { get; set; }
        public string Creator { get; set; }
        public BuildStatus? BuildStatus { get; set; }
        public BuildResult? BuildResult { get; set; }
        public string DisplayString => $"{ProjectName} {RepositoryName} {Creator} #{Id}: {Title} {Status} Build: #{BuildId} {BuildStatus} {BuildResult} {BuildTime}";
        public TimeSpan? BuildTime { get; set; }
        public string RepositoryName { get; set; }
        public int? BuildId { get; set; }
        public PullRequestStatus Status { get; set; }
        public string Title { get; set; }
    }
}