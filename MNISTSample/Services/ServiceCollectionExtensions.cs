namespace MNISTSample.Services;

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransientForSubtypes<T>(this IServiceCollection services)
    {
        var assembly = Assembly.GetAssembly(typeof(T));
        if (assembly == null)
        {
            throw new InvalidOperationException("The specified assembly couldn't be found.");
        }

        foreach (var type in assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(T))))
        {
            services.AddTransient(type);
        }

        return services;
    }
}