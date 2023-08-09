using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ByteDev.DotNet.Project;
using ByteDev.DotNet.Solution;
using Foundation.Tools.Codegen.Generators;
using Foundation.Tools.Codegen.Structures;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Caching.Memory;

namespace Foundation.Tools.Codegen.Services;

public class CodegenService
{
    private const string GeneratedFolderName = "fgen_generated";

    private List<Type> GeneratorTypes { get; } = new();

    public CodegenService()
    {
        RegisterGenerators();
    }

    private void RegisterGenerators()
    {
        var baseGeneratorType = typeof(Generator);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(baseGeneratorType));

        foreach (var t in types)
            GeneratorTypes.Add(t);
    }

    public static SyntaxTree ParseIntoSyntaxTree(string source)
    {
        return CSharpSyntaxTree.ParseText(source);
    }

    public static Dictionary<Guid, SyntaxTree> GetAllSyntaxTreesFromProject(Project project)
    {
        var trees = new Dictionary<Guid, SyntaxTree>();
        foreach (var file in project.Files)
        {
            var tree = ParseIntoSyntaxTree(file.Value.Content);
            trees.Add(file.Key, tree);
        }

        return trees;
    }

    /// <summary>
    ///   Generates code for a project.
    ///   Runs all generators on all files in the project.
    ///   Then saves the generated code to the project's obj/foundation_generated folder.
    /// </summary>
    /// <param name="project"></param>
    public void GenerateForProject(Project project)
    {
        var compilation = CSharpCompilation.Create(project.Name)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        Dictionary<Guid, SyntaxTree> trees = GetAllSyntaxTreesFromProject(project);

        foreach (var tree in trees)
            compilation = compilation.AddSyntaxTrees(tree.Value);

        // Each tree is a file in the project.
        foreach (var tree in trees)
        {
            // Get the source code for the file.
            // This will be iterated by every generator before being saved.
            var latestSource = project.Files[tree.Key].Content;
            var sourceTree = tree.Value;
            var expectedFilename = Path.GetFileNameWithoutExtension(project.Files[tree.Key].Name);

            var file = project.Files[tree.Key];

            // Get generation options for each class inside file.
            // These are only extracted for classes that have the generate comment.
            var generationOptions = ParseGenerationComments(sourceTree);

            // If no classes from this file have the generate comment, then skip it.
            if (!tree.Value.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Any(c => generationOptions.ContainsKey(c)))
                continue;

            var wasModified = false;
            foreach (var generatorType in GeneratorTypes)
            {
                var generator = (Generator)Activator.CreateInstance(generatorType);
                generator.Setup(cache, compilation, ParseGenerationComments(sourceTree), project, file, sourceTree);
                foreach (var node in sourceTree.GetRoot().DescendantNodes())
                    generator.OnVisitSyntaxNode(node);
                
                var result = generator.Generate();
                if (!result.Success)
                    continue;
                
                wasModified = true;
                Console.WriteLine($"File {file.Name} iterated by generator {generatorType.Name}");

                sourceTree = result.SyntaxTree;
                expectedFilename = result.ExpectedFilename ?? expectedFilename;
                latestSource = result.Source;
            }

            if (!wasModified)
                continue;

            var resultSource = AddAutogeneratedComments(FormatCode(latestSource));

            // Create a new file in the obj/Debug and obj/Release folders for each target framework.
            static string GetPathForTarget(string path, string type, string target)
                => Path.Combine(path, "obj", type, target, GeneratedFolderName);

            var filename = $"{expectedFilename}.g.cs";
            var generatedPath = GetPathForTarget(project.Path, "[Type]", "[TargetFramework]");
            foreach (var target in project.Frameworks)
            {
                var debugPath = GetPathForTarget(project.Path, "Debug", target.Moniker);
                var releasePath = GetPathForTarget(project.Path, "Release", target.Moniker);

                SaveFile(filename, debugPath, resultSource);
                SaveFile(filename, releasePath, resultSource);
            }

            Console.WriteLine($"Created file {filename} in {generatedPath}.");
        }
    }

    /// <summary>
    /// Saves a file to disk.
    /// </summary>
    /// <param name="filename">The name of the file to save.</param>
    /// <param name="path">The path to save the file to.</param>
    /// <param name="content">The content of the file.</param>
    /// <returns>True if the file was saved successfully, false otherwise.</returns>
    /// <remarks>
    /// If the file already exists, it will be overwritten.
    /// </remarks>
    private static bool SaveFile(string filename, string path, string content)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var fullPath = Path.Combine(path, filename);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        try
        {
            File.WriteAllText(fullPath, content);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to save file {path}:\n{e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds autogenerated comments to the top of the source code.
    /// </summary>
    /// <param name="source">The source code to add comments to.</param>
    /// <returns>The source code with comments added.</returns>
    private static string AddAutogeneratedComments(string source)
    {
        var sourceBuilder = new StringBuilder();

        sourceBuilder.AppendLine("// ------------------------------------------------------------------------------");
        sourceBuilder.AppendLine("// <auto-generated>");
        sourceBuilder.AppendLine("// This file was automatically generated by Foundation.");
        sourceBuilder.AppendLine("// It is a compilation of all [Query] classes, related to GraphQL APIs.");
        sourceBuilder.AppendLine("// All methods were modified to include a execution time and database call tracking.");
        sourceBuilder.AppendLine("// [Do not modify this file directly, as it will be overwritten.]");
        sourceBuilder.AppendLine("// [Do not check this file into source control.]");
        sourceBuilder.AppendLine("// [To regenerate this file, run the *fgen* tool.]");
        sourceBuilder.AppendLine("// </auto-generated>");
        sourceBuilder.AppendLine("// ------------------------------------------------------------------------------");
        sourceBuilder.AppendLine();
        sourceBuilder.Append(source);

        return sourceBuilder.ToString();
    }

    /// <summary>
    /// Parses the source code for generation comments.
    /// </summary>
    /// <param name="tree">The syntax tree to parse.</param>
    /// <returns>A dictionary of classes and their generation options.</returns>
    /// <remarks>
    /// Generation comments are comments that start with "// foundation generate ".
    /// They are used to specify which generators should run on a class.
    /// </remarks>
    private static Dictionary<SyntaxNode, string[]> ParseGenerationComments(SyntaxTree tree)
    {
        const string generateComment = "// foundation generate ";

        Dictionary<SyntaxNode, string[]> classComments = new();
        var root = tree.GetRoot();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var @class in classes)
        {
            var trivia = @class.GetLeadingTrivia();
            var comments = trivia.Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));

            string foundGenerateComment = default!;
            try
            {
                foundGenerateComment = comments
                    .Select(c => c.ToFullString())
                    .First(c => c.StartsWith(generateComment));
            }
            catch
            {
                continue;
            }

            var generationOptions = foundGenerateComment[generateComment.Length..]
                .Split(',')
                .Select(s => s.Trim())
                .ToArray();

            classComments.Add(@class, generationOptions);
        }

        return classComments;
    }

    /// <summary>
    /// Formats the code to be more human readable.
    /// </summary>
    /// <param name="code">The code to format.</param>
    /// <param name="cancelToken">The cancellation token.</param>
    /// <returns>The formatted code.</returns>
    /// <remarks>
    /// Even though is code is autogenerated and will only be ran by the compiler,
    /// it's still nice to have it formatted properly. This is especially useful for debugging.
    /// </remarks>
    private static string FormatCode(string code, CancellationToken cancelToken = default)
    {
        return CSharpSyntaxTree.ParseText(code, cancellationToken: cancelToken)
            .GetRoot(cancelToken)
            .NormalizeWhitespace()
            .SyntaxTree
            .GetText(cancelToken)
            .ToString();
    }
}