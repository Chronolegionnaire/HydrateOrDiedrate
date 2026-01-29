using HarmonyLib;
using System;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config.Patching;

public class CollectibleTarget
{
    private readonly string domain;
    private readonly string pathPrefix;
    private readonly Regex pathRemainderRexeg;
    private readonly bool exactMatch;

    public CollectibleTarget(ReadOnlySpan<char> code)
    {
        var index = code.IndexOf(':');
        if (index != -1)
        {
            if (!(index == 1 && code[0] == '*'))
            {
                domain = code[..index].ToString();
            }
            code = code[(index + 1)..];
        }

        index = code.IndexOf('*');
        if (index != -1)
        {
            pathPrefix = code[..index].ToString();
            code = code[index..];

            if (code.IsEmpty || (code.Length == 1 && code[0] == '*')) return;

            pathRemainderRexeg = new Regex(
                $"^{Regex.Escape(code.ToString()).Replace("\\*", ".*")}$",
                RegexOptions.Compiled
            );
        }
        else
        {
            // No wildcards used => EXACT match
            exactMatch = true;
            pathPrefix = code.ToString();
        }
    }

    public bool IsMatch(CollectibleObject collectible)
    {
        var code = collectible.Code;
        if (code is null || (domain is not null && code.Domain != domain)) return false;

        ReadOnlySpan<char> path = code.Path;

        if (exactMatch)
        {
            // must be identical, not "starts with"
            return path.SequenceEqual(pathPrefix);
        }

        if (!path.StartsWith(pathPrefix)) return false;
        path = path[pathPrefix.Length..];

        return pathRemainderRexeg is null || pathRemainderRexeg.IsMatch(path);
    }
}