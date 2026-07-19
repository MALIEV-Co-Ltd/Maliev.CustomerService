namespace Maliev.CustomerService.Tests;

using System;
using System.IO;
using System.Xml.Linq;

using Xunit;

public sealed class SharedDependencyFloorContractTests
{
    private static readonly string Root = FindRoot();

    [Theory]
    [InlineData("Maliev.CustomerService.Api/Maliev.CustomerService.Api.csproj", "MassTransit.EntityFrameworkCore", "[8.5.10, 9.0.0)")]
    [InlineData("Maliev.CustomerService.Infrastructure/Maliev.CustomerService.Infrastructure.csproj", "MassTransit.EntityFrameworkCore", "[8.5.10, 9.0.0)")]
    [InlineData("Maliev.CustomerService.Infrastructure/Maliev.CustomerService.Infrastructure.csproj", "Npgsql.EntityFrameworkCore.PostgreSQL", "10.0.3")]
    public void SharedDependencyFloor_IsPinned(string project, string package, string expectedVersion)
    {
        var document = XDocument.Load(Path.Combine(Root, project));
        var reference = Assert.Single(document.Descendants("PackageReference"),
            element => string.Equals((string?)element.Attribute("Include"), package, StringComparison.Ordinal));

        Assert.Equal(expectedVersion, (string?)reference.Attribute("Version"));
    }

    [Fact]
    public void CentralMicrosoftDependencyFloor_IsCurrent()
    {
        var document = XDocument.Load(Path.Combine(Root, "Directory.Build.props"));
        foreach (var reference in document.Descendants("PackageReference"))
        {
            Assert.Equal("10.0.10", (string?)reference.Attribute("Version"));
        }
    }

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
