using Splat;

namespace PinBoard.Util;

public static class ReadonlyDependencyResolverExtensions
{
    public static T GetRequiredService<T>(this IReadonlyDependencyResolver resolver)
    {
        return resolver.GetService<T>() ?? throw new InvalidOperationException($"{typeof(T).FullName} not found");
    }
}
