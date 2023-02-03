using System.Reflection;

namespace Core.CodeGen.File;

public static class DirectoryHelper
{
    public static string AssemblyDirectory
    {
        get
        {
            string codeBase = Assembly.GetExecutingAssembly().Location;
            UriBuilder uri = new(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path)!;
        }
    }

    public static ICollection<string> GetFilesInDirectoryTree(string root, string searchPattern)
    {
        List<string> files = new();
        Stack<string> stack = new();
        stack.Push(root);

        while (stack.Count > 0)
        {
            string dir = stack.Pop();
            files.AddRange(collection: Directory.GetFiles(dir, searchPattern));

            foreach (string subdir in Directory.GetDirectories(dir)) stack.Push(subdir);
        }

        return files;
    }
}