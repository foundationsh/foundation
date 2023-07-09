﻿using MongoDB.Driver;
using Octokit;
using Reality.Common.Data;
using Reality.Services.Portfolio.Configurations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Project = Reality.Common.Entities.Project;

namespace Reality.Services.Portfolio.Services;

public class ProjectService : IHostedService, IDisposable
{
    private Configuration Configuration { get; }

    private GitHubClient GitHubClient { get; }
    private ILogger<ProjectService> Logger { get; }

    private readonly IMongoCollection<Project> Projects;

    private int ExecutionCount;
    private Timer? Timer;

    public ProjectService(Configuration configuration, GitHubClient github,
        IDatabaseContext databaseContext, ILogger<ProjectService> logger)
    {
        Configuration = configuration;

        GitHubClient = github;
        Logger = logger;
        Projects = databaseContext.GetCollection<Project>("github_projects");
    }

    public Task StartAsync(CancellationToken token)
    {
        Logger.LogInformation("Project Service started.");

        Timer = new Timer(
            callback: (state) =>
            {
                _ = UpdateProjects(state);
            },
            state: null,
            TimeSpan.Zero,
            period: TimeSpan.FromMinutes(Configuration.ProjectUpdateInterval)
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token)
    {
        Timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Timer?.Dispose();
    }


    private async Task UpdateProjects(object? state)
    {
        var count = Interlocked.Increment(ref ExecutionCount);

        Logger.LogInformation("Updating projects. Count: {Count}", count);

        var allRepos = await GitHubClient.Repository.GetAllForCurrent();
        var repoCollection = new RepositoryCollection();

        Logger.LogDebug("Found {Count} repositories: {Repos} belonging to {User}",
            allRepos.Count, string.Join(", ", allRepos.Select(x => x.FullName).ToList()), (await GitHubClient.User.Current()).Login);

        foreach (var repo in allRepos)
            repoCollection.Add(repo.FullName);

        var possibleFiles = await GitHubClient.Search.SearchCode(new SearchCodeRequest
        {
            Path = "/.project",
            FileName = "metadata.yml",
            Repos = repoCollection,

        });

        var projectFiles = possibleFiles.Items.Where(x => x.Name == "metadata.yml");

        Logger.LogDebug("Found {Count} project metadata files", projectFiles.Count());

        foreach (var metadataFile in projectFiles)
        {
            var owner = metadataFile.Repository.Owner.Login;
            var repo = metadataFile.Repository.Name;
            var path = metadataFile.Path;

            Logger.LogDebug("Found repo {Repo} with project metadata", repo);

            var raw = await GitHubClient.Repository.Content.GetRawContent(owner, repo, path);
            if (raw is null || raw.Length is 0)
            {
                Logger.LogError("No content found for {Repo}", repo);
                continue;
            }

            var content = System.Text.Encoding.UTF8.GetString(raw);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var project = deserializer.Deserialize<Project>(content);

            var repoContent = await GitHubClient.Repository.Content.GetAllContents(owner, repo, "/.project");
            var repository = repoContent.Where(c => c.Type == ContentType.File && c.Name == "icon.jpg").FirstOrDefault();
            project.IconUrl ??= repository?.DownloadUrl;
            project.RepositoryUrl ??= repository?.HtmlUrl;

            var filter = Builders<Project>.Filter.Where(x => x.Name == project.Name
                && x.RepositoryUrl == project.RepositoryUrl);

            await Projects.ReplaceOneAsync(filter, project, new ReplaceOptions { IsUpsert = true });

            Logger.LogInformation("Updated project {Project} in database", project.Name);
        }
    }
}