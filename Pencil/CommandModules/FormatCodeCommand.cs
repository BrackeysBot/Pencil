﻿using Discord;
using Discord.Interactions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Pencil.CommandModules;

internal sealed class FormatCodeCommand : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly string[] SupportedLanguages =
    {
        "actionscript", "angelscript", "arcade", "arduino", "aspectj", "autohotkey", "autoit", "cal", "capnproto", "ceylon",
        "clean", "coffeescript", "cpp", "crystal", "c", "cs", "csharp", "css", "d", "dart", "diff", "dos", "dts", "glsl", "gml",
        "go", "gradle", "groovy", "haxe", "hsp", "http", "java", "js", "json", "kotlin", "leaf", "less", "lisp", "livescript",
        "lsl", "lua", "mathematica", "matlab", "mel", "perl", "n1ql", "nginx", "nix", "objectivec", "openscad", "php",
        "powershell", "processing", "protobuff", "puppet", "qml", "r", "reasonml", "roboconf", "rsl", "rust", "scala", "scss",
        "sql", "stan", "swift", "tcl", "thrift", "typescript", "vala", "zephir"
    };

    [MessageCommand("Format Code")]
    [RequireContext(ContextType.Guild)]
    public async Task FormatCodeAsync(IMessage message)
    {
        string code = message.Content;
        string firstWord = code.Split(' ')[0];
        if (TryDetectLanguage(firstWord, out string language))
        {
            // Length + 1 for removing the whitespace as well, faster than calling .Trim()
            code = code[(language.Length + 1)..];
        }

        string trimmedCode = RemoveEmptyMethods(RemoveBackticks(code));
        SyntaxTree tree = CSharpSyntaxTree.ParseText(trimmedCode);
        SyntaxNode node = (await tree.GetRootAsync()).NormalizeWhitespace();
        string formattedCode = node.ToFullString();

        await RespondAsync($"```{language}\n{formattedCode}\n```", ephemeral: true).ConfigureAwait(false);
    }

    private static bool TryDetectLanguage(string input, out string language)
    {
        if (SupportedLanguages.Contains(input))
        {
            language = input;
            return true;
        }

        language = "cs";
        return false;
    }

    private static string RemoveBackticks(string input)
    {
        if (!input.StartsWith("```"))
            return input;

        input = input.Remove(0, 3);

        if (!TryDetectLanguage(input.Split('\n')[0], out string language))
            return input;

        input = input.Remove(0, language.Length);
        char[] charArray = input.ToCharArray();
        for (var charIndex = 0; charIndex < charArray.Length; charIndex++)
        {
            if (charArray[charIndex] == '`')
                charArray[charIndex] = ' ';
        }

        return new string(charArray);
    }

    private static string RemoveEmptyMethods(string remainingText)
    {
        List<string> codeLines = remainingText.Split('\n').ToList();

        var isChecking = false;
        var startIndex = 0;
        int startComment = -1;
        int updateComment = -1;

        for (var lineIndex = 0; lineIndex < codeLines.Count; lineIndex++)
        {
            if (codeLines[lineIndex].Contains("// Start is called before the first frame update"))
            {
                startComment = lineIndex;
            }
            else if (codeLines[lineIndex].Contains("// Update is called once per frame"))
            {
                updateComment = lineIndex;
            }

            if (codeLines[lineIndex].Contains("void"))
            {
                isChecking = true;
                startIndex = lineIndex;
            }

            else if (isChecking && codeLines[lineIndex].Contains("}"))
            {
                isChecking = false;

                if (startComment != -1)
                {
                    codeLines[startComment] = codeLines[startComment]
                        .Replace("// Start is called before the first frame update", string.Empty);
                }

                if (updateComment != -1)
                {
                    codeLines[updateComment] =
                        codeLines[updateComment].Replace("// Update is called once per frame", string.Empty);
                }

                for (int i = startIndex; i < lineIndex + 1; i++)
                {
                    codeLines.RemoveAt(startIndex);
                }

                updateComment = -1;
                startComment = -1;
                startIndex = 0;
                lineIndex = 0;
            }
            else if (isChecking && !string.IsNullOrWhiteSpace(codeLines[lineIndex]) && !codeLines[lineIndex].Contains("{"))
            {
                isChecking = false;
                updateComment = -1;
                startComment = -1;
                startIndex = 0;
            }
        }

        return string.Join('\n', codeLines);
    }
}
