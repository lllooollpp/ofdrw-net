using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfdrwNet.Converter.Batch;
using OfdrwNet.Converter.ColorManagement;
using OfdrwNet.Converter.Compatibility;
using OfdrwNet.Converter.Extensions;
using OfdrwNet.Converter.Forms;
using OfdrwNet.Converter.Interaction;
using OfdrwNet.Converter.Layout;
using OfdrwNet.Converter.Media;
using OfdrwNet.Converter.Memory;
using OfdrwNet.Converter.Recognition;
using OfdrwNet.Converter.Reporting;
using OfdrwNet.Converter.Scripting;
using OfdrwNet.Converter.Security;
using OfdrwNet.Converter.Validation;
using OfdrwNet.Converter.Versioning;

namespace OfdrwNet.Converter.Configuration;

/// <summary>
/// Dependency injection extensions for PDF-to-OFD converter services
/// </summary>
public static class ConverterServiceCollectionExtensions
{
    /// <summary>
    /// Register all PDF-to-OFD converter services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddOfdConverter(
        this IServiceCollection services,
        Action<ConverterServicesOptions>? configure = null)
    {
        var options = new ConverterServicesOptions();
        configure?.Invoke(options);

        // Register core services
        services.AddColorManagementServices(options);
        services.AddRecognitionServices(options);
        services.AddLayoutServices(options);
        services.AddFormServices(options);
        services.AddInteractionServices(options);
        services.AddScriptingServices(options);
        services.AddMediaServices(options);
        services.AddMemoryServices(options);
        services.AddVersioningServices(options);
        services.AddCompatibilityServices(options);
        services.AddSecurityServices(options);
        services.AddExtensionServices(options);
        services.AddValidationServices(options);
        services.AddReportingServices(options);
        services.AddBatchServices(options);

        return services;
    }

    /// <summary>
    /// Register color management services (T040-T041)
    /// </summary>
    private static IServiceCollection AddColorManagementServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableColorManagement)
        {
            services.TryAddSingleton<ColorSpaceConverter>();
            services.TryAddSingleton<ColorProfileManager>();
        }

        return services;
    }

    /// <summary>
    /// Register configuration services (T042)
    /// </summary>
    public static IServiceCollection AddConfigurationServices(
        this IServiceCollection services)
    {
        services.TryAddSingleton<ConverterOptionsBuilder>();
        return services;
    }

    /// <summary>
    /// Register recognition services (T043-T045)
    /// </summary>
    private static IServiceCollection AddRecognitionServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableRecognition)
        {
            services.TryAddSingleton<RuleBasedTableRecognizer>();
            services.TryAddSingleton<BasicFormulaRecognizer>();
            services.TryAddSingleton<CompositeFallbackPolicy>();
        }

        return services;
    }

    /// <summary>
    /// Register layout services (T046)
    /// </summary>
    private static IServiceCollection AddLayoutServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableLayoutDetection)
        {
            services.TryAddSingleton<LayoutFeaturesDetector>();
        }

        return services;
    }

    /// <summary>
    /// Register form services (T047-T048)
    /// </summary>
    private static IServiceCollection AddFormServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableFormMapping)
        {
            services.TryAddSingleton<PdfFormMapper>();
            services.TryAddSingleton<XfaDetector>();
            services.TryAddSingleton<XfaHintWriter>();
        }

        return services;
    }

    /// <summary>
    /// Register interaction services (T049-T051)
    /// </summary>
    private static IServiceCollection AddInteractionServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableInteraction)
        {
            services.TryAddSingleton<AnnotationExtractor>();
            services.TryAddSingleton<BookmarkConverter>();
            services.TryAddSingleton<ActionMapper>();
        }

        return services;
    }

    /// <summary>
    /// Register scripting services (T052-T053)
    /// </summary>
    private static IServiceCollection AddScriptingServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableScripting)
        {
            services.TryAddSingleton<JavaScriptScanner>();
            services.TryAddSingleton<QuickJsSnapshotExecutor>();
        }

        return services;
    }

    /// <summary>
    /// Register media services (T054)
    /// </summary>
    private static IServiceCollection AddMediaServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableMedia)
        {
            services.TryAddSingleton<MediaExtractor>();
        }

        return services;
    }

    /// <summary>
    /// Register memory management services (T055-T057)
    /// </summary>
    private static IServiceCollection AddMemoryServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableMemoryManagement)
        {
            services.TryAddSingleton<MemoryGuard>();
        }

        return services;
    }

    /// <summary>
    /// Register versioning services (T056-T057)
    /// </summary>
    private static IServiceCollection AddVersioningServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableVersioning)
        {
            services.TryAddSingleton<DiffBasedVersionManager>();
            services.TryAddSingleton<VersionMergeService>();
        }

        return services;
    }

    /// <summary>
    /// Register compatibility services (T058-T059)
    /// </summary>
    private static IServiceCollection AddCompatibilityServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableCompatibility)
        {
            services.TryAddSingleton<JsonCompatibilityProfileProvider>();
            services.TryAddSingleton<FeatureDowngrader>();
        }

        return services;
    }

    /// <summary>
    /// Register security services (T063-T064)
    /// </summary>
    private static IServiceCollection AddSecurityServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableSecurity)
        {
            services.TryAddSingleton<PermissionConfigurator>();
            services.TryAddSingleton<CryptoEngine>();
        }

        return services;
    }

    /// <summary>
    /// Register extension services (T065-T067)
    /// </summary>
    private static IServiceCollection AddExtensionServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableExtensions)
        {
            services.TryAddSingleton<MetadataService>();
            services.TryAddSingleton<AttachmentAdder>();
        }

        return services;
    }

    /// <summary>
    /// Register batch processing services (T067)
    /// </summary>
    private static IServiceCollection AddBatchServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableBatchProcessing)
        {
            services.TryAddSingleton<BatchProcessor>();
        }

        return services;
    }

    /// <summary>
    /// Register validation services (T068-T070)
    /// </summary>
    private static IServiceCollection AddValidationServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableValidation)
        {
            services.TryAddSingleton<SchemaValidator>();
            services.TryAddSingleton<SemanticRuleEngine>();
            services.TryAddSingleton<CompositeValidationEngine>();
        }

        return services;
    }

    /// <summary>
    /// Register reporting services (T071)
    /// </summary>
    private static IServiceCollection AddReportingServices(
        this IServiceCollection services,
        ConverterServicesOptions options)
    {
        if (options.EnableReporting)
        {
            services.TryAddScoped<ErrorReportBuilder>();
        }

        return services;
    }

    /// <summary>
    /// Register signing services (T060-T062) - separate from converter core
    /// </summary>
    public static IServiceCollection AddOfdSigning(this IServiceCollection services)
    {
        // Signing services are registered separately via OfdrwNet.Sign project
        // This is a placeholder for documentation
        return services;
    }
}

