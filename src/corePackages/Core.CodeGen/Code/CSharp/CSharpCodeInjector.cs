using System.Text.RegularExpressions;

namespace Core.CodeGen.Code.CSharp;

public static class CSharpCodeInjector
{
    public static async Task AddCodeLinesToMethodAsync(string filePath, string methodName, string[] codeLines)
    {
        List<string> fileContent = (await System.IO.File.ReadAllLinesAsync(filePath)).ToList();
        string methodStartRegex =
            @"((public|protected|internal|protected internal|private protected|private)\s+)?(static\s+)?(void|[a-zA-Z]+\s+<.*>|[a-zA-Z]+)\s+\b" +
            methodName + @"\b\s*\((.*)\)";
        const string scopeBlockStartRegex = @"\{";
        const string scopeBlockEndRegex = @"\}";

        int startIndex = -1;
        int curlyBracketCounter = 0;
        int endIndex = -1;
        for (int i = 0; i < fileContent.Count; ++i)
            if (startIndex == -1)
            {
                Match methodStart = Regex.Match(input: fileContent[i], methodStartRegex);
                if (!methodStart.Success) continue;

                startIndex = i;
                if (Regex.Match(input: fileContent[i], scopeBlockStartRegex).Success) ++curlyBracketCounter;
            }
            else
            {
                if (Regex.Match(input: fileContent[i], scopeBlockStartRegex).Success) ++curlyBracketCounter;
                if (Regex.Match(input: fileContent[i], scopeBlockEndRegex).Success) --curlyBracketCounter;
                if (curlyBracketCounter != 0) continue;

                endIndex = i;

                for (int j = endIndex - 1; j > startIndex; --j)
                {
                    if (Regex.Match(input: fileContent[j], scopeBlockEndRegex).Success) break;
                    if (Regex.Match(input: fileContent[j], pattern: @"\)\s+return").Success) break;
                    if (Regex.Match(input: fileContent[j], pattern: @"\s+return").Success &&
                        Regex.Match(input: fileContent[j - 1], pattern: @"(if|else if|else)\s*\(").Success) break;

                    if (Regex.Match(input: fileContent[j], pattern: @"\s+return").Success)
                    {
                        endIndex = j;
                        break;
                    }
                }

                break;
            }

        if (startIndex == -1 || endIndex == -1) throw new Exception($"{methodName} not found in {filePath}.");

        ICollection<string> methodContent = fileContent.Skip(startIndex + 1).Take(endIndex - 1 - startIndex).ToArray();
        int minimumSpaceCountInMethod;
        if (methodContent.Count < 2)
            minimumSpaceCountInMethod = fileContent[startIndex].TakeWhile(char.IsWhiteSpace).Count() * 2;
        else
            minimumSpaceCountInMethod = methodContent.Where(line => !string.IsNullOrEmpty(line))
                                                     .Min(line => line.TakeWhile(char.IsWhiteSpace).Count());

        fileContent.InsertRange(
            endIndex, collection: codeLines.Select(line => new string(c: ' ', minimumSpaceCountInMethod) + line));
        await System.IO.File.WriteAllLinesAsync(filePath, contents: fileContent.ToArray());
    }
}