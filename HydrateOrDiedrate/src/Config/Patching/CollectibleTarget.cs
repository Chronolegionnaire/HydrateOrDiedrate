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

    public CollectibleTarget(ReadOnlySpan<char> code)
    {
        var index = code.IndexOf(':'); //Extract domain
        if (index != -1)
        {
            if(index != 1 || code[0] != '*') //But only if domain is not a wildcard
            {
                domain = code[..index].ToString();
            }
            code = code[(index + 1)..];
        }

        index = code.IndexOf('*'); //Extract until first wildcard
        if(index != -1)
        {
            pathPrefix = code[..index].ToString();
            code = code[index..];
        }
        else
        {
            //No wildcards used
            pathPrefix = code.ToString();
            return;
        }

        if(code.IsEmpty || (code.Length == 1 && code[0] == '*')) return;
        
        //If more remained then we have to create a regex
        pathRemainderRexeg = new Regex($"^{Regex.Escape(code.ToString()).Replace("\\*", ".*")}$", RegexOptions.Compiled);
    }

    public bool IsMatch(CollectibleObject collectible)
    {
        var code = collectible.Code;
        if(code is null || (domain is not null && code.Domain != domain)) return false;
        ReadOnlySpan<char> path = code.Path;

        if(!path.StartsWith(pathPrefix)) return false;
        path = path[pathPrefix.Length..];

        if(pathRemainderRexeg is null) return true;
        
        return pathRemainderRexeg.IsMatch(path);
    }
}