/// <summary>
/// Configuration options for converter services
/// </summary>
public class ConverterServicesOptions
{
    /// <summary>
    /// Enable color management services (T040-T041)
    /// </summary>
    public bool EnableColorManagement { get; set; } = true;

    /// <summary>
    /// Enable recognition services (T043-T045)
    /// </summary>
    public bool EnableRecognition { get; set; } = true;

    /// <summary>
    /// Enable layout detection (T046)
    /// </summary>
    public bool EnableLayoutDetection { get; set; } = true;

    /// <summary>
    /// Enable form mapping services (T047-T048)
    /// </summary>
    public bool EnableFormMapping { get; set; } = true;

    /// <summary>
    /// Enable interaction services (T049-T051)
    /// </summary>
    public bool EnableInteraction { get; set; } = true;

    /// <summary>
    /// Enable scripting services (T052-T053)
    /// </summary>
    public bool EnableScripting { get; set; } = true;

    /// <summary>
    /// Enable media extraction (T054)
    /// </summary>
    public bool EnableMedia { get; set; } = true;

    /// <summary>
    /// Enable memory management (T055)
    /// </summary>
    public bool EnableMemoryManagement { get; set; } = true;

    /// <summary>
    /// Enable versioning services (T056-T057)
    /// </summary>
    public bool EnableVersioning { get; set; } = false;

    /// <summary>
    /// Enable compatibility services (T058-T059)
    /// </summary>
    public bool EnableCompatibility { get; set; } = true;

    /// <summary>
    /// Enable security services (T063-T064)
    /// </summary>
    public bool EnableSecurity { get; set; } = true;

    /// <summary>
    /// Enable extension services (T065-T066)
    /// </summary>
    public bool EnableExtensions { get; set; } = true;

    /// <summary>
    /// Enable batch processing (T067)
    /// </summary>
    public bool EnableBatchProcessing { get; set; } = true;

    /// <summary>
    /// Enable validation services (T068-T070)
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Enable reporting services (T071)
    /// </summary>
    public bool EnableReporting { get; set; } = true;
}
