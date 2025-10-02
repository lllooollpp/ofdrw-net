using OfdrwNet.Core.Diagnostics;

namespace OfdrwNet.Converter.Validation;

/// <summary>
/// Semantic validation rule interface
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Rule identifier
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Rule description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Execute validation rule
    /// </summary>
    /// <param name="context">Validation context</param>
    /// <returns>List of errors found (empty if valid)</returns>
    List<ErrorRecord> Validate(ValidationContext context);
}

/// <summary>
/// Validation context containing document structure
/// </summary>
public class ValidationContext
{
    /// <summary>
    /// OFD file path
    /// </summary>
    public string OfdPath { get; init; } = string.Empty;

    /// <summary>
    /// Parsed document elements (key: resource ID, value: element data)
    /// </summary>
    public Dictionary<string, object> Elements { get; init; } = new();

    /// <summary>
    /// Resource references (key: referrer, value: list of referenced IDs)
    /// </summary>
    public Dictionary<string, List<string>> References { get; init; } = new();

    /// <summary>
    /// Page bounding boxes (page index -> bounds)
    /// </summary>
    public Dictionary<int, (double Width, double Height)> PageBounds { get; init; } = new();

    /// <summary>
    /// Add element to context
    /// </summary>
    public void AddElement(string id, object element) => Elements[id] = element;

    /// <summary>
    /// Add reference relationship
    /// </summary>
    public void AddReference(string from, string to)
    {
        if (!References.ContainsKey(from))
            References[from] = new List<string>();
        References[from].Add(to);
    }
}

/// <summary>
/// Semantic rule validation engine
/// </summary>
public class SemanticRuleEngine
{
    private readonly List<IValidationRule> _rules = new();

    /// <summary>
    /// Register validation rule
    /// </summary>
    public SemanticRuleEngine AddRule(IValidationRule rule)
    {
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Execute all registered rules
    /// </summary>
    public List<ErrorRecord> Validate(ValidationContext context)
    {
        var errors = new List<ErrorRecord>();

        foreach (var rule in _rules)
        {
            try
            {
                var ruleErrors = rule.Validate(context);
                errors.AddRange(ruleErrors);
            }
            catch (Exception ex)
            {
                errors.Add(new ErrorRecord
                {
                    Code = "VAL_SEMANTIC_VIOLATION",
                    Message = $"Rule {rule.RuleId} failed: {ex.Message}",
                    Context = context.OfdPath,
                    Severity = ErrorSeverity.Error,
                    Exception = ex,
                    Feature = rule.RuleId
                });
            }
        }

        return errors;
    }

    /// <summary>
    /// Get all registered rule IDs
    /// </summary>
    public List<string> GetRuleIds() => _rules.Select(r => r.RuleId).ToList();
}

/// <summary>
/// Reference integrity validation rule
/// </summary>
public class ReferenceIntegrityRule : IValidationRule
{
    public string RuleId => "REF_INTEGRITY";
    public string Description => "Validates all resource references exist";

    public List<ErrorRecord> Validate(ValidationContext context)
    {
        var errors = new List<ErrorRecord>();

        foreach (var (referrer, targets) in context.References)
        {
            foreach (var target in targets)
            {
                if (!context.Elements.ContainsKey(target))
                {
                    errors.Add(new ErrorRecord
                    {
                        Code = ErrorCodes.STR_INVALID_REFERENCE,
                        Message = $"Referenced resource '{target}' not found",
                        Context = $"{referrer} -> {target}",
                        Severity = ErrorSeverity.Error,
                        Feature = RuleId
                    });
                }
            }
        }

        return errors;
    }
}

/// <summary>
/// Circular reference detection rule
/// </summary>
public class CircularReferenceRule : IValidationRule
{
    public string RuleId => "CIRCULAR_REF";
    public string Description => "Detects circular resource references";

    public List<ErrorRecord> Validate(ValidationContext context)
    {
        var errors = new List<ErrorRecord>();
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var node in context.References.Keys)
        {
            if (DetectCycle(node, context, visited, recursionStack, out var cycle))
            {
                errors.Add(new ErrorRecord
                {
                    Code = ErrorCodes.STR_CIRCULAR_REFERENCE,
                    Message = "Circular reference detected",
                    Context = string.Join(" -> ", cycle),
                    Severity = ErrorSeverity.Error,
                    Feature = RuleId
                });
            }
        }

        return errors;
    }

    private bool DetectCycle(string node, ValidationContext context, HashSet<string> visited,
        HashSet<string> stack, out List<string> cycle)
    {
        cycle = new List<string>();

        if (!visited.Contains(node))
        {
            visited.Add(node);
            stack.Add(node);

            if (context.References.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        if (DetectCycle(neighbor, context, visited, stack, out cycle))
                        {
                            cycle.Insert(0, node);
                            return true;
                        }
                    }
                    else if (stack.Contains(neighbor))
                    {
                        cycle.Add(neighbor);
                        cycle.Add(node);
                        return true;
                    }
                }
            }
        }

        stack.Remove(node);
        return false;
    }
}

/// <summary>
/// Bounds validation rule (objects within page bounds)
/// </summary>
public class BoundsValidationRule : IValidationRule
{
    public string RuleId => "BOUNDS_CHECK";
    public string Description => "Validates objects are within page boundaries";

    public List<ErrorRecord> Validate(ValidationContext context)
    {
        var errors = new List<ErrorRecord>();

        // Placeholder: actual bounds checking requires parsing page objects
        // Will be implemented in resource/rendering phases

        return errors;
    }
}
