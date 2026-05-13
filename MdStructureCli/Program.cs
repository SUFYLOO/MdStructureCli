using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

if (args.Length == 0)
{
    Console.WriteLine("""
Usage:
  mdstruct <structure.md> [output-folder]

Example:
  mdstruct structure.md .
  mdstruct docs/tree.md ./src
""");
    return;
}

var markdownFile = args[0];

var outputRoot = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Directory.GetCurrentDirectory();

if (!File.Exists(markdownFile))
{
    Console.WriteLine($"File not found: {markdownFile}");
    return;
}

var content = File.ReadAllText(markdownFile);
var tree = ExtractTree(content);

Generate(tree, outputRoot);

Console.WriteLine($"Generated successfully in: {outputRoot}");

static string ExtractTree(string markdown)
{
    var codeBlockMatch = Regex.Match(
        markdown,
        @"```(?:txt|text|bash|tree)?\s*(.*?)```",
        RegexOptions.Singleline | RegexOptions.IgnoreCase
    );

    return codeBlockMatch.Success
        ? codeBlockMatch.Groups[1].Value
        : markdown;
}

static void Generate(string asciiTree, string outputRoot)
{
    Directory.CreateDirectory(outputRoot);

    var stack = new Stack<(int Level, string Path)>();
    stack.Push((-1, outputRoot));

    foreach (var rawLine in asciiTree.Split('\n'))
    {
        var line = rawLine.TrimEnd('\r');

        if (string.IsNullOrWhiteSpace(line))
            continue;

        if (!LooksLikeTreeLine(line))
            continue;

        var level = GetLevel(line);
        var name = CleanName(line);

        if (string.IsNullOrWhiteSpace(name))
            continue;

        while (stack.Peek().Level >= level)
            stack.Pop();

        var parent = stack.Peek().Path;
        var fullPath = Path.Combine(parent, name.TrimEnd('/', '\\'));

        if (IsDirectory(name))
        {
            Directory.CreateDirectory(fullPath);
            stack.Push((level, fullPath));
        }
        else
        {
            Directory.CreateDirectory(parent);

            if (!File.Exists(fullPath))
                File.WriteAllText(fullPath, string.Empty);
        }
    }
}

static bool LooksLikeTreeLine(string line)
{
    var trimmed = line.Trim();

    return trimmed.Contains("├──")
        || trimmed.Contains("└──")
        || trimmed.EndsWith("/")
        || trimmed.EndsWith("\\")
        || Path.HasExtension(trimmed);
}

static int GetLevel(string line)
{
    var index = line.IndexOf("├──", StringComparison.Ordinal);

    if (index < 0)
        index = line.IndexOf("└──", StringComparison.Ordinal);

    if (index < 0)
        return 0;

    return index / 4 + 1;
}

static string CleanName(string line)
{
    return line
        .Replace("├──", "")
        .Replace("└──", "")
        .Replace("│", "")
        .Trim();
}

static bool IsDirectory(string name)
{
    name = name.Trim();

    return name.EndsWith("/")
        || name.EndsWith("\\")
        || !Path.HasExtension(name);
}