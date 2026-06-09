using System.Globalization;
using System.Text;

namespace AdaptadorGEO.Geometry;

internal static class GeoWktParser
{
    public static GeoGeometry Parse(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            throw new FormatException("WKT text cannot be empty.");
        }

        var parser = new Parser(wkt);
        var geometry = parser.ParseGeometry();
        parser.ExpectEnd();
        return geometry;
    }

    private sealed class Parser
    {
        private readonly string _text;
        private int _index;

        public Parser(string text)
        {
            _text = text;
        }

        public GeoGeometry ParseGeometry()
        {
            SkipWhitespace();

            var type = ReadWord().ToUpperInvariant();

            return type switch
            {
                "POINT" => ParsePoint(),
                "LINESTRING" => ParseLineString(),
                "POLYGON" => ParsePolygon(),
                "MULTIPOINT" => ParseMultiPoint(),
                "MULTILINESTRING" => ParseMultiLineString(),
                "MULTIPOLYGON" => ParseMultiPolygon(),
                "GEOMETRYCOLLECTION" => ParseGeometryCollection(),
                _ => throw Error($"Unsupported WKT geometry type '{type}'.")
            };
        }

        public void ExpectEnd()
        {
            SkipWhitespace();

            if (!IsEnd)
            {
                throw Error("Unexpected trailing content.");
            }
        }

        private GeoPoint ParsePoint()
        {
            ExpectChar('(');
            var point = ReadPoint();
            ExpectChar(')');
            return point;
        }

        private LineString ParseLineString()
        {
            var points = ParsePointList();
            if (points.Count < 2)
            {
                throw Error("A LINESTRING must contain at least two points.");
            }

            return Create(() => new LineString(points));
        }

        private Polygon ParsePolygon()
        {
            ExpectChar('(');
            ExpectChar('(');
            var points = ParsePointListUntil(')');
            ExpectChar(')');

            if (points.Count < 4)
            {
                throw Error("A POLYGON outer ring must contain at least four points.");
            }

            return Create(() => new Polygon(points));
        }

        private MultiPoint ParseMultiPoint()
        {
            ExpectChar('(');
            var points = new List<GeoPoint>();

            if (TryConsumeChar(')'))
            {
                throw Error("A MULTIPOINT must contain at least one point.");
            }

            while (true)
            {
                SkipWhitespace();

                if (PeekChar() == '(')
                {
                    ExpectChar('(');
                    points.Add(ReadPoint());
                    ExpectChar(')');
                }
                else
                {
                    points.Add(ReadPoint());
                }

                SkipWhitespace();
                if (TryConsumeChar(','))
                {
                    continue;
                }

                ExpectChar(')');
                break;
            }

            return Create(() => new MultiPoint(points));
        }

        private MultiLineString ParseMultiLineString()
        {
            ExpectChar('(');
            var lines = new List<LineString>();

            if (TryConsumeChar(')'))
            {
                throw Error("A MULTILINESTRING must contain at least one line string.");
            }

            while (true)
            {
                ExpectChar('(');
                var points = ParsePointListUntil(')');
                lines.Add(Create(() => new LineString(points)));

                SkipWhitespace();
                if (TryConsumeChar(','))
                {
                    continue;
                }

                ExpectChar(')');
                break;
            }

            return Create(() => new MultiLineString(lines));
        }

        private MultiPolygon ParseMultiPolygon()
        {
            ExpectChar('(');
            var polygons = new List<Polygon>();

            if (TryConsumeChar(')'))
            {
                throw Error("A MULTIPOLYGON must contain at least one polygon.");
            }

            while (true)
            {
                ExpectChar('(');
                ExpectChar('(');
                var points = ParsePointListUntil(')');
                ExpectChar(')');
                polygons.Add(Create(() => new Polygon(points)));

                SkipWhitespace();
                if (TryConsumeChar(','))
                {
                    continue;
                }

                ExpectChar(')');
                break;
            }

            return Create(() => new MultiPolygon(polygons));
        }

        private GeometryCollection ParseGeometryCollection()
        {
            ExpectChar('(');
            var geometries = new List<GeoGeometry>();

            if (TryConsumeChar(')'))
            {
                throw Error("A GEOMETRYCOLLECTION must contain at least one geometry.");
            }

            while (true)
            {
                geometries.Add(ParseGeometry());

                SkipWhitespace();
                if (TryConsumeChar(','))
                {
                    continue;
                }

                ExpectChar(')');
                break;
            }

            return Create(() => new GeometryCollection(geometries));
        }

        private List<GeoPoint> ParsePointList()
        {
            ExpectChar('(');
            return ParsePointListUntil(')');
        }

        private List<GeoPoint> ParsePointListUntil(char closingChar)
        {
            var points = new List<GeoPoint>();

            if (TryConsumeChar(closingChar))
            {
                throw Error("A coordinate list cannot be empty.");
            }

            while (true)
            {
                points.Add(ReadPoint());

                SkipWhitespace();
                if (TryConsumeChar(','))
                {
                    continue;
                }

                ExpectChar(closingChar);
                break;
            }

            return points;
        }

        private GeoPoint ReadPoint()
        {
            var longitude = ReadDouble();
            var latitude = ReadDouble();
            return Create(() => new GeoPoint(latitude, longitude));
        }

        private double ReadDouble()
        {
            SkipWhitespace();

            var start = _index;
            if (PeekChar() is '+' or '-')
            {
                _index++;
            }

            while (!IsEnd)
            {
                var c = _text[_index];
                if (char.IsDigit(c) || c == '.')
                {
                    _index++;
                    continue;
                }

                if ((c == 'e' || c == 'E') && _index + 1 < _text.Length)
                {
                    _index++;
                    if (_text[_index] is '+' or '-')
                    {
                        _index++;
                    }

                    continue;
                }

                break;
            }

            var token = _text[start.._index];
            if (token.Length == 0 || !double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw Error($"Invalid numeric value '{token}'.");
            }

            return value;
        }

        private string ReadWord()
        {
            SkipWhitespace();

            var start = _index;
            while (!IsEnd && char.IsLetter(_text[_index]))
            {
                _index++;
            }

            if (start == _index)
            {
                throw Error("Expected a WKT geometry type.");
            }

            return _text[start.._index];
        }

        private void SkipWhitespace()
        {
            while (!IsEnd && char.IsWhiteSpace(_text[_index]))
            {
                _index++;
            }
        }

        private void ExpectChar(char expected)
        {
            SkipWhitespace();
            if (!TryConsumeChar(expected))
            {
                throw Error($"Expected '{expected}'.");
            }
        }

        private bool TryConsumeChar(char expected)
        {
            SkipWhitespace();
            if (!IsEnd && _text[_index] == expected)
            {
                _index++;
                return true;
            }

            return false;
        }

        private char PeekChar()
        {
            SkipWhitespace();
            return IsEnd ? '\0' : _text[_index];
        }

        private bool IsEnd => _index >= _text.Length;

        private FormatException Error(string message) =>
            new($"Invalid WKT at position {_index}: {message}");

        private static T Create<T>(Func<T> factory)
        {
            try
            {
                return factory();
            }
            catch (ArgumentException ex)
            {
                throw new FormatException(ex.Message, ex);
            }
        }
    }
}
