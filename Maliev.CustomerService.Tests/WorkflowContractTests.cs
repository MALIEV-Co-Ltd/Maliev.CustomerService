namespace Maliev.CustomerService.Tests;

using System;
using System.IO;

using Xunit;

public sealed class WorkflowContractTests
{
    private static readonly string Root = FindRoot();
    private static readonly string Workflows = Path.Combine(Root, ".github", "workflows");

    [Theory]
    [InlineData("pr-validation.yml", "pull_request:")]
    [InlineData("ci-main.yml", "main")]
    [InlineData("ci-develop.yml", "develop")]
    [InlineData("ci-staging.yml", "release/v*")]
    public void EntryWorkflows_AreReadOnlyValidationOnly(string file, string trigger)
    {
        var text = ReadWorkflow(file);

        Assert.Contains(trigger, text);
        Assert.Contains("contents: read", text);
        Assert.Contains("uses: ./.github/workflows/_validate.yml", text);
        AssertSafe(text);
    }

    [Fact]
    public void ReusableValidation_UsesImmutableCredentialFreeSources()
    {
        var text = ReadWorkflow("_validate.yml");

        Assert.Contains("actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0", text);
        Assert.Contains("actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68", text);
        Assert.Contains("ref: 9c41d6524a485bf03ba022b8170f47366ab1a77a", text);
        Assert.Contains("ref: 979e1bcb3c3ed9c414f652c94b56297543c031b2", text);
        Assert.Equal(3, text.Split("/p:SharedSourceRoot=../shared", StringSplitOptions.None).Length - 1);
        Assert.Equal(3, text.Split("/p:UsePackageReferences=false", StringSplitOptions.None).Length - 1);
        AssertSafe(text);
    }

    [Fact]
    public void ProjectGraph_UsesPortableSharedSourceOverrides()
    {
        foreach (var project in new[] { "Maliev.CustomerService.Api", "Maliev.CustomerService.Application" })
        {
            var text = File.ReadAllText(Path.Combine(Root, project, $"{project}.csproj"));
            Assert.Contains("$(SharedSourceRoot)", text);
            Assert.DoesNotContain("$(GITHUB_ACTIONS)", text);
        }
    }

    [Fact]
    public void EveryWorkflow_ForbidsSecretsAndDeployment()
    {
        foreach (var file in Directory.GetFiles(Workflows, "*.yml"))
        {
            AssertSafe(File.ReadAllText(file));
        }
    }

    private static void AssertSafe(string text)
    {
        foreach (var value in new[]
        {
            "secrets.", "GITOPS_PAT", "GCP_SA_KEY", "NUGET_PASSWORD", "id-token: write",
            "credentials_json", "google-github-actions/auth", "gcloud auth", "docker push",
            "maliev-gitops", "kustomize edit", "gh pr create", "pull_request_target",
        })
        {
            Assert.DoesNotContain(value, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ReadWorkflow(string file) => File.ReadAllText(Path.Combine(Workflows, file));

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Maliev.CustomerService.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate CustomerService repository root.");
    }
}
