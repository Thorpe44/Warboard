#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time v38 compatibility migration.
///
/// The local AeldariRulesSystem still contains the old single-detachment
/// storage shape. v38 keeps those rule bodies, but redirects its public
/// detachment queries to AeldariDetachmentRuntime so all selected 11e
/// detachments can be active together.
/// </summary>
[InitializeOnLoad]
public static class WarboardV38MultiDetachmentMigration
{
    private const string Marker =
        "WARBOARD_V38_MULTI_DETACHMENT";

    private const string RelativeRulesPath =
        "Assets/Scripts/Core/AeldariRulesSystem.cs";

    private const string RelativeSelfPath =
        "Assets/Editor/WarboardV38MultiDetachmentMigration.cs";

    static WarboardV38MultiDetachmentMigration()
    {
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        if (EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Run;
            return;
        }

        string projectRoot =
            Directory.GetParent(
                Application.dataPath).FullName;

        string rulesPath =
            Path.Combine(
                projectRoot,
                RelativeRulesPath);

        string selfPath =
            Path.Combine(
                projectRoot,
                RelativeSelfPath);

        try
        {
            if (!File.Exists(rulesPath))
            {
                Debug.LogError(
                    "WARBOARD v38 migration could not find " +
                    RelativeRulesPath + ".");
                return;
            }

            string source =
                File.ReadAllText(rulesPath);

            if (!source.Contains(Marker))
            {
                Backup(
                    projectRoot,
                    rulesPath);

                source =
                    ReplaceMethod(
                        source,
                        "public AeldariDetachment GetDetachment(",
                        GetDetachmentMethod());

                source =
                    ReplaceMethod(
                        source,
                        "public string RuleSummary(",
                        RuleSummaryMethod());

                source =
                    ReplaceMethod(
                        source,
                        "public string DetachmentName(",
                        DetachmentNameMethod());

                source =
                    ReplaceMethod(
                        source,
                        "public AeldariStratagemDefinition[] Stratagems(",
                        StratagemsMethod());

                source =
                    ReplaceMethod(
                        source,
                        "public string[] Enhancements(",
                        EnhancementsMethod());

                source =
                    ReplaceMethod(
                        source,
                        "public bool DetachmentIs(",
                        DetachmentIsMethod());

                int classBrace = source.IndexOf('{');

                if (classBrace < 0)
                {
                    throw new InvalidOperationException(
                        "Could not locate AeldariRulesSystem class body.");
                }

                source =
                    source.Insert(
                        classBrace + 1,
                        "\n    // " + Marker + "\n");

                if (!source.Contains(
                        "AeldariDetachmentRuntime.GetSelected") ||
                    !source.Contains(
                        "AeldariDetachmentRuntime.Has"))
                {
                    throw new InvalidOperationException(
                        "v38 validation failed: multi-detachment runtime references were not installed.");
                }

                File.WriteAllText(
                    rulesPath,
                    NormalizeCrLf(source),
                    new UTF8Encoding(false));

                WriteReport(
                    projectRoot,
                    "v38 multi-detachment compatibility migration completed successfully.\n" +
                    "AeldariRulesSystem detachment queries now use AeldariDetachmentRuntime.\n");

                AssetDatabase.ImportAsset(
                    RelativeRulesPath,
                    ImportAssetOptions.ForceUpdate);
            }

            DeleteSelf(selfPath);
        }
        catch (Exception exception)
        {
            WriteReport(
                projectRoot,
                "v38 migration failed:\n" +
                exception + "\n");

            Debug.LogException(exception);
        }
    }

    private static string ReplaceMethod(
        string source,
        string signatureStart,
        string replacement)
    {
        int signature =
            source.IndexOf(
                signatureStart,
                StringComparison.Ordinal);

        if (signature < 0)
        {
            throw new InvalidOperationException(
                "Could not find method signature: " +
                signatureStart);
        }

        int openBrace =
            source.IndexOf('{', signature);

        if (openBrace < 0)
        {
            throw new InvalidOperationException(
                "Could not find method body for: " +
                signatureStart);
        }

        int depth = 0;
        int closeBrace = -1;

        for (int i = openBrace;
             i < source.Length;
             i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
            {
                depth--;

                if (depth == 0)
                {
                    closeBrace = i;
                    break;
                }
            }
        }

        if (closeBrace < 0)
        {
            throw new InvalidOperationException(
                "Could not find closing brace for: " +
                signatureStart);
        }

        int lineStart = signature;

        while (lineStart > 0 &&
               source[lineStart - 1] != '\n' &&
               source[lineStart - 1] != '\r')
        {
            lineStart--;
        }

        return
            source.Substring(0, lineStart) +
            replacement.TrimEnd() +
            "\n\n" +
            source.Substring(closeBrace + 1)
                .TrimStart('\r', '\n');
    }

