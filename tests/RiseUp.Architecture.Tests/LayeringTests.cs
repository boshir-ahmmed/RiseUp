using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace RiseUp.Architecture.Tests;

/// <summary>Enforces Web.UI → Facade → Entities ← DataAccess, and the legacy-CRM corrections.</summary>
public class LayeringTests
{
    private static readonly Assembly Framework = typeof(RiseUp.Framework.Results.Result).Assembly;
    private static readonly Assembly Entities = typeof(RiseUp.Entities.Common.BaseEntity).Assembly;
    private static readonly Assembly DataAccess = typeof(RiseUp.DataAccess.RiseUpDbContext).Assembly;
    private static readonly Assembly Facade = typeof(RiseUp.Facade.DependencyInjection).Assembly;
    private static readonly Assembly Web = typeof(RiseUp.Web.UI.Infrastructure.AppController).Assembly;

    private static void Assert(TestResult result, string because) =>
        Xunit.Assert.True(result.IsSuccessful, because + " Offenders: " + string.Join(", ", result.FailingTypeNames ?? []));

    [Fact] public void Framework_HasNoApplicationDependencies() =>
        Assert(Types.InAssembly(Framework).ShouldNot().HaveDependencyOnAny("RiseUp.Entities", "RiseUp.DataAccess", "RiseUp.Facade", "RiseUp.Web").GetResult(), "Framework is the base layer.");

    [Fact] public void Entities_DependOnlyOnFramework() =>
        Assert(Types.InAssembly(Entities).ShouldNot().HaveDependencyOnAny("RiseUp.DataAccess", "RiseUp.Facade", "RiseUp.Web", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore").GetResult(), "Entities must stay pure.");

    [Fact] public void Facade_DoesNotReferenceEfCoreDataAccessOrWeb() =>
        Assert(Types.InAssembly(Facade).ShouldNot().HaveDependencyOnAny("RiseUp.DataAccess", "RiseUp.Web", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore").GetResult(), "Facade uses repository ports only (no AddDbContext in Facade).");

    [Fact] public void DataAccess_DoesNotReferenceFacadeOrWeb() =>
        Assert(Types.InAssembly(DataAccess).ShouldNot().HaveDependencyOnAny("RiseUp.Facade", "RiseUp.Web").GetResult(), "DataAccess implements Entities ports.");

    [Fact] public void Controllers_DoNotTouchDbContextOrDataAccess() =>
        Assert(Types.InAssembly(Web).That().Inherit(typeof(Controller)).ShouldNot().HaveDependencyOnAny("RiseUp.DataAccess", "Microsoft.EntityFrameworkCore").GetResult(), "Controllers must stay thin and go through the Facade.");

    [Fact] public void Controllers_DoNotBindEntities()
    {
        var entityTypes = Entities.GetTypes().Where(t => t.Namespace?.StartsWith("RiseUp.Entities") == true && t.IsClass && !t.IsAbstract && t.Namespace != "RiseUp.Entities.Abstractions").ToHashSet();
        var offenders = Web.GetTypes().Where(t => typeof(Controller).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetParameters().Any(p => entityTypes.Contains(p.ParameterType)))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}").ToList();
        Xunit.Assert.Empty(offenders);   // fixes legacy Login(User user) over-posting
    }

    [Fact]
    public void Repositories_ExposeNoSaveChanges()
    {
        var repoInterfaces = Entities.GetTypes().Where(t => t.IsInterface && t.Name.EndsWith("Repository"));
        Xunit.Assert.All(repoInterfaces, i => Xunit.Assert.DoesNotContain(i.GetMethods(), m => m.Name.Contains("SaveChanges")));
    }

    [Fact]
    public void Facades_AreRegisteredOnlyThroughInterfaces()
    {
        var facades = Facade.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Facade"));
        Xunit.Assert.All(facades, f => Xunit.Assert.Contains(f.GetInterfaces(), i => i.Name == "I" + f.Name));
    }
}
