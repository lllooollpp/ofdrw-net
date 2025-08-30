using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription.Color;

namespace OfdrwNet.Core.PageDescription.Media
{
    public enum MediaType
    {
        Image,
        Video,
        Other
    }

    public class CtMultiMedia
    {
        private MediaType? type;
        private string? format;
        private StLoc? mediaFile;

        public CtMultiMedia SetType(MediaType t) { type = t; return this; }
        public CtMultiMedia SetFormat(string f) { format = f; return this; }
        public CtMultiMedia SetMediaFile(StLoc loc) { mediaFile = loc; return this; }

        public MediaType? GetType() => type;
        public string? GetFormat() => format;
        public StLoc? GetMediaFile() => mediaFile;
    }
}
