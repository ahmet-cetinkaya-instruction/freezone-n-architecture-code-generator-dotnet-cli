﻿using System.Text.RegularExpressions;

namespace Core.CodeGen.Code.CSharp;

public static class CSharpCodeInjector
{
    public static async Task AddCodeLinesToMethodAsync(string filePath, string methodName, string[] codeLines)
    {
        List<string> fileContent = (await System.IO.File.ReadAllLinesAsync(filePath)).ToList();
        string methodStartRegex =
            @"((public|protected|internal|protected internal|private protected|private)\s+)?(static\s+)?(void|[a-zA-Z]+\s+<.*>|[a-zA-Z]+)\s+\b" +
            methodName + @"\b\s*\(";
        const string scopeBlockStartRegex = @"\{";
        const string scopeBlockEndRegex = @"\}";

        int methodStartIndex = -1;
        int methodEndIndex = -1;
        int curlyBracketCountInMethod = 1;
        for (int i = 0; i < fileContent.Count; ++i)
        {
            Match methodStart = Regex.Match(input: fileContent[i], methodStartRegex);
            if (!methodStart.Success) continue;

            methodStartIndex = i;
            if (!Regex.Match(input: fileContent[i], pattern: @"\{").Success)
                for (int j = methodStartIndex + 1; j < fileContent.Count; ++j)
                {
                    if (!Regex.Match(input: fileContent[j], pattern: @"\{").Success) continue;
                    methodStartIndex = j;
                    break;
                }
        }

        for (int i = methodStartIndex + 1; i < fileContent.Count; ++i)
        {
            if (Regex.Match(input: fileContent[i], scopeBlockStartRegex).Success) ++curlyBracketCountInMethod;
            if (Regex.Match(input: fileContent[i], scopeBlockEndRegex).Success) --curlyBracketCountInMethod;
            if (curlyBracketCountInMethod != 0) continue;

            methodEndIndex = i;

            for (int j = methodEndIndex - 1; j > methodStartIndex; --j)
            {
                if (Regex.Match(input: fileContent[j], scopeBlockEndRegex).Success) break;
                if (Regex.Match(input: fileContent[j], pattern: @"\)\s+return").Success) break;
                if (Regex.Match(input: fileContent[j], pattern: @"\s+return").Success &&
                    Regex.Match(input: fileContent[j - 1], pattern: @"(if|else if|else)\s*\(").Success) break;

                if (Regex.Match(input: fileContent[j], pattern: @"\s+return").Success)
                {
                    methodEndIndex = j;
                    break;
                }
            }

            break;
        }

        if (methodStartIndex == -1 || methodEndIndex == -1)
            throw new Exception($"{methodName} not found in {filePath}.");

        ICollection<string> methodContent =
            fileContent.Skip(methodStartIndex + 1).Take(methodEndIndex - 1 - methodStartIndex).ToArray();
        int minimumSpaceCountInMethod;
        if (methodContent.Count < 2)
            minimumSpaceCountInMethod = fileContent[methodStartIndex].TakeWhile(char.IsWhiteSpace).Count() * 2;
        else
            minimumSpaceCountInMethod = methodContent.Where(line => !string.IsNullOrEmpty(line))
                                                     .Min(line => line.TakeWhile(char.IsWhiteSpace).Count());

        fileContent.InsertRange(
            methodEndIndex, collection: codeLines.Select(line => new string(c: ' ', minimumSpaceCountInMethod) + line));
        await System.IO.File.WriteAllLinesAsync(filePath, contents: fileContent.ToArray());
    }

    public static async Task AddCodeLinesAsPropertyAsync(string filePath, string[] codeLines)
    {
        string[] fileContent = await System.IO.File.ReadAllLinesAsync(filePath);
        const string propertyStartRegex =
            @"(public|protected|internal|protected internal|private protected|private)?\s+(const|static)?\s*(\w+)\s+(\w+)\s*\{[^}]+\}";

        int indexToAdd = -1;
        for (int i = 0; i < fileContent.Length; ++i)
        {
            Match propertyStart = Regex.Match(input: fileContent[i], propertyStartRegex);
            if (propertyStart.Success) indexToAdd = i;
        }

        int propertySpaceCountInClass;
        if (indexToAdd == -1)
        {
            const string classRegex = @"class\s+(\w+)";

            for (int i = 0; i < fileContent.Length; ++i)
            {
                Match propertyStart = Regex.Match(input: fileContent[i], classRegex);
                if (propertyStart.Success) indexToAdd = i;
            }

            propertySpaceCountInClass = fileContent[indexToAdd].TakeWhile(char.IsWhiteSpace).Count() * 2;
        }
        else
        {
            propertySpaceCountInClass = fileContent[indexToAdd].TakeWhile(char.IsWhiteSpace).Count();
        }

        List<string> updatedFileContent = new(fileContent);
        updatedFileContent.InsertRange(index: indexToAdd + 1, collection: codeLines.Select(line =>
                                           new string(c: ' ', propertySpaceCountInClass) + line));

        await System.IO.File.WriteAllLinesAsync(filePath, contents: updatedFileContent.ToArray());
    }

    public static async Task AddCodeLinesToRegionAsync(string filePath, IEnumerable<string> linesToAdd, string regionName)
    {
        List<string> fileContent = (await System.IO.File.ReadAllLinesAsync(filePath)).ToList();
        string regionStartRegex = @$"^\s*#region\s*{regionName}\s*";
        const string regionEndRegex = @"^\s*#endregion\s*";

        bool isInRegion = false;
        int indexToAdd;
        for (indexToAdd = 0; indexToAdd < fileContent.Count; indexToAdd++)
        {
            string fileLine = fileContent[indexToAdd];

            if (Regex.Match(fileLine, regionStartRegex).Success)
            {
                isInRegion = true;
                continue;
            }

            if (!isInRegion) continue;
            if (!Regex.Match(fileLine, regionEndRegex).Success) continue;

            string previousLine = fileContent[index: indexToAdd - 1];
            if (Regex.Match(previousLine, regionStartRegex).Success)
            {
                fileContent.Insert(indexToAdd, string.Empty);
                indexToAdd += 2;
            }

            if (!string.IsNullOrEmpty(previousLine)) fileContent.Insert(index: indexToAdd - 1, string.Empty);

            int minimumSpaceCountInRegion = fileContent[indexToAdd].TakeWhile(char.IsWhiteSpace).Count();

            fileContent.InsertRange(index: indexToAdd - 1, linesToAdd.Select(line =>
                                                        new string(c: ' ', minimumSpaceCountInRegion) + line));
            await System.IO.File.WriteAllLinesAsync(filePath, contents: fileContent);
            break;
        }
    }
}