using SyncLabsHub.Core.Models;

namespace SyncLabsHub.Core;

/// <summary>
/// The product catalog. Mirrors synclabs/src/data/tools.ts. When the website gains a
/// products table this can move to Supabase; the Hub already treats it as a flat list.
/// </summary>
public static class ToolCatalog
{
    public static readonly IReadOnlyList<ToolCatalogEntry> All = new List<ToolCatalogEntry>
    {
        new()
        {
            Id = "dynamo-batch-processor",
            Name = "Dynamo Batch Processor",
            Tagline = "Queue and run Dynamo scripts across hundreds of Revit files in one action.",
            Platforms = new[] { "Revit", "Dynamo" },
            Kind = ToolKind.RevitPlugin,
            Glyph = "Code24"
        },
        new()
        {
            Id = "parameter-sync",
            Name = "Parameter Sync",
            Tagline = "Detect and propagate parameter changes across linked models automatically.",
            Platforms = new[] { "Revit" },
            Kind = ToolKind.RevitPlugin,
            Glyph = "ArrowSync24"
        },
        new()
        {
            Id = "grasshopper-automation",
            Name = "Grasshopper Automation Hub",
            Tagline = "Turn Grasshopper definitions into 24/7 automated, triggered pipelines.",
            Platforms = new[] { "Rhino", "Grasshopper" },
            Kind = ToolKind.RhinoPlugin,
            Glyph = "Cube24"
        },
        new()
        {
            Id = "model-checker",
            Name = "Model Checker",
            Tagline = "Run standards checks on Revit models and catch violations before rework.",
            Platforms = new[] { "Revit" },
            Kind = ToolKind.RevitPlugin,
            Glyph = "ShieldCheckmark24"
        },
        new()
        {
            Id = "data-exporter",
            Name = "Data Exporter Pro",
            Tagline = "Extract element data from Revit into Excel, CSV, JSON or SQL in minutes.",
            Platforms = new[] { "Revit", "Dynamo" },
            Kind = ToolKind.RevitPlugin,
            Glyph = "DocumentArrowDown24"
        },
        new()
        {
            Id = "ai-element-classifier",
            Name = "AI Element Classifier",
            Tagline = "Auto-classify and tag model elements with machine learning.",
            Platforms = new[] { "Revit", "Dynamo", "Grasshopper" },
            Kind = ToolKind.RevitPlugin,
            Glyph = "BrainCircuit24"
        }
    };

    public static ToolCatalogEntry? ById(string id) =>
        All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
}
