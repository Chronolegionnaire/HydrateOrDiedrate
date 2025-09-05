using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Recipes
{
    public static class WaterVariantRecipeGenerator
    {
        private static readonly AssetLocation PlainWater = new AssetLocation("game", "waterportion");
        private static readonly AssetLocation SaltWater  = new AssetLocation("game", "saltwaterportion");

        private static readonly AssetLocation[] FreshAlts =
        {
            new AssetLocation("hydrateordiedrate", "boiledwaterportion"),
            new AssetLocation("hydrateordiedrate", "boiledrainwaterportion"),
            new AssetLocation("hydrateordiedrate", "distilledwaterportion"),
            new AssetLocation("hydrateordiedrate", "rainwaterportion"),
            new AssetLocation("hydrateordiedrate", "wellwaterportion-fresh"),
        };

        private static readonly AssetLocation[] SaltAlts =
        {
            new AssetLocation("hydrateordiedrate", "wellwaterportion-salt"),
        };
        private static readonly string FromPlainFull = PlainWater.ToString();
        private static readonly string FromSaltFull  = SaltWater.ToString();
        private static readonly string[] FreshAltsFull = FreshAlts.Select(al => al.ToString()).ToArray();
        private static readonly string[] SaltAltsFull  = SaltAlts.Select(al => al.ToString()).ToArray();

        public static void Generate(ICoreAPI api)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var candidates =
                new List<(object host, IList list, Type elemType, string hostMemberName, MethodInfo addMethod)>();

            candidates.AddRange(DiscoverRecipeListsOnHost(api.World, "IWorldAccessor", flags));
            foreach (var ms in api.ModLoader.Systems)
                candidates.AddRange(DiscoverRecipeListsOnHost(ms, ms.GetType().Name, flags));

            int totalLists = 0, totalRecipes = 0, totalAdded = 0;

            foreach (var (host, list, elemType, hostName, addMethod) in candidates)
            {
                totalLists++;
                totalRecipes += list.Count;

                int addedHere = 0;

                if (typeof(GridRecipe).IsAssignableFrom(elemType))
                {
                    addedHere = HandleGridRecipes(api, list);
                }
                else if (typeof(CookingRecipe).IsAssignableFrom(elemType))
                {
                    addedHere = HandleCookingRecipes(api, list);
                }
                else if (typeof(BarrelRecipe).IsAssignableFrom(elemType))
                {
                    addedHere = HandleBarrelRecipes(api, list);
                }
                else if (elemType.Namespace == "ACulinaryArtillery" && elemType.Name == "DoughRecipe")
                {
                    addedHere = HandleAcaDoughRecipes(api, list);
                }
                else if (elemType.Namespace == "ACulinaryArtillery" && elemType.Name == "SimmerRecipe")
                {
                    addedHere = HandleAcaSimmerRecipes(api, list);
                }
                else if ((elemType.Namespace?.StartsWith("MakeTea") == true) && elemType.Name == "TeapotRecipe")
                {
                    addedHere = HandleTeapotRecipes(api, list, addMethod, host);
                }

                totalAdded += addedHere;
            }
        }
        private static IEnumerable<(object host, IList list, Type elemType, string hostMemberName, MethodInfo addMethod)>
            DiscoverRecipeListsOnHost(object host, string hostLabel, BindingFlags flags)
        {
            var results = new List<(object, IList, Type, string, MethodInfo)>();

            (bool ok, IList list, Type elemType, MethodInfo add) TryUnwrapRegistry(object obj)
            {
                if (obj == null) return (false, null, null, null);
                var recProp = obj.GetType().GetProperty("Recipes", flags);
                if (recProp?.CanRead != true) return (false, null, null, null);

                var value = recProp.GetValue(obj);
                if (value is not IList ilist) return (false, null, null, null);

                var elemType = GetElementType(ilist);
                if (elemType?.Name?.Contains("Recipe", StringComparison.OrdinalIgnoreCase) != true)
                    return (false, null, null, null);

                var addMethod = obj.GetType()
                    .GetMethods(flags)
                    .FirstOrDefault(mi =>
                    {
                        if (mi.Name != "Add") return false;
                        var pars = mi.GetParameters();
                        return pars.Length == 1 && (elemType?.IsAssignableFrom(pars[0].ParameterType) ?? false);
                    });

                return (true, ilist, elemType, addMethod);
            }

            foreach (var m in host.GetType().GetMembers(flags))
            {
                Type memberType = null;
                Func<object> getter = null;
                string memberName = m.Name;

                switch (m)
                {
                    case FieldInfo fi:
                        memberType = fi.FieldType; getter = () => fi.GetValue(host); break;
                    case PropertyInfo pi:
                        if (!pi.CanRead) continue;
                        memberType = pi.PropertyType;
                        getter = () => { try { return pi.GetValue(host); } catch { return null; } };
                        break;
                    default: continue;
                }

                if (memberType == null) continue;
                var obj = getter?.Invoke();

                if (obj is IList ilistA)
                {
                    var elemType = GetElementType(ilistA);
                    if (elemType?.Name?.Contains("Recipe", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var addOnHost = host.GetType().GetMethods(flags).FirstOrDefault(mi =>
                        {
                            if (mi.Name != "Add") return false;
                            var pars = mi.GetParameters();
                            return pars.Length == 1 && pars[0].ParameterType == elemType;
                        });

                        results.Add((host, ilistA, elemType, $"{hostLabel}.{memberName}", addOnHost));
                        continue;
                    }
                }

                var reg = obj;
                var (ok, list, elemType2, addOnRegistry) = TryUnwrapRegistry(reg);
                if (ok) results.Add((reg, list, elemType2, $"{hostLabel}.{memberName}.Recipes", addOnRegistry));
            }

            foreach (var mi in host.GetType().GetMethods(flags))
            {
                if (mi.GetParameters().Length != 0) continue;
                if (!typeof(IList).IsAssignableFrom(mi.ReturnType)) continue;

                try
                {
                    var ret = mi.Invoke(host, null) as IList;
                    if (ret == null) continue;

                    var elemType = GetElementType(ret);
                    if (elemType?.Name?.Contains("Recipe", StringComparison.OrdinalIgnoreCase) != true) continue;

                    var addOnHost = host.GetType().GetMethods(flags).FirstOrDefault(m2 =>
                    {
                        if (m2.Name != "Add") return false;
                        var pars = m2.GetParameters();
                        return pars.Length == 1 && pars[0].ParameterType == elemType;
                    });

                    results.Add((host, ret, elemType, $"{hostLabel}.{mi.Name}()", addOnHost));
                }
                catch { /* ignore */ }
            }

            return results;
        }

        private static Type GetElementType(IList list)
        {
            var lt = list.GetType();
            if (lt.IsArray) return lt.GetElementType();
            return lt.IsGenericType ? lt.GetGenericArguments().FirstOrDefault() : typeof(object);
        }

        private static RecipeRegistrySystem TryGetRegistry(ICoreAPI api)
        {
            try { return api.ModLoader.GetModSystem<RecipeRegistrySystem>(); }
            catch { return null; }
        }
        private static string NormDom(string dom) => string.IsNullOrEmpty(dom) ? "game" : dom;

        private static bool AssetLocEquals(AssetLocation a, AssetLocation b)
        {
            if (a == null || b == null) return false;
            return NormDom(a.Domain).Equals(NormDom(b.Domain), StringComparison.OrdinalIgnoreCase)
                && a.Path.Equals(b.Path, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesStringOrPath(string codeStr, AssetLocation want)
        {
            if (string.IsNullOrWhiteSpace(codeStr)) return false;
            if (codeStr.Contains(":"))
            {
                var parts = codeStr.Split(':');
                if (parts.Length != 2) return false;
                return parts[0].Equals(NormDom(want.Domain), StringComparison.OrdinalIgnoreCase)
                    && parts[1].Equals(want.Path, StringComparison.OrdinalIgnoreCase);
            }
            return NormDom(want.Domain).Equals("game", StringComparison.OrdinalIgnoreCase)
                && codeStr.Equals(want.Path, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSuffix(AssetLocation to) => $"-hod-{to.Domain}-{to.Path}";

        private static void BumpNameAndCodeWithSuffix(object recipe, string defaultPrefix, AssetLocation to)
        {
            var suffix = BuildSuffix(to);
            var codeStr = ReflectionHelper.Get(recipe, "Code") as string;
            if (codeStr != null) ReflectionHelper.Set(recipe, "Code", codeStr + suffix);
            if (ReflectionHelper.Get(recipe, "Name") is AssetLocation nameAL)
            {
                var newAL = new AssetLocation(nameAL.Domain, (nameAL.Path ?? defaultPrefix) + suffix);
                ReflectionHelper.Set(recipe, "Name", newAL);
            }
        }
        private static bool IngMatches(object ing, AssetLocation target)
        {
            if (ing == null) return false;
            if (ReflectionHelper.Get(ing, "Code") is AssetLocation al && AssetLocEquals(al, target)) return true;
            if (ReflectionHelper.Get(ing, "Code") is string s && MatchesStringOrPath(s, target)) return true;
            var rs = ReflectionHelper.Get(ing, "ResolvedItemstack");
            var coll = ReflectionHelper.Get(rs, "Collectible");
            var cAL = ReflectionHelper.Get(coll, "Code") as AssetLocation;
            if (AssetLocEquals(cAL, target)) return true;

            return false;
        }
        private static bool TrySwapIng(object ing, AssetLocation from, AssetLocation to)
        {
            if (ing == null) return false;
            if (!IngMatches(ing, from)) return false;

            ReflectionHelper.Set(ing, "Code", to.Clone());
            ReflectionHelper.Set(ing, "ResolvedItemstack", null);
            ReflectionHelper.SetNumberIfExists(ing, "ConsumeQuantity", 0);
            ReflectionHelper.SetNumberIfExists(ing, "Quantity", 0);
            return true;
        }

        private static void ResolveRecipe(ICoreAPI api, object recipe, string reason)
        {
            if (ReflectionHelper.TryInvoke(recipe, "Resolve", api.World as IServerWorldAccessor ?? api.World as IWorldAccessor, reason)) return;
            if (ReflectionHelper.TryInvoke(recipe, "ResolveIngredients", api.World)) return;
        }

        private static int HandleBarrelRecipes(ICoreAPI api, IList list)
        {
            int added = 0;
            var originals = list.Cast<BarrelRecipe>().ToList();

            foreach (var br in originals)
            {
                if (!BarrelHas(br, PlainWater) && !BarrelHas(br, SaltWater)) continue;

                foreach (var (from, repls) in new[] { (PlainWaterAL: PlainWater, FreshAltsAL: FreshAlts), (SaltWaterAL: SaltWater, SaltAltsAL: SaltAlts) })
                {
                    if (!BarrelHas(br, from)) continue;

                    foreach (var to in repls)
                    {
                        var clone = br.Clone();
                        bool changed = false;

                        foreach (var ing in clone.Ingredients ?? Array.Empty<BarrelRecipeIngredient>())
                            changed |= TrySwapIng(ing, from, to);

                        if (!changed) continue;

                        var suffix = BuildSuffix(to);
                        if (clone.Name != null)
                            clone.Name = new AssetLocation(clone.Name.Domain, (clone.Name.Path ?? "barrelrecipe") + suffix);
                        clone.Code = (clone.Code ?? "barrelrecipe") + suffix;

                        try { clone.Resolve(api.World, "[HoD Barrel clone]"); } catch { }
                        list.Add(clone);
                        added++;
                    }
                }
            }

            return added;
        }

        private static bool BarrelHas(BarrelRecipe br, AssetLocation target)
        {
            if (br?.Ingredients == null) return false;
            foreach (var ing in br.Ingredients) if (IngMatches(ing, target)) return true;
            return false;
        }
        private static int HandleGridRecipes(ICoreAPI api, IList list)
        {
            int added = 0;
            var originals = list.Cast<GridRecipe>().ToList();

            foreach (var gr in originals)
            {
                if (!GridRecipeHas(gr, PlainWater) && !GridRecipeHas(gr, SaltWater)) continue;

                foreach (var (from, repls) in new[] { (PlainWaterAL: PlainWater, FreshAltsAL: FreshAlts), (SaltWaterAL: SaltWater, SaltAltsAL: SaltAlts) })
                {
                    if (!GridRecipeHas(gr, from)) continue;

                    foreach (var to in repls)
                    {
                        var clone = gr.Clone();
                        bool changed = false;
                        if (clone.Ingredients != null)
                            foreach (var kv in clone.Ingredients.ToList())
                                changed |= TrySwapIng(kv.Value, from, to);

                        if (clone.resolvedIngredients != null)
                        {
                            foreach (var ig in clone.resolvedIngredients)
                                changed |= TrySwapIng(ig, from, to);

                            try { clone.ResolveIngredients(api.World); } catch { }
                        }
                        var attr = clone.Attributes;
                        if (attr != null && attr["liquidContainerProps"].Exists && attr["liquidContainerProps"]["requiresContent"].Exists)
                        {
                            var codeStr = attr["liquidContainerProps"]["requiresContent"]["code"].AsString(null);
                            if (!string.IsNullOrEmpty(codeStr) && (MatchesStringOrPath(codeStr, from) || codeStr.Equals(from.ToString(), StringComparison.OrdinalIgnoreCase)))
                            {
                                var tok = attr.Token?.SelectToken("$.liquidContainerProps.requiresContent.code");
                                tok?.Replace(to.ToString());
                                changed = true;
                            }
                        }

                        if (!changed) continue;

                        var baseName = clone.Name?.ToString() ?? clone.Output?.Code?.ToShortString() ?? "hodrecipe";
                        clone.Name = new AssetLocation(baseName + BuildSuffix(to));

                        list.Add(clone);
                        added++;
                    }
                }
            }

            return added;
        }

        private static bool GridRecipeHas(GridRecipe gr, AssetLocation target)
        {
            if (gr == null) return false;

            if (gr.resolvedIngredients != null)
                foreach (var ig in gr.resolvedIngredients)
                    if (IngMatches(ig, target)) return true;

            if (gr.Ingredients != null)
                foreach (var ing in gr.Ingredients.Values)
                    if (IngMatches(ing, target)) return true;

            var attr = gr.Attributes;
            if (attr != null && attr["liquidContainerProps"].Exists && attr["liquidContainerProps"]["requiresContent"].Exists)
            {
                var reqCode = attr["liquidContainerProps"]["requiresContent"]["code"].AsString(null);
                if (!string.IsNullOrEmpty(reqCode) && (MatchesStringOrPath(reqCode, target) || reqCode.Equals(target.ToString(), StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        private static int HandleCookingRecipes(ICoreAPI api, IList list)
        {
            int added = 0;

            foreach (var cr in list.Cast<CookingRecipe>())
            {
                if (!CookingHas(cr, PlainWater) && !CookingHas(cr, SaltWater)) continue;

                bool changedAny = false;

                foreach (var ing in cr.Ingredients ?? Array.Empty<CookingRecipeIngredient>())
                {
                    if (ing == null) continue;

                    bool isFreshWaterSlot = ing.ValidStacks != null &&
                                            ing.ValidStacks.Any(v => AssetLocEquals(v?.Code, PlainWater));
                    bool isSaltWaterSlot = ing.ValidStacks != null &&
                                           ing.ValidStacks.Any(v => AssetLocEquals(v?.Code, SaltWater));

                    string[] addList = null;
                    if (isFreshWaterSlot) addList = BuildFreshUnion();
                    else if (isSaltWaterSlot) addList = BuildSaltUnion();
                    else continue;

                    var existing = ing.ValidStacks?
                                       .Where(v => v?.Code != null)
                                       .Select(v => v.Code.ToString())
                                       .ToHashSet(StringComparer.OrdinalIgnoreCase)
                                   ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var s in addList) existing.Add(s);
                    var merged = existing
                        .Select(s => new CookingRecipeStack
                        {
                            Code = new AssetLocation(s),
                            Type = EnumItemClass.Item,
                            ResolvedItemstack = null
                        })
                        .ToArray();
                    ing.ValidStacks = merged;
                    changedAny = true;
                }

                if (changedAny)
                {
                    try
                    {
                        cr.Resolve(api.World as IServerWorldAccessor, "[HoD add cooking recipe]");
                    }
                    catch
                    {
                    }

                    added++;
                }
            }

            return added;
            string[] BuildFreshUnion()
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { FromPlainFull };
                foreach (var s in FreshAltsFull) set.Add(s);
                return set.ToArray();
            }

            string[] BuildSaltUnion()
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { FromSaltFull };
                foreach (var s in SaltAltsFull) set.Add(s);
                return set.ToArray();
            }
        }
        private static bool CookingHas(CookingRecipe cr, AssetLocation target)
        {
            if (cr?.Ingredients == null) return false;

            foreach (var ing in cr.Ingredients)
            {
                if (ing == null) continue;

                if (!string.IsNullOrEmpty(ing.Code) && (MatchesStringOrPath(ing.Code, target) || ing.Code.Equals(target.ToString(), StringComparison.OrdinalIgnoreCase)))
                    return true;

                var vst = ing.ValidStacks;
                if (vst != null)
                    for (int i = 0; i < vst.Length; i++)
                        if (AssetLocEquals(vst[i]?.Code, target))
                            return true;
            }

            return false;
        }
        private static int HandleAcaDoughRecipes(ICoreAPI api, IList list)
        {
            int added = 0;
            var originals = list.Cast<object>().ToList();

            foreach (var src in originals)
            {
                var ingredientsArr = ReflectionHelper.Get(src, "Ingredients") as Array;
                if (ingredientsArr == null) continue;

                bool usesPlain = AcaDoughHas(ingredientsArr, PlainWater);
                bool usesSalt  = AcaDoughHas(ingredientsArr, SaltWater);
                if (!usesPlain && !usesSalt) continue;

                foreach (var (from, repls) in new[] { (PlainWaterAL: PlainWater, FreshAltsAL: FreshAlts), (SaltWaterAL: SaltWater, SaltAltsAL: SaltAlts) })
                {
                    if ((from == PlainWater && !usesPlain) || (from == SaltWater && !usesSalt)) continue;

                    foreach (var to in repls)
                    {
                        var clone = ReflectionHelper.Invoke(src, "Clone");
                        if (clone == null) continue;

                        bool changed = false;
                        var arr = ReflectionHelper.Get(clone, "Ingredients") as Array;
                        if (arr != null)
                        {
                            foreach (var doughIng in arr.Cast<object>())
                            {
                                var inputs = ReflectionHelper.Get(doughIng, "Inputs") as Array;
                                if (inputs == null) continue;
                                for (int i = 0; i < inputs.Length; i++)
                                    changed |= TrySwapIng(inputs.GetValue(i), from, to);
                            }
                        }

                        if (!changed) continue;

                        ResolveRecipe(api, clone, "[HoD ACA Dough clone]");
                        BumpNameAndCodeWithSuffix(clone, "acadough", to);
                        list.Add(clone);
                        added++;
                    }
                }
            }

            return added;
        }

        private static bool AcaDoughHas(Array doughIngredientsArr, AssetLocation target)
        {
            foreach (var doughIng in doughIngredientsArr.Cast<object>())
            {
                var inputs = ReflectionHelper.Get(doughIng, "Inputs") as Array;
                if (inputs == null) continue;
                foreach (var ing in inputs.Cast<object>())
                    if (IngMatches(ing, target)) return true;
            }
            return false;
        }

        private static int HandleAcaSimmerRecipes(ICoreAPI api, IList list)
        {
            int added = 0;
            var originals = list.Cast<object>().ToList();

            foreach (var src in originals)
            {
                var ingredientsArr = ReflectionHelper.Get(src, "Ingredients") as Array;
                if (ingredientsArr == null || ingredientsArr.Length == 0) continue;

                bool usesPlain = ingredientsArr.Cast<object>().Any(o => IngMatches(o, PlainWater));
                bool usesSalt  = ingredientsArr.Cast<object>().Any(o => IngMatches(o, SaltWater));
                if (!usesPlain && !usesSalt) continue;

                foreach (var (from, repls) in new[] { (PlainWaterAL: PlainWater, FreshAltsAL: FreshAlts), (SaltWaterAL: SaltWater, SaltAltsAL: SaltAlts) })
                {
                    if ((from == PlainWater && !usesPlain) || (from == SaltWater && !usesSalt)) continue;

                    foreach (var to in repls)
                    {
                        var clone = ReflectionHelper.Invoke(src, "Clone");
                        if (clone == null) continue;

                        bool changed = false;
                        var arr = ReflectionHelper.Get(clone, "Ingredients") as Array;
                        if (arr != null)
                            for (int i = 0; i < arr.Length; i++)
                                changed |= TrySwapIng(arr.GetValue(i), from, to);

                        if (!changed) continue;

                        ResolveRecipe(api, clone, "[HoD ACA Simmer clone]");
                        BumpNameAndCodeWithSuffix(clone, "acasimmer", to);
                        list.Add(clone);
                        added++;
                    }
                }
            }
            return added;
        }

        private static int HandleTeapotRecipes(ICoreAPI api, IList list, MethodInfo addMethod, object host)
        {
            int added = 0;
            var originals = list.Cast<object>().ToList();

            foreach (var src in originals)
            {
                var ings = ReflectionHelper.Get(src, "Ingredients") as Array;
                if (ings == null || ings.Length == 0) continue;

                bool usesPlain = ings.Cast<object>().Any(o => IngMatches(o, PlainWater));
                bool usesSalt = ings.Cast<object>().Any(o => IngMatches(o, SaltWater));
                if (!usesPlain && !usesSalt) continue;

                foreach (var (from, repls) in new[]
                         {
                             (PlainWaterAL: PlainWater, FreshAltsAL: FreshAlts),
                             (SaltWaterAL: SaltWater, SaltAltsAL: SaltAlts)
                         })
                {
                    if ((from == PlainWater && !usesPlain) || (from == SaltWater && !usesSalt)) continue;

                    foreach (var to in repls)
                    {
                        var clone = ReflectionHelper.Invoke(src, "Clone");
                        if (clone == null) continue;

                        var srcArr = ReflectionHelper.Get(clone, "Ingredients") as Array;
                        if (srcArr == null) continue;

                        var elemType = srcArr.GetType().GetElementType();
                        var newArr = Array.CreateInstance(elemType, srcArr.Length);

                        bool changed = false;
                        for (int i = 0; i < srcArr.Length; i++)
                        {
                            var ing = srcArr.GetValue(i);
                            var ingCopy = ReflectionHelper.ShallowClone(ing);
                            if (TrySwapIng(ingCopy, from, to)) changed = true;
                            newArr.SetValue(ingCopy, i);
                        }

                        ReflectionHelper.Set(clone, "Ingredients", newArr);

                        if (!changed) continue;

                        var outObj = ReflectionHelper.Get(clone, "Output");
                        var outClone = ReflectionHelper.Invoke(outObj, "Clone");
                        if (outClone != null) ReflectionHelper.Set(clone, "Output", outClone);

                        BumpNameAndCodeWithSuffix(clone, "teapot", to);
                        ResolveRecipe(api, clone, "[HoD Teapot clone]");

                        if (addMethod != null) addMethod.Invoke(host, new[] { clone });
                        else list.Add(clone);

                        added++;
                    }
                }
            }
            return added;
        }
        private static class ReflectionHelper
        {
            private const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            public static object Get(object obj, string name)
            {
                if (obj == null || string.IsNullOrEmpty(name)) return null;
                var t = obj.GetType();

                var pi = t.GetProperty(name, F);
                if (pi != null && pi.CanRead) { try { return pi.GetValue(obj); } catch { } }

                var fi = t.GetField(name, F);
                if (fi != null) { try { return fi.GetValue(obj); } catch { } }

                return null;
            }

            public static bool Set(object obj, string name, object value)
            {
                if (obj == null || string.IsNullOrEmpty(name)) return false;
                var t = obj.GetType();

                var pi = t.GetProperty(name, F);
                if (pi != null && pi.CanWrite)
                { try { pi.SetValue(obj, value); return true; } catch { } }

                var fi = t.GetField(name, F);
                if (fi != null && !fi.IsInitOnly)
                { try { fi.SetValue(obj, value); return true; } catch { } }

                return false;
            }

            public static object Invoke(object obj, string name, params object[] args)
            {
                if (obj == null) return null;

                var argTypes = args?.Select(a => a?.GetType() ?? typeof(object)).ToArray() ?? Type.EmptyTypes;
                var t = obj.GetType();

                var mi = t.GetMethod(name, F, null, argTypes, null) ?? t.GetMethod(name, F);
                if (mi == null) return null;

                try { return mi.Invoke(obj, args?.Length > 0 ? args : null); }
                catch { return null; }
            }

            public static bool TryInvoke(object obj, string name, params object[] args)
            {
                if (obj == null) return false;
                var methods = obj.GetType().GetMethods(F).Where(m => m.Name == name).ToArray();
                foreach (var m in methods)
                {
                    var pars = m.GetParameters();
                    if (pars.Length != (args?.Length ?? 0)) continue;
                    try { m.Invoke(obj, args); return true; } catch { }
                }
                return false;
            }

            public static bool TryGetInt(object obj, out int value, params string[] names)
            {
                value = 0;
                if (obj == null || names == null) return false;
                foreach (var n in names)
                {
                    var v = Get(obj, n);
                    if (v is int i) { value = i; return true; }
                    if (v is float f) { value = (int)f; return true; }
                }
                return false;
            }

            public static void SetNumberIfExists(object obj, string name, double number)
            {
                if (obj == null) return;
                var t = obj.GetType();

                var pi = t.GetProperty(name, F);
                if (pi != null && pi.CanWrite && (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(float)))
                {
                    try
                    {
                        if (pi.PropertyType == typeof(int)) pi.SetValue(obj, (int)number);
                        else pi.SetValue(obj, (float)number);
                        return;
                    }
                    catch { }
                }

                var fi = t.GetField(name, F);
                if (fi != null && !fi.IsInitOnly && (fi.FieldType == typeof(int) || fi.FieldType == typeof(float)))
                {
                    try
                    {
                        if (fi.FieldType == typeof(int)) fi.SetValue(obj, (int)number);
                        else fi.SetValue(obj, (float)number);
                    }
                    catch { }
                }
            }

            public static object ShallowClone(object obj)
            {
                if (obj == null) return null;
                var m = obj.GetType().GetMethod("MemberwiseClone", F);
                return m?.Invoke(obj, null);
            }
        }
    }
}
