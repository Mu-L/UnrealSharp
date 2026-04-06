using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Exporters;

namespace UnrealSharpManagedGlue.Utilities;

public static class PropertyGetterSetterUtilities
{
    public static bool MakeGetterSetterPair(this UhtFunction function, Dictionary<string, GetterSetterPair> getterSetterPairs)
    {
        string scriptName = function.GetFunctionName();
        bool isGetter = CheckIfGetter(scriptName, function);
        bool isSetter = CheckIfSetter(scriptName, function);

        if (!isGetter && !isSetter)
        {
            return false;
        }

        string propertyName = scriptName.Length > 3 ? scriptName.Substring(3) : function.SourceName;
        propertyName = NameMapper.EscapeKeywords(propertyName);

        UhtClass owningClass = (UhtClass)function.Outer!;
        UhtFunction? sameNameFunction = owningClass.FindFunctionByName(propertyName);
        if (sameNameFunction != null && sameNameFunction != function)
        {
            return false;
        }

        UhtProperty? propertyWithSameName = owningClass.FindPropertyByName(propertyName, (property, name) => property.SourceName == name || property.GetPropertyName() == name);
        UhtProperty primaryProperty = GetPrimaryProperty(function);
        
        if (propertyWithSameName != null && (!propertyWithSameName.IsSameType(primaryProperty) || propertyWithSameName.HasAnyGetter() || propertyWithSameName.HasAnySetter()))
        {
            return false;
        }

        if (!getterSetterPairs.TryGetValue(propertyName, out GetterSetterPair? pair))
        {
            pair = new GetterSetterPair(propertyName, primaryProperty);
            getterSetterPairs[propertyName] = pair;
        }

        if (pair.Accessors.Count == 2)
        {
            return true;
        }

        if (isGetter)
        {
            pair.Getter = function;
            pair.GetterExporter = GetterSetterFunctionExporter.Create(function, primaryProperty, GetterSetterMode.Get, EFunctionProtectionMode.UseUFunctionProtection);
        }
        else
        {
            pair.Setter = function;
            pair.SetterExporter = GetterSetterFunctionExporter.Create(function, primaryProperty, GetterSetterMode.Set, EFunctionProtectionMode.UseUFunctionProtection);
        }
        
        return true;
    }

    static bool CheckIfGetter(string scriptName, UhtFunction function)
    {
        if (!scriptName.StartsWith("Get", StringComparison.Ordinal) || function.IsBlueprintEvent())
        {
            return false;
        }

        int paramCount = function.Properties.Count();
        if (function.ReturnProperty != null)
        {
            return paramCount == 1 || (paramCount == 2 && function.Properties.First().IsWorldContextParameter());
        }

        return paramCount == 1 && function.HasOutParams() && !function.Properties.First().HasAnyFlags(EPropertyFlags.ConstParm);
    }

    static bool CheckIfSetter(string scriptName, UhtFunction function)
    {
        if (!scriptName.StartsWith("Set", StringComparison.Ordinal) || function.IsBlueprintEvent() || function.ReturnProperty != null)
        {
            return false;
        }

        UhtProperty? property = function.Properties.FirstOrDefault();
        return property != null && function.Properties.Count() == 1 && (!property.HasAllFlags(EPropertyFlags.OutParm | EPropertyFlags.ReferenceParm) || property.HasAllFlags(EPropertyFlags.ConstParm));
    }
    
    static UhtProperty GetPrimaryProperty(UhtFunction function)
    {
        if (function.ReturnProperty != null)
        {
            return function.ReturnProperty;
        }

        return function.Properties.FirstOrDefault(p => p.HasAllFlags(EPropertyFlags.OutParm) && !p.HasAllFlags(EPropertyFlags.ConstParm)) ?? function.Properties.First();
    }
}