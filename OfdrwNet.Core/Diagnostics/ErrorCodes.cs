namespace OfdrwNet.Core.Diagnostics;

/// <summary>
/// Standard error codes for conversion process.
/// Organized by domain prefix for categorization and filtering.
/// </summary>
public static class ErrorCodes
{
    // STR - Structure & References
    public const string STR_MISSING_RESOURCE = "STR_MISSING_RESOURCE";
    public const string STR_INVALID_REFERENCE = "STR_INVALID_REFERENCE";
    public const string STR_CIRCULAR_REFERENCE = "STR_CIRCULAR_REFERENCE";
    public const string STR_INVALID_PAGE_TREE = "STR_INVALID_PAGE_TREE";
    public const string STR_MALFORMED_DOCUMENT = "STR_MALFORMED_DOCUMENT";

    // RES - Resources (Fonts, Images, ICC)
    public const string RES_FONT_UNEMBEDDED = "RES_FONT_UNEMBEDDED";
    public const string RES_FONT_ENCODING_ERROR = "RES_FONT_ENCODING_ERROR";
    public const string RES_FONT_GB18030_INCOMPLETE = "RES_FONT_GB18030_INCOMPLETE";
    public const string RES_IMAGE_EXTRACTION_FAILED = "RES_IMAGE_EXTRACTION_FAILED";
    public const string RES_IMAGE_DPI_EXCEEDED = "RES_IMAGE_DPI_EXCEEDED";
    public const string RES_ICC_LOAD_FAILED = "RES_ICC_LOAD_FAILED";
    public const string RES_ICC_TRANSFORM_FAILED = "RES_ICC_TRANSFORM_FAILED";

    // REC - Recognition (Table/Formula)
    public const string REC_TABLE_LOW_CONF = "REC_TABLE_LOW_CONF";
    public const string REC_TABLE_DETECTION_FAILED = "REC_TABLE_DETECTION_FAILED";
    public const string REC_FORMULA_LOW_CONF = "REC_FORMULA_LOW_CONF";
    public const string REC_FORMULA_PARSING_FAILED = "REC_FORMULA_PARSING_FAILED";
    public const string REC_COMPOSITE_FALLBACK = "REC_COMPOSITE_FALLBACK";

    // JS - JavaScript Processing
    public const string JS_REMOVED = "JS_REMOVED";
    public const string JS_SCAN_ERROR = "JS_SCAN_ERROR";
    public const string JS_SNAPSHOT_FAILED = "JS_SNAPSHOT_FAILED";
    public const string JS_EXECUTION_TIMEOUT = "JS_EXECUTION_TIMEOUT";

    // XFA - XFA Forms
    public const string XFA_UNSUPPORTED_SCRIPT = "XFA_UNSUPPORTED_SCRIPT";
    public const string XFA_CALCULATION_REMOVED = "XFA_CALCULATION_REMOVED";
    public const string XFA_VALIDATION_LOST = "XFA_VALIDATION_LOST";
    public const string XFA_FORMATTING_LOST = "XFA_FORMATTING_LOST";

    // SEC - Security (Signature, Permission, Encryption)
    public const string SEC_PERM_NOT_ENCRYPTED = "SEC_PERM_NOT_ENCRYPTED";
    public const string SEC_SIGNER_LOAD_FAILED = "SEC_SIGNER_LOAD_FAILED";
    public const string SEC_SIGNATURE_CREATION_FAILED = "SEC_SIGNATURE_CREATION_FAILED";
    public const string SEC_ENCRYPTION_FAILED = "SEC_ENCRYPTION_FAILED";
    public const string SEC_INVALID_PERMISSION_CONFIG = "SEC_INVALID_PERMISSION_CONFIG";

    // CLR - Color Management
    public const string CLR_DELTA_EXCEED = "CLR_DELTA_EXCEED";
    public const string CLR_FALLBACK_SRGB = "CLR_FALLBACK_SRGB";
    public const string CLR_COLORSPACE_UNSUPPORTED = "CLR_COLORSPACE_UNSUPPORTED";

