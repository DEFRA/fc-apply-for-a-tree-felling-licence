using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.Common;

/// <summary>
/// A modified version of <see cref="DbContextFactorySource`1"/> that iterates over each public constructor of a <see cref="DbContext"/> to
/// find one that accepts <see cref="DbContextOptions"/>. This allows for a <see cref="DbContext"/> implementation to have multiple public
/// constructors, where a default no-arg constructor is required by the EF core tooling to create migrations.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class CustomDbContextFactorySource<TContext> : IDbContextFactorySource<TContext> 
    where TContext : DbContext
{
    public CustomDbContextFactorySource()
        => Factory = CreateActivator();

    /// <inheritdoc />
    public Func<IServiceProvider, DbContextOptions<TContext>, TContext> Factory { get; }

    private static Func<IServiceProvider, DbContextOptions<TContext>, TContext> CreateActivator()
    {
        var constructors
            = typeof(TContext).GetTypeInfo().DeclaredConstructors
                .Where(c => !c.IsStatic && c.IsPublic)
                .ToArray();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();

            if (parameters.Length == 1)
            {
                var isGeneric = parameters[0].ParameterType == typeof(DbContextOptions<TContext>);
                if (isGeneric
                    || parameters[0].ParameterType == typeof(DbContextOptions))
                {
                    var optionsParam = Expression.Parameter(typeof(DbContextOptions<TContext>), "options");
                    var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");

                    return Expression.Lambda<Func<IServiceProvider, DbContextOptions<TContext>, TContext>>(
                            Expression.New(
                                constructor,
                                isGeneric
                                    ? optionsParam
                                    : (Expression)Expression.Convert(optionsParam, typeof(DbContextOptions))),
                            providerParam, optionsParam)
                        .Compile();
                }
            }
        }

        var factory = ActivatorUtilities.CreateFactory(typeof(TContext), new Type[0]);

        return (p, _) => (TContext)factory(p, null);
    }
}