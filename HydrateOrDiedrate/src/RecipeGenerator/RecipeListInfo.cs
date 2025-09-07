using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.RecipeGenerator;

public class RecipeListInfo
{
    public RecipeListInfo(object targetObject, MemberInfo targetMember, IList list)
    {
        TargetObject = targetObject ?? throw new ArgumentNullException(nameof(targetObject));
        TargetMember = targetMember ?? throw new ArgumentNullException(nameof(targetMember));
        RecipeList = list ?? throw new ArgumentNullException(nameof(list));

        var listType = list.GetType();
        ElementType = listType.IsArray ? listType.GetElementType() : listType.FindGenericInterfaceDefinition(typeof(IList<>))?.GetGenericArguments()[0] ?? typeof(object);


        if(targetObject is ModSystem modSystem)
        {
            Source = modSystem.Mod.Info.ModID;
        }
        else if(targetObject is IWorldAccessor)
        {
            Source = "game";
        }
    }

    public string Source { get; set; } = "unknown";

    public object TargetObject { get; }
    
    public MemberInfo TargetMember { get; }

    public string HostMemberName => TargetMember.Name;

    public IList RecipeList { get; }
    
    public Type ElementType { get; }

    public void GenerateVariantsForHoDWater(IServerWorldAccessor world, RecipeItemProcessor processor, ILogger logger)
    {
        try
        {
            var newRecipes = new List<object>();
            foreach (var recipe in RecipeList)
            {
                processor(world, this, recipe, newRecipes);
            }

            if(newRecipes.Count == 0) return;

            var registerMethod = TargetObject.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault(m => m.Name.Contains("register", StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == ElementType);

            if(registerMethod is not null)
            {
                foreach (var newRecipe in newRecipes)
                {
                    try
                    {
                        registerMethod.Invoke(TargetObject, [newRecipe]);
                    }
                    catch (Exception e)
                    {
                        logger.Error("[{0}] Error registering HoD water variant recipe for {1} ({2}): {3}", Source, TargetObject, HostMemberName, e);
                        continue;
                    }
                }

                return;
            }

            if (RecipeList.GetType().IsArray)
            {
                if (!TargetMember.CanSetValue())
                {
                    logger.Warning("[{0}] Cannot add HoD water variant recipes to {1} ({2}) because the property is read-only.", Source, TargetObject, HostMemberName);
                    return;
                }
                var newArray = Array.CreateInstance(ElementType, RecipeList.Count + newRecipes.Count);
                
                var originalCount = RecipeList.Count;
                Array.Copy((Array)RecipeList, newArray, originalCount);
                
                for (int i = 0; i < newRecipes.Count; i++) newArray.SetValue(newRecipes[i], originalCount + i);

                TargetMember.SetValue(newArray, TargetObject);
                return;
            }

            foreach (var newRecipe in newRecipes) RecipeList.Add(newRecipe);
        }
        catch (Exception e)
        {
            logger.Error("[{0}] Error generating HoD water variant recipes for {1} ({2}): {3}", Source, TargetObject, HostMemberName, e);
        }
    }
}