    // VER - Version Control
    public const string VER_CHAIN_LIMIT = "VER_CHAIN_LIMIT";
    public const string VER_MERGE_FAILED = "VER_MERGE_FAILED";
    public const string VER_DELTA_TOO_LARGE = "VER_DELTA_TOO_LARGE";
    public const string VER_METADATA_WRITE_FAILED = "VER_METADATA_WRITE_FAILED";

    // CMP - Compatibility & Downgrade
    public const string CMP_SOFTMASK_RASTERIZED = "CMP_SOFTMASK_RASTERIZED";
    public const string CMP_TRANSPARENCY_FLATTENED = "CMP_TRANSPARENCY_FLATTENED";
    public const string CMP_FEATURE_UNSUPPORTED = "CMP_FEATURE_UNSUPPORTED";
    public const string CMP_PROFILE_LOAD_FAILED = "CMP_PROFILE_LOAD_FAILED";
    public const string CMP_DOWNGRADE_FAILED = "CMP_DOWNGRADE_FAILED";

    // MEM - Memory & Segmentation
    public const string MEM_SEGMENT_TRIGGER = "MEM_SEGMENT_TRIGGER";
    public const string MEM_THRESHOLD_EXCEEDED = "MEM_THRESHOLD_EXCEEDED";
    public const string MEM_SEGMENT_MERGE_FAILED = "MEM_SEGMENT_MERGE_FAILED";
    public const string MEM_OUT_OF_MEMORY = "MEM_OUT_OF_MEMORY";

    // BAT - Batch Processing
    public const string BAT_FILE_FAILED = "BAT_FILE_FAILED";
    public const string BAT_PARALLEL_EXCEPTION = "BAT_PARALLEL_EXCEPTION";
    public const string BAT_SCHEDULING_ERROR = "BAT_SCHEDULING_ERROR";

    // VAL - Validation
    public const string VAL_SCHEMA_ERROR = "VAL_SCHEMA_ERROR";
    public const string VAL_SEMANTIC_VIOLATION = "VAL_SEMANTIC_VIOLATION";
    public const string VAL_REFERENCE_INTEGRITY = "VAL_REFERENCE_INTEGRITY";
    public const string VAL_BOUNDS_VIOLATION = "VAL_BOUNDS_VIOLATION";

    // INT - Interaction (Annotations, Links, Bookmarks)
    public const string INT_ANNOTATION_MAPPING_FAILED = "INT_ANNOTATION_MAPPING_FAILED";
    public const string INT_LINK_ACTION_UNSUPPORTED = "INT_LINK_ACTION_UNSUPPORTED";
    public const string INT_BOOKMARK_MALFORMED = "INT_BOOKMARK_MALFORMED";
    public const string INT_FORM_FIELD_CONVERSION_FAILED = "INT_FORM_FIELD_CONVERSION_FAILED";

    // MED - Media Resources
    public const string MED_EXTRACTION_FAILED = "MED_EXTRACTION_FAILED";
    public const string MED_FORMAT_UNSUPPORTED = "MED_FORMAT_UNSUPPORTED";
    public const string MED_EMBEDDING_FAILED = "MED_EMBEDDING_FAILED";

    // EXT - Extensions & Metadata
    public const string EXT_CUSTOM_METADATA_INVALID = "EXT_CUSTOM_METADATA_INVALID";
    public const string EXT_TAG_WRITE_FAILED = "EXT_TAG_WRITE_FAILED";
    public const string EXT_TEMPLATE_LOAD_FAILED = "EXT_TEMPLATE_LOAD_FAILED";

    // GEN - General / Uncategorized
    public const string GEN_CONVERSION_FAILED = "GEN_CONVERSION_FAILED";
    public const string GEN_IO_ERROR = "GEN_IO_ERROR";
    public const string GEN_UNEXPECTED_ERROR = "GEN_UNEXPECTED_ERROR";
    public const string GEN_CONFIGURATION_INVALID = "GEN_CONFIGURATION_INVALID";
}