    private static string GetDetachmentMethod()
    {
        return @"    public AeldariDetachment GetDetachment(
        string faction)
    {
        AeldariDetachment legacy;

        if (!detachmentByFaction.TryGetValue(
                faction,
                out legacy))
        {
            legacy =
                AeldariDetachment.Warhost;
        }

        return AeldariDetachmentRuntime.Primary(
            faction,
            legacy);
    }";
    }

    private static string RuleSummaryMethod()
    {
        return @"    public string RuleSummary(
        string faction)
    {
        if (!IsAeldariFaction(faction))
            return """";

        AeldariDetachment[] selected =
            AeldariDetachmentRuntime
                .GetSelected(faction)
                .ToArray();

        if (selected.Length == 0)
            return """";

        return string.Join(
            "" | "",
            selected
                .Select(
                    detachment =>
                    {
                        AeldariDetachmentDefinition definition =
                            Definitions[detachment];

                        return
                            definition.DisplayName +
                            "" — "" +
                            definition.RuleName +
                            "": "" +
                            definition.RuleSummary;
                    })
                .ToArray());
    }";
    }

    private static string DetachmentNameMethod()
    {
        return @"    public string DetachmentName(
        string faction)
    {
        if (!IsAeldariFaction(faction))
            return """";

        return string.Join(
            "" + "",
            AeldariDetachmentRuntime
                .GetSelected(faction)
                .Select(
                    detachment =>
                        Definitions[detachment]
                            .DisplayName)
                .ToArray());
    }";
    }

    private static string StratagemsMethod()
    {
        return @"    public AeldariStratagemDefinition[] Stratagems(
        string faction)
    {
        if (!IsAeldariFaction(faction))
            return new AeldariStratagemDefinition[0];

        return AeldariDetachmentRuntime
            .GetSelected(faction)
            .SelectMany(
                detachment =>
                    Definitions[detachment]
                        .Stratagems ??
                    new AeldariStratagemDefinition[0])
            .GroupBy(
                stratagem =>
                    stratagem.Name,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }";
    }

    private static string EnhancementsMethod()
    {
        return @"    public string[] Enhancements(
        string faction)
    {
        if (!IsAeldariFaction(faction))
            return new string[0];

        return AeldariDetachmentRuntime
            .GetSelected(faction)
            .SelectMany(
                detachment =>
                    Definitions[detachment]
                        .Enhancements ??
                    new string[0])
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }";
    }

    private static string DetachmentIsMethod()
    {
        return @"    public bool DetachmentIs(
        string faction,
        AeldariDetachment detachment)
    {
        return
            IsAeldariFaction(faction) &&
            AeldariDetachmentRuntime.Has(
                faction,
                detachment);
    }";
    }

    private static void Backup(
        string projectRoot,
        string rulesPath)
    {
        string backupDirectory =
            Path.Combine(
                projectRoot,
                "Library/WarboardBackups/V38");

        Directory.CreateDirectory(
            backupDirectory);

        File.Copy(
            rulesPath,
            Path.Combine(
                backupDirectory,
                "AeldariRulesSystem_PreV38.cs.txt"),
            true);
    }

    private static void WriteReport(
        string projectRoot,
        string text)
    {
        string path =
            Path.Combine(
                projectRoot,
                "Library/WarboardV38MultiDetachmentReport.txt");

        Directory.CreateDirectory(
            Path.GetDirectoryName(path));

        File.WriteAllText(
            path,
            text,
            new UTF8Encoding(false));
    }

    private static string NormalizeCrLf(
        string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", "\r\n");
    }

    private static void DeleteSelf(
        string selfPath)
    {
        if (File.Exists(selfPath))
            File.Delete(selfPath);

        string meta = selfPath + ".meta";

        if (File.Exists(meta))
            File.Delete(meta);

        AssetDatabase.Refresh(
            ImportAssetOptions.ForceUpdate);
    }
}
#endif
