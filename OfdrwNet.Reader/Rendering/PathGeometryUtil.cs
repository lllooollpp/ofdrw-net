using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace OfdrwNet.Reader.Rendering
{
    internal static class PathGeometryUtil
    {
        internal static List<PathSegment> Parse(string? pathData)
        {
            var segments = new List<PathSegment>();
            if (string.IsNullOrWhiteSpace(pathData))
            {
                return segments;
            }

            var tokens = Tokenize(pathData);
            if (tokens.Count == 0)
            {
                return segments;
            }

            int index = 0;
            PointF current = PointF.Empty;
            PointF subPathStart = PointF.Empty;
            PointF? lastCubicControl = null;
            PointF? lastQuadraticControl = null;

            while (index < tokens.Count)
            {
                char commandChar = tokens[index][0];
                if (!char.IsLetter(commandChar))
                {
                    throw new FormatException($"Unexpected token '{tokens[index]}' in path data.");
                }
                index++;

                bool isRelative = char.IsLower(commandChar);
                char cmd = char.ToUpperInvariant(commandChar);

                switch (cmd)
                {
                    case 'M':
                    {
                        var points = ReadPoints(tokens, ref index, current, isRelative, expectedPairs: null);
                        if (points.Count == 0)
                        {
                            throw new FormatException("Move command must include coordinates.");
                        }

                        var first = points[0];
                        segments.Add(PathSegment.MoveTo(first));
                        current = first;
                        subPathStart = first;
                        lastCubicControl = null;
                        lastQuadraticControl = null;

                        for (int i = 1; i < points.Count; i++)
                        {
                            var pt = points[i];
                            segments.Add(PathSegment.LineTo(pt));
                            current = pt;
                        }
                        break;
                    }
                    case 'L':
                    {
                        var points = ReadPoints(tokens, ref index, current, isRelative, expectedPairs: null);
                        foreach (var pt in points)
                        {
                            segments.Add(PathSegment.LineTo(pt));
                            current = pt;
                        }
                        lastCubicControl = null;
                        lastQuadraticControl = null;
                        break;
                    }
                    case 'H':
                    {
                        while (index < tokens.Count && !char.IsLetter(tokens[index][0]))
                        {
                            float x = ParseFloat(tokens[index++]);
                            if (isRelative)
                            {
                                x = current.X + x;
                            }
                            current = new PointF(x, current.Y);
                            segments.Add(PathSegment.LineTo(current));
                        }
                        lastCubicControl = null;
                        lastQuadraticControl = null;
                        break;
                    }
                    case 'V':
                    {
                        while (index < tokens.Count && !char.IsLetter(tokens[index][0]))
                        {
                            float y = ParseFloat(tokens[index++]);
                            if (isRelative)
                            {
                                y = current.Y + y;
                            }
                            current = new PointF(current.X, y);
                            segments.Add(PathSegment.LineTo(current));
                        }
                        lastCubicControl = null;
                        lastQuadraticControl = null;
                        break;
                    }
                    case 'C':
                    {
                        while (index < tokens.Count && !char.IsLetter(tokens[index][0]))
                        {
                            if (index + 5 >= tokens.Count)
                            {
                                throw new FormatException("Cubic curve command requires six parameters per segment.");
                            }

                            var c1 = ParsePoint(tokens, ref index, current, isRelative);
                            var c2 = ParsePoint(tokens, ref index, current, isRelative);
                            var end = ParsePoint(tokens, ref index, current, isRelative);

                            segments.Add(PathSegment.CubicBezier(c1, c2, end));
                            current = end;
                            lastCubicControl = c2;
                            lastQuadraticControl = null;
                        }
                        break;
                    }
                    case 'S':
                    {
                        while (index < tokens.Count && !char.IsLetter(tokens[index][0]))
                        {
                            if (index + 3 >= tokens.Count)
                            {
                                throw new FormatException("Smooth cubic curve requires four parameters per segment.");
                            }

                            PointF reflected = lastCubicControl.HasValue
                                ? ReflectPoint(lastCubicControl.Value, current)
                                : current;

                            var c2 = ParsePoint(tokens, ref index, current, isRelative);
                            var end = ParsePoint(tokens, ref index, current, isRelative);

                            segments.Add(PathSegment.CubicBezier(reflected, c2, end));
                            current = end;
                            lastCubicControl = c2;
                            lastQuadraticControl = null;
                        }
                        break;
                    }
                    case 'Q':
                    {
                        while (index < tokens.Count && !char.IsLetter(tokens[index][0]))
                        {
                            if (index + 3 >= tokens.Count)
                            {
                                throw new FormatException("Quadratic curve requires four parameters per segment.");
                            }

                            var control = ParsePoint(tokens, ref index, current, isRelative);
                            var end = ParsePoint(tokens, ref index, current, isRelative);
                            (PointF c1, PointF c2) = QuadraticToCubic(current, control, end);
                            segments.Add(PathSegment.CubicBezier(c1, c2, end));
                            current = end;
                            lastCubicControl = c2;
                            lastQuadraticControl = control;
                        }
                        break;
                    }
                    case 'T':
                    {
                        while (index < tokens.Count && !char.IsLetter(tokens[index][0]))
                        {
                            if (index + 1 >= tokens.Count)
                            {
                                throw new FormatException("Smooth quadratic curve requires two parameters per segment.");
                            }

                            PointF control = lastQuadraticControl.HasValue
                                ? ReflectPoint(lastQuadraticControl.Value, current)
                                : current;
                            var end = ParsePoint(tokens, ref index, current, isRelative);
                            (PointF c1, PointF c2) = QuadraticToCubic(current, control, end);
                            segments.Add(PathSegment.CubicBezier(c1, c2, end));
                            current = end;
                            lastCubicControl = c2;
                            lastQuadraticControl = control;
                        }
                        break;
                    }
                    case 'A':
                    {
                        while (index < tokens.Count && !char.IsLetter(tokens[index][0]))
                        {
                            if (index + 6 >= tokens.Count)
                            {
                                throw new FormatException("Arc command requires seven parameters per segment.");
                            }

                            float rx = ParseFloat(tokens[index++]);
                            float ry = ParseFloat(tokens[index++]);
                            float angle = ParseFloat(tokens[index++]);
                            float largeArc = ParseFloat(tokens[index++]);
                            float sweep = ParseFloat(tokens[index++]);
                            var end = ParsePoint(tokens, ref index, current, isRelative);

                            var arcSegments = ArcToCubicBeziers(current, end, rx, ry, angle, largeArc != 0f, sweep != 0f);
                            foreach (var arcSeg in arcSegments)
                            {
                                segments.Add(arcSeg);
                                if (arcSeg.Points.Count > 0)
                                {
                                    current = arcSeg.Points[arcSeg.Points.Count - 1];
                                }
                                if (arcSeg.Command == 'C' && arcSeg.Points.Count >= 2)
                                {
                                    lastCubicControl = arcSeg.Points[arcSeg.Points.Count - 2];
                                }
                                else
                                {
                                    lastCubicControl = null;
                                }
                            }
                            lastQuadraticControl = null;
                        }
                        break;
                    }
                    case 'Z':
                    {
                        segments.Add(PathSegment.ClosePath());
                        current = subPathStart;
                        lastCubicControl = null;
                        lastQuadraticControl = null;
                        break;
                    }
                    default:
                        throw new FormatException($"Unsupported path command '{cmd}'.");
                }
            }

            return segments;
        }

        internal static List<PathSegment> ApplyTransform(List<PathSegment> segments, Matrix? ctm, double scaleX, double scaleY, bool convertToPixels)
        {
            if (segments.Count == 0)
            {
                return segments;
            }

            float a = 1f, b = 0f, c = 0f, d = 1f, e = 0f, f = 0f;
            if (ctm != null)
            {
                var elements = ctm.Elements;
                a = elements[0];
                b = elements[1];
                c = elements[2];
                d = elements[3];
                e = elements[4];
                f = elements[5];
            }

            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                if (seg.Command == 'Z' || seg.Points.Count == 0)
                {
                    continue;
                }

                var transformedPoints = new List<PointF>(seg.Points.Count);
                foreach (var pt in seg.Points)
                {
                    float x = pt.X;
                    float y = pt.Y;
                    float tx = a * x + c * y + e;
                    float ty = b * x + d * y + f;

                    if (convertToPixels)
                    {
                        tx = (float)(tx * scaleX);
                        ty = (float)(ty * scaleY);
                    }

                    transformedPoints.Add(new PointF(tx, ty));
                }

                segments[i] = new PathSegment(seg.Command, transformedPoints);
            }

            return segments;
        }

        internal static GraphicsPath ToGraphicsPath(List<PathSegment> segments)
        {
            var path = new GraphicsPath();
            if (segments.Count == 0)
            {
                return path;
            }

            PointF current = PointF.Empty;
            PointF subPathStart = PointF.Empty;
            bool figureOpen = false;

            foreach (var segment in segments)
            {
                switch (segment.Command)
                {
                    case 'M':
                        if (segment.Points.Count > 0)
                        {
                            current = segment.Points[0];
                            subPathStart = current;
                            path.StartFigure();
                            figureOpen = true;
                        }
                        break;
                    case 'L':
                        if (segment.Points.Count > 0)
                        {
                            if (!figureOpen)
                            {
                                path.StartFigure();
                                figureOpen = true;
                            }
                            var dest = segment.Points[0];
                            path.AddLine(current, dest);
                            current = dest;
                        }
                        break;
                    case 'C':
                        if (segment.Points.Count >= 3)
                        {
                            if (!figureOpen)
                            {
                                path.StartFigure();
                                figureOpen = true;
                            }
                            var c1 = segment.Points[0];
                            var c2 = segment.Points[1];
                            var end = segment.Points[2];
                            path.AddBezier(current, c1, c2, end);
                            current = end;
                        }
                        break;
                    case 'Z':
                        if (figureOpen)
                        {
                            path.CloseFigure();
                            figureOpen = false;
                        }
                        current = subPathStart;
                        break;
                }
            }

            return path;
        }

        internal static RectangleF ComputeBounds(List<PathSegment> segments)
        {
            bool hasPoint = false;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var segment in segments)
            {
                foreach (var pt in segment.Points)
                {
                    hasPoint = true;
                    if (pt.X < minX) minX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y > maxY) maxY = pt.Y;
                }
            }

            return hasPoint
                ? RectangleF.FromLTRB(minX, minY, maxX, maxY)
                : RectangleF.Empty;
        }

        internal static string ToPathData(List<PathSegment> segments)
        {
            var sb = new System.Text.StringBuilder();
            bool first = true;
            foreach (var segment in segments)
            {
                if (!first)
                {
                    sb.Append(' ');
                }
                first = false;
                sb.Append(segment.Command);
                if (segment.Points.Count > 0)
                {
                    sb.Append(' ');
                    for (int i = 0; i < segment.Points.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(' ');
                        }
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###}", segment.Points[i].X, segment.Points[i].Y);
                    }
                }
            }
            return sb.ToString();
        }

        private static List<string> Tokenize(string data)
        {
            var tokens = new List<string>();
            var sb = new System.Text.StringBuilder();
            foreach (char ch in data)
            {
                if (char.IsLetter(ch))
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                    tokens.Add(ch.ToString());
                }
                else if (char.IsWhiteSpace(ch) || ch == ',')
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }

            if (sb.Length > 0)
            {
                tokens.Add(sb.ToString());
            }

            return tokens;
        }

        private static List<PointF> ReadPoints(List<string> tokens, ref int index, PointF current, bool isRelative, int? expectedPairs)
        {
            var points = new List<PointF>();
            int consumedPairs = 0;

            while (index < tokens.Count && !char.IsLetter(tokens[index][0]))
            {
                float x = ParseFloat(tokens[index++]);
                if (index >= tokens.Count || char.IsLetter(tokens[index][0]))
                {
                    throw new FormatException("Incomplete coordinate pair in path data.");
                }
                float y = ParseFloat(tokens[index++]);

                if (isRelative)
                {
                    x += current.X;
                    y += current.Y;
                }

                points.Add(new PointF(x, y));
                consumedPairs++;

                if (expectedPairs.HasValue && consumedPairs >= expectedPairs.Value)
                {
                    break;
                }
            }

            return points;
        }

        private static PointF ParsePoint(List<string> tokens, ref int index, PointF current, bool isRelative)
        {
            float x = ParseFloat(tokens[index++]);
            if (index >= tokens.Count || char.IsLetter(tokens[index][0]))
            {
                throw new FormatException("Incomplete coordinate pair in path data.");
            }
            float y = ParseFloat(tokens[index++]);
            if (isRelative)
            {
                x += current.X;
                y += current.Y;
            }
            return new PointF(x, y);
        }

        private static float ParseFloat(string token)
        {
            if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new FormatException($"Invalid numeric value '{token}'.");
            }
            return value;
        }

        private static PointF ReflectPoint(PointF point, PointF around)
        {
            return new PointF(2 * around.X - point.X, 2 * around.Y - point.Y);
        }

        private static (PointF c1, PointF c2) QuadraticToCubic(PointF start, PointF control, PointF end)
        {
            var c1 = new PointF(
                start.X + (2f / 3f) * (control.X - start.X),
                start.Y + (2f / 3f) * (control.Y - start.Y));
            var c2 = new PointF(
                end.X + (2f / 3f) * (control.X - end.X),
                end.Y + (2f / 3f) * (control.Y - end.Y));
            return (c1, c2);
        }

        private static List<PathSegment> ArcToCubicBeziers(PointF start, PointF end, float rx, float ry, float angleDeg, bool largeArc, bool sweep)
        {
            var segments = new List<PathSegment>();
            if (Math.Abs(start.X - end.X) < 0.00001f && Math.Abs(start.Y - end.Y) < 0.00001f)
            {
                return segments;
            }

            if (rx == 0f || ry == 0f)
            {
                segments.Add(PathSegment.LineTo(end));
                return segments;
            }

            double angle = angleDeg * Math.PI / 180.0;
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);

            double dx2 = (start.X - end.X) / 2.0;
            double dy2 = (start.Y - end.Y) / 2.0;

            double x1Prime = cosAngle * dx2 + sinAngle * dy2;
            double y1Prime = -sinAngle * dx2 + cosAngle * dy2;

            rx = Math.Abs(rx);
            ry = Math.Abs(ry);

            double rxSq = rx * rx;
            double rySq = ry * ry;
            double x1PrimeSq = x1Prime * x1Prime;
            double y1PrimeSq = y1Prime * y1Prime;

            double lambda = x1PrimeSq / rxSq + y1PrimeSq / rySq;
            if (lambda > 1)
            {
                double scale = Math.Sqrt(lambda);
                rx *= (float)scale;
                ry *= (float)scale;
                rxSq = rx * rx;
                rySq = ry * ry;
            }

            double sign = (largeArc == sweep) ? -1 : 1;
            double coef = Math.Sqrt(
                Math.Max(0, (rxSq * rySq - rxSq * y1PrimeSq - rySq * x1PrimeSq) /
                (rxSq * y1PrimeSq + rySq * x1PrimeSq))) * sign;

            double cxPrime = coef * ((rx * y1Prime) / ry);
            double cyPrime = coef * (-(ry * x1Prime) / rx);

            double cx = cosAngle * cxPrime - sinAngle * cyPrime + (start.X + end.X) / 2.0;
            double cy = sinAngle * cxPrime + cosAngle * cyPrime + (start.Y + end.Y) / 2.0;

            double startVectorX = (x1Prime - cxPrime) / rx;
            double startVectorY = (y1Prime - cyPrime) / ry;
            double endVectorX = (-x1Prime - cxPrime) / rx;
            double endVectorY = (-y1Prime - cyPrime) / ry;

            double startAngle = VectorAngle(1, 0, startVectorX, startVectorY);
            double sweepAngle = VectorAngle(startVectorX, startVectorY, endVectorX, endVectorY);

            if (!sweep && sweepAngle > 0)
            {
                sweepAngle -= 2 * Math.PI;
            }
            else if (sweep && sweepAngle < 0)
            {
                sweepAngle += 2 * Math.PI;
            }

            int segmentsCount = (int)Math.Ceiling(Math.Abs(sweepAngle / (Math.PI / 2))); // quarter circle max per segment
            double deltaAngle = sweepAngle / segmentsCount;
            double t = 8.0 / 3.0 * Math.Sin(deltaAngle / 4.0) * Math.Sin(deltaAngle / 4.0) / Math.Sin(deltaAngle / 2.0);

            double currentAngle = startAngle;
            PointF currentPoint = start;

            for (int i = 0; i < segmentsCount; i++)
            {
                double nextAngle = currentAngle + deltaAngle;

                var cosCurrent = Math.Cos(currentAngle);
                var sinCurrent = Math.Sin(currentAngle);
                var cosNext = Math.Cos(nextAngle);
                var sinNext = Math.Sin(nextAngle);

                var endpoint = MapToEllipse(cosNext, sinNext, rx, ry, cosAngle, sinAngle, cx, cy);
                var control1 = MapToEllipse(
                    cosCurrent - t * sinCurrent,
                    sinCurrent + t * cosCurrent,
                    rx, ry, cosAngle, sinAngle, cx, cy);
                var control2 = MapToEllipse(
                    cosNext + t * sinNext,
                    sinNext - t * cosNext,
                    rx, ry, cosAngle, sinAngle, cx, cy);

                segments.Add(PathSegment.CubicBezier(control1, control2, endpoint));
                currentPoint = endpoint;
                currentAngle = nextAngle;
            }

            return segments;
        }

        private static double VectorAngle(double ux, double uy, double vx, double vy)
        {
            double dot = ux * vx + uy * vy;
            double len = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            double cos = Math.Clamp(dot / len, -1.0, 1.0);
            double angle = Math.Acos(cos);
            if (ux * vy - uy * vx < 0)
            {
                angle = -angle;
            }
            return angle;
        }

        private static PointF MapToEllipse(double x, double y, double rx, double ry, double cosAngle, double sinAngle, double cx, double cy)
        {
            double xNew = rx * x;
            double yNew = ry * y;
            double xp = cosAngle * xNew - sinAngle * yNew;
            double yp = sinAngle * xNew + cosAngle * yNew;
            return new PointF((float)(xp + cx), (float)(yp + cy));
        }
    }

    internal readonly struct PathSegment
    {
        public char Command { get; }
        public IReadOnlyList<PointF> Points { get; }

        public PathSegment(char command, IReadOnlyList<PointF> points)
        {
            Command = command;
            Points = points;
        }

        public static PathSegment MoveTo(PointF point) => new PathSegment('M', new[] { point });

        public static PathSegment LineTo(PointF point) => new PathSegment('L', new[] { point });

        public static PathSegment CubicBezier(PointF control1, PointF control2, PointF end)
        {
            return new PathSegment('C', new[] { control1, control2, end });
        }

        public static PathSegment ClosePath() => new PathSegment('Z', Array.Empty<PointF>());
    }
}
