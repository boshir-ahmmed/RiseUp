// Sample file added to test pushing and merging through GitHub.
// It sits outside the projects in RiseUp.slnx, so it is not compiled
// and does not affect the app, the build or the tests. Safe to delete.

namespace RiseUp.Samples.PushTest;

public static class HelloRiseUp
{
    public static string Greet(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? "Hello from RiseUp!"
            : $"Hello, {name.Trim()}! Welcome to RiseUp.";
}
