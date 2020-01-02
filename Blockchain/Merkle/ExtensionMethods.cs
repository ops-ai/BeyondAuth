/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Clifton.Core.ExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool If<T>(this T v, Func<T, bool> predicate, Action<T> action)
        {
            var ret = predicate(v);

            if (ret)
            {
                action(v);
            }

            return ret;
        }

        public static bool If(this bool b, Action action)
        {
            if (b)
            {
                action();
            }

            return b;
        }

        public static void IfElse(this bool b, Action ifTrue, Action ifFalse)
        {
            if (b) ifTrue(); else ifFalse();
        }

        // Type is...
        public static bool Is<T>(this object obj, Action<T> action)
        {
            var ret = obj is T;

            if (ret)
            {
                action((T)obj);
            }

            return ret;
        }

        // ---------- if-then-else as lambda expressions --------------

        // If the test returns true, execute the action.
        // Works with objects, not value types.
        public static void IfTrue<T>(this T obj, Func<T, bool> test, Action<T> action)
        {
            if (test(obj))
            {
                action(obj);
            }
        }

        /// <summary>
        /// Returns true if the object is null.
        /// </summary>
        public static bool IfNull<T>(this T obj) => obj == null;

        /// <summary>
        /// If the object is null, performs the action and returns true.
        /// </summary>
        public static bool IfNull<T>(this T obj, Action action)
        {
            var ret = obj == null;

            if (ret) { action(); }

            return ret;
        }

        /// <summary>
        /// Returns true if the object is not null.
        /// </summary>
        public static bool IfNotNull<T>(this T obj) => obj != null;

        /// <summary>
        /// Return the result of the func if 'T is not null, passing 'T to func.
        /// </summary>
        public static R IfNotNullReturn<T, R>(this T obj, Func<T, R> func)
        {
            if (obj != null)
            {
                return func(obj);
            }
            else
            {
                return default(R);
            }
        }

        /// <summary>
        /// Return the result of func if 'T is null.
        /// </summary>
        public static R ElseIfNullReturn<T, R>(this T obj, Func<R> func)
        {
            if (obj == null)
            {
                return func();
            }
            else
            {
                return default(R);
            }
        }

        /// <summary>
        /// If the object is not null, performs the action and returns true.
        /// </summary>
        public static bool IfNotNull<T>(this T obj, Action<T> action)
        {
            var ret = obj != null;

            if (ret) { action(obj); }

            return ret;
        }

        /// <summary>
        /// If not null, return the evaluation of the function, otherwise return the default value.
        /// </summary>
        public static R IfNotNull<T, R>(this T obj, R defaultValue, Func<T, R> func)
        {
            var ret = defaultValue;

            if (obj != null)
            {
                ret = func(obj);
            }

            return ret;
        }

        /// <summary>
        /// If the boolean is true, performs the specified action.
        /// </summary>
        public static bool Then(this bool b, Action f)
        {
            if (b) { f(); }

            return b;
        }

        /// <summary>
        /// If the boolean is false, performs the specified action and returns the complement of the original state.
        /// </summary>
        public static void Else(this bool b, Action f)
        {
            if (!b) { f(); }
        }

        // ---------- Dictionary --------------

        /// <summary>
        /// Return the key for the dictionary value or throws an exception if more than one value matches.
        /// </summary>
        public static TKey KeyFromValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue val)
        {
            // from: http://stackoverflow.com/questions/390900/cant-operator-be-applied-to-generic-types-in-c
            // "Instead of calling Equals, it's better to use an IComparer<T> - and if you have no more information, EqualityComparer<T>.Default is a good choice: Aside from anything else, this avoids boxing/casting."
            return dict.Single(t => EqualityComparer<TValue>.Default.Equals(t.Value, val)).Key;
        }

        // ---------- DBNull value --------------

        // Note the "where" constraint, only value types can be used as Nullable<T> types.
        // Otherwise, we get a bizzare error that doesn't really make it clear that T needs to be restricted as a value type.
        public static object AsDBNull<T>(this Nullable<T> item) where T : struct
        {
            // If the item is null, return DBNull.Value, otherwise return the item.
            return item as object ?? DBNull.Value;
        }

        // ---------- ForEach iterators --------------

        /// <summary>
        /// Implements a ForEach for generic enumerators.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }

        /// <summary>
        /// ForEach with an index.
        /// </summary>
        public static void ForEachWithIndex<T>(this IEnumerable<T> collection, Action<T, int> action)
        {
            var n = 0;

            foreach (var item in collection)
            {
                action(item, n++);
            }
        }

        /// <summary>
        /// Implements ForEach for non-generic enumerators.
        /// </summary>
        // Usage: Controls.ForEach<Control>(t=>t.DoSomething());
        public static void ForEach<T>(this IEnumerable collection, Action<T> action)
        {
            foreach (T item in collection)
            {
                action(item);
            }
        }

        public static T Single<T>(this IEnumerable collection, Func<T, bool> expr)
        {
            var ret = default(T);
            var found = false;

            foreach (T item in collection)
            {
                if (expr(item))
                {
                    ret = item;
                    found = true;
                    break;
                }
            }

            found.Else(() => { throw new ApplicationException("Collection does not contain item in qualifier."); });

            return ret;
        }

        public static bool Contains<T>(this IEnumerable collection, Func<T, bool> expr)
        {
            var found = false;

            foreach (T item in collection)
            {
                if (expr(item))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        public static T SingleOrDefault<T>(this IEnumerable collection, Func<T, bool> expr)
        {
            var ret = default(T);

            foreach (T item in collection)
            {
                if (expr(item))
                {
                    ret = item;
                    break;
                }
            }

            return ret;
        }

        public static void ForEach(this DataTable dt, Action<DataRow> action)
        {
            foreach (DataRow row in dt.Rows)
            {
                action(row);
            }
        }

        public static void ForEach(this DataView dv, Action<DataRowView> action)
        {
            foreach (DataRowView drv in dv)
            {
                action(drv);
            }
        }

        /// <summary>
        /// Returns a new dictionary having merged the two source dictionaries.
        /// </summary>
        public static Dictionary<T, U> Merge<T, U>(this Dictionary<T, U> dict1, Dictionary<T, U> dict2) => (new[] { dict1, dict2 }).SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);

        // ---------- events --------------

        /// <summary>
        /// Encapsulates testing for whether the event has been wired up.
        /// </summary>
        public static void Fire<TEventArgs>(this EventHandler<TEventArgs> theEvent, object sender, TEventArgs e = null) where TEventArgs : EventArgs
        {
            if (theEvent != null)
            {
                theEvent(sender, e);
            }
        }

        /// <summary>
        /// Encapsulates testing for whether the event has been wired up.
        /// </summary>
        public static void Fire<TEventArgs>(this PropertyChangedEventHandler theEvent, object sender, TEventArgs e) where TEventArgs : PropertyChangedEventArgs
        {
            if (theEvent != null)
            {
                theEvent(sender, e);
            }
        }

        // ---------- collection management --------------

        // From the comments of the blog entry http://blog.jordanterrell.com/post/LINQ-Distinct()-does-not-work-as-expected.aspx regarding why Distinct doesn't work right.
        public static IEnumerable<T> RemoveDuplicates<T>(this IEnumerable<T> source) => RemoveDuplicates(source, (t1, t2) => t1.Equals(t2));

        public static IEnumerable<T> RemoveDuplicates<T>(this IEnumerable<T> source, Func<T, T, bool> equater)
        {
            // copy the source array 
            var result = new List<T>();

            foreach (var item in source)
            {
                if (result.All(t => !equater(item, t)))
                {
                    // Doesn't exist already: Add it 
                    result.Add(item);
                }
            }

            return result;
        }

        public static IEnumerable<T> Replace<T>(this IEnumerable<T> source, T newItem, Func<T, T, bool> equater)
        {
            var result = new List<T>();

            foreach (var item in source)
            {
                if (!equater(item, newItem))
                {
                    result.Add(item);
                }
            }

            result.Add(newItem);

            return result;
        }

        public static void AddIfUnique<T>(this IList<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }

        public static void AddIfUnique<T>(this IList<T> list, T item, Func<T, bool> comparer)
        {
            if (!list.Contains(comparer))
            {
                list.Add(item);
            }
        }

        public static void RemoveLast<T>(this IList<T> list) => list.RemoveAt(list.Count - 1);

        // ---------- List to DataTable --------------

        // From http://stackoverflow.com/questions/564366/generic-list-to-datatable
        // which also suggests, for better performance, HyperDescriptor: http://www.codeproject.com/Articles/18450/HyperDescriptor-Accelerated-dynamic-property-acces
        public static DataTable AsDataTable<T>(this IList<T> data)
        {
            var props = TypeDescriptor.GetProperties(typeof(T));
            var table = new DataTable();

            for (var i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                table.Columns.Add(prop.Name, prop.PropertyType);
            }

            var values = new object[props.Count];

            foreach (var item in data)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = props[i].GetValue(item);
                }
                table.Rows.Add(values);
            }

            return table;
        }

        public static bool IsEmpty(this string s) => s == string.Empty;
#if INCLUDE_DRAWING
        public static Image Resize(this Image image, int maxWidth)
        {
            // To encode as JPG, see http://stackoverflow.com/questions/10894836/c-sharp-convert-image-formats-to-jpg
            // scale the height to the desired width.
            int height = image.Height * maxWidth / image.Width;
            return (Image)(new Bitmap(image, new Size(maxWidth, height)));

            // High quality conversion.  see http://stackoverflow.com/questions/1922040/resize-an-image-c-sharp
            // But CompositingMode isn't resolving even though I have .NET 4.5 selected.
            /*
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Image destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
             */
        }

        public static string ToBase64(this Image image)
        {
            string base64String = String.Empty;

            if (image != null)
            {
                // For some reason, the RawFormat converter sometimes throws an exception, so we are saving to a file.
                image.Save("id2.jpg", ImageFormat.Jpeg);
                byte[] data = File.ReadAllBytes("id2.jpg");
                base64String = Convert.ToBase64String(data);

                /*
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();
                    // Convert byte[] to Base64 String
                    base64String = Convert.ToBase64String(imageBytes);
                }
                 */
            }

            return base64String;
        }

        /// Returns just the RGB component of a color.
        public static int ToRgb(this Color color)
        {
            return color.ToArgb() & 0x00FFFFFF;
        }

#endif

        /// <summary>
        /// Returns the smaller of the two values.
        /// </summary>
        public static int Smaller(this int val, int otherVal) => val < otherVal ? val : otherVal;

        /// <summary>
        /// Returns the larger of the two values.
        /// </summary>
        public static int Larger(this int val, int otherVal) => val > otherVal ? val : otherVal;

        /// <summary>
        /// Converts a DateTime to Posix time.
        /// </summary>
        /// <remarks>
        /// Posix time (aka Unix time) is the number of seconds elapsed since midnight, 1970-Jan-01.
        /// </remarks>
        public static double ToPosix(this DateTime date)
        {
            var start = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

            return (date - start).TotalSeconds;
        }

        /// <summary>
        /// Converts a Posix time to a DateTime.
        /// </summary>
        /// <remarks>
        /// Posix time (aka Unix time) is the number of seconds elapsed since midnight, 1970-Jan-01.
        /// </remarks>
        public static DateTime FromPosix(this double posix)
        {
            var start = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);

            return start.AddSeconds(posix).ToLocalTime();
        }

        /// <summary>
        /// Takes a Posix time as a string and returns it as a string in the format "MM-dd-yy".
        /// </summary>
        public static string DateFromPosix(this string s, string format = "MM-dd-yy")
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(s.to_l());

            return dt.ToString(format);
        }

        /// <summary>
        /// Takes a Posix time as a long and returns it as a string in the format "MM-dd-yy".
        /// </summary>      
        public static string DateFromPosix(this long l, string format = "MM-dd-yy")
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(l);

            return dt.ToString(format);
        }

        /// <summary>
        /// Converts an integer to a string.
        /// </summary>
        public static string to_s(this int i) => i.ToString();

        /// <summary>
        /// Converts a string to a long (Int64). If the passed-in string is null or
        /// empty, zero is returned.
        /// </summary>
        public static long to_l(this string src)
        {
            long ret = 0;

            if (!string.IsNullOrEmpty(src))
            {
                ret = Convert.ToInt64(src);
            }

            return ret;
        }

        /// <summary>
        /// Converts a string to an int (Int32).
        /// </summary>
        public static int to_i(this string src) => Convert.ToInt32(src);

        public static bool IsInt(this string src)
        {
            var ret = false;

            if (!string.IsNullOrEmpty(src))
            {
                ret = int.TryParse(src, out var n);
            }

            return ret;
        }

        /// <summary>
        /// Converts a string to a boolean.
        /// </summary>
        public static bool to_b(this string src) => Convert.ToBoolean(src);

        /// <summary>
        /// Converts a string to a float.
        /// </summary>
        public static float to_f(this string src) => (float)Convert.ToDouble(src);

        /// <summary>
        /// Converts a string to a double.
        /// </summary>
        public static double to_d(this string src) => Convert.ToDouble(src);

        /// <summary>
        /// Converts a string to a decimal.
        /// </summary>
        public static decimal to_dec(this string src) => Convert.ToDecimal(src);

        public static T ToEnum<T>(this string src)
        {
            var enumVal = (T)Enum.Parse(typeof(T), src);

            return enumVal;
        }

        /// <summary>
        /// Converts an object to a string. If the passed-in object is null, an
        /// empty string is returned.
        /// </summary>
        public static string SafeToString(this object src)
        {
            var ret = string.Empty;

            if (src != null)
            {
                ret = src.ToString();
            }

            return ret;
        }

        /// <summary>
        /// Returns true if the passed-in string can be converted successfully to an Int32; 
        /// otherwise, false.
        /// </summary>
        /// <remarks>
        /// The TryParse method is like the Parse method, except the TryParse method does not 
        /// throw an exception if the conversion fails.
        /// </remarks>
        public static bool IsInt32(this string src)
        {
            // The result variable will contain the 32-bit signed integer value equivalent 
            // of the number contained in src, if the conversion succeeded, or zero if the 
            // conversion failed.
            var ret = int.TryParse(src, out var result);

            return ret;
        }

        /// <summary>
        /// Replaces each quotation mark (") with a single quote (').
        /// </summary>
        public static string ParseQuote(this string src) => src.Replace("\"", "'");

        /// <summary>
        /// Replaces each single quote (') with two single quotes ('').
        /// </summary>
        public static string ParseSingleQuote(this string src) => src.Replace("'", "''");

        /// <summary>
        /// Returns the passed-in string surrounded by single quotes.
        /// </summary>
        public static string SingleQuote(this string src) => "'" + src + "'";

        /// <summary>
        /// Returns the passed-in string surrounded by quotation marks (").
        /// </summary>
        public static string Quote(this string src) => "\"" + src + "\"";

        /// <summary>
        /// Exchanges ' for " and " for ' then escapes the quotation marks (each /0xFF
        /// becomes \").
        /// Javascript JSON support, which must be formatted like '{"foo":"bar"}'
        /// </summary>
        public static string ExchangeQuoteSingleQuote(this string src)
        {
            var ret = src.Replace("'", "\0xFF");
            ret = ret.Replace("\"", "'");
            ret = ret.Replace("\0xFF", "\"");

            return ret;
        }

        /// <summary>
        /// Returns the source string surrounded by a single whitespace.
        /// </summary>
        public static string Spaced(this string src) => " " + src + " ";

        /// <summary>
        /// Returns the source string surrounded by parentheses.
        /// </summary>
        public static string Parens(this string src) => "(" + src + ")";

        /// <summary>
        /// Returns the source string surrounded by brackets.
        /// </summary>
        public static string Brackets(this string src) => "[" + src + "]";

        /// <summary>
        /// Returns the source string surrounded by braces.
        /// </summary>
        public static string CurlyBraces(this string src) => "{" + src + "}";

        public static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }

        /// <summary>
        /// Returns everything between the start-character and the first occurence
        /// of the end-character, after the start-character, exclusive.
        /// </summary>
        /// <param name="src">The source string.</param>
        /// <param name="start">The first char to find.</param>
        /// <param name="end">The end char to find.</param>
        /// <returns>The string between the start and end chars, or an empty string if 
        /// the start char or end char is not found.</returns>
        public static string Between(this string src, char start, char end)
        {
            var ret = string.Empty;
            var idxStart = src.IndexOf(start);

            if (idxStart != -1)
            {
                ++idxStart;
                var idxEnd = src.IndexOf(end, idxStart);

                if (idxEnd != -1)
                {
                    ret = src.Substring(idxStart, idxEnd - idxStart);
                }
            }

            return ret;
        }

        /// <summary>
        /// Returns everything between the start-string and the first occurence
        /// of the end-string, after the start-string, exclusive.
        /// </summary>
        /// <param name="src">The source string.</param>
        /// <param name="start">The first string to find.</param>
        /// <param name="end">The end string to find.</param>
        /// <returns>The string between the start and end strings, or an empty string if 
        /// the start string or end string is not found.</returns>
        public static string Between(this string src, string start, string end)
        {
            var ret = string.Empty;
            var idxStart = src.IndexOf(start);

            if (idxStart != -1)
            {
                idxStart += start.Length;
                var idxEnd = src.IndexOf(end, idxStart);

                if (idxEnd != -1)
                {
                    ret = src.Substring(idxStart, idxEnd - idxStart);
                }
            }

            return ret;
        }

        /// <summary>
        /// Returns everything between the start-character and the last occurence
        /// of the end-character, after the start-character, exclusive.
        /// </summary>
        /// <param name="src">The source string.</param>
        /// <param name="start">The first char to find.</param>
        /// <param name="end">The end char to find.</param>
        /// <returns>The string between the start char and the last occurence of the
        /// end char, or an empty string if the start char or end char is not found.</returns>
        public static string BetweenEnds(this string src, char start, char end)
        {
            var ret = string.Empty;
            var idxStart = src.IndexOf(start);

            if (idxStart != -1)
            {
                ++idxStart;
                var idxEnd = src.LastIndexOf(end);

                if (idxEnd != -1)
                {
                    ret = src.Substring(idxStart, idxEnd - idxStart);
                }
            }

            return ret;
        }

        /// <summary>
        /// Returns the number of occurances of a character (find) within a string (src).
        /// </summary>
        /// <param name="src">The source string.</param>
        /// <param name="find">The search char.</param>
        /// <returns>The # of times the char occurs in the search string.</returns>
        public static int Count(this string src, char find)
        {
            var ret = 0;

            foreach (var s in src)
            {
                if (s == find)
                {
                    ++ret;
                }
            }

            return ret;
        }

        /// <summary>
        /// Return a new string that is "around" (left of and right of) the specified string.
        /// Only the first occurance is processed.
        /// </summary>
        /// <remarks>
        /// Alt summary: Removes the first occurance of a string (s) inside another string (src)
        /// and returns the altered src. [If s is not found, src is returned unchanged?]
        /// </remarks>
        public static string Surrounding(this string src, string s) => src.LeftOf(s) + src.RightOf(s);

        /// <summary>
        /// Returns true if a string (src) starts with the specified string (s); otherwise
        /// returns false.
        /// </summary>
        public static bool BeginsWith(this string src, string s) => src.StartsWith(s);

        /// <summary>
        /// Returns the portion of a string (src) that is to the right of the first occurance 
        /// of the specified character (c), (i.e. excluding c). Returns an empty string if the 
        /// specified character is not found.
        /// </summary>
        public static string RightOf(this string src, char c)
        {
            var ret = string.Empty;
            var idx = src.IndexOf(c);

            if (idx != -1)
            {
                ret = src.Substring(idx + 1);
            }

            return ret;
        }

        /// <summary>
        /// Returns the portion of a string (src) that is to the right of the first occurance 
        /// of the specified string (s), (i.e. excluding s). Returns an empty string if the 
        /// specified string is not found.
        /// </summary>
        public static string RightOf(this string src, string s)
        {
            var ret = string.Empty;
            var idx = src.IndexOf(s);

            if (idx != -1)
            {
                ret = src.Substring(idx + s.Length);
            }

            return ret;
        }

        /// <summary>
        /// Returns everything to the right of the rightmost char c.
        /// </summary>
        /// <param name="src">The source string.</param>
        /// <param name="c">The search char.</param>
        /// <returns>Returns everything to the right of the rightmost search char, or an empty string.</returns>
        public static string RightOfRightmostOf(this string src, char c)
        {
            var ret = string.Empty;
            var idx = src.LastIndexOf(c);

            if (idx != -1)
            {
                ret = src.Substring(idx + 1);
            }

            return ret;
        }

        /// <summary>
        /// Left of the first occurance of c.
        /// </summary>
        /// <param name="src">The source string.</param>
        /// <param name="c">Return everything to the left of this character.</param>
        /// <returns>String to the left of c, or the entire string.</returns>
        public static string LeftOf(this string src, char c)
        {
            var ret = src;
            var idx = src.IndexOf(c);

            if (idx != -1)
            {
                ret = src.Substring(0, idx);
            }

            return ret;
        }

        public static string LeftOf(this string src, string s)
        {
            var ret = src;
            var idx = src.IndexOf(s);

            if (idx != -1)
            {
                ret = src.Substring(0, idx);
            }

            return ret;
        }

        /// <summary>
        /// Returns null if the string is null or empty; otherwise returns the string.
        /// </summary>
        public static string NullIfEmpty(this string src) => string.IsNullOrEmpty(src) ? null : src;

        /// <summary>
        /// Safe left of n chars. If string length is less than n, returns string;
        /// otherwise returns the first n chars of string.
        /// </summary>
        public static string LeftOf(this string src, int n) => src.Length < n ? src : src.Substring(n);

        public static string LeftOfRightmostOf(this string src, char c)
        {
            var ret = src;
            var idx = src.LastIndexOf(c);

            if (idx != -1)
            {
                ret = src.Substring(0, idx);
            }

            return ret;
        }

        public static string LeftOfRightmostOf(this string src, string s)
        {
            var ret = src;
            var idx = src.IndexOf(s);
            var idx2 = idx;

            while (idx2 != -1)
            {
                idx2 = src.IndexOf(s, idx + s.Length);

                if (idx2 != -1)
                {
                    idx = idx2;
                }
            }

            if (idx != -1)
            {
                ret = src.Substring(0, idx);
            }

            return ret;
        }

        public static string RightOfRightmostOf(this string src, string s)
        {
            var ret = src;
            var idx = src.IndexOf(s);
            var idx2 = idx;

            while (idx2 != -1)
            {
                idx2 = src.IndexOf(s, idx + s.Length);

                if (idx2 != -1)
                {
                    idx = idx2;
                }
            }

            if (idx != -1)
            {
                ret = src.Substring(idx + s.Length, src.Length - (idx + s.Length));
            }

            return ret;
        }

        public static char Rightmost(this string src)
        {
            var c = '\0';

            if (src.Length > 0)
            {
                c = src[src.Length - 1];
            }

            return c;
        }

        /// <summary>
        /// Returns the passed-in string, with the last character removed. If the
        /// passed-in string has one or zero characters, an empty string is returned.
        /// </summary>
        public static string TrimLastChar(this string src)
        {
            var ret = string.Empty;
            var len = src.Length;

            if (len > 0)
            {
                ret = src.Substring(0, len - 1);
            }

            return ret;
        }

        /// <summary>
        /// Returns true if a string is null or empty, otherwise returns false. 
        /// The passed-in string is tested without alteration, then it is trimmed and 
        /// tested. </summary>
        public static bool IsBlank(this string src) => string.IsNullOrEmpty(src) || (src.Trim() == string.Empty);

        /// <summary>
        /// Loops through an array of strings and returns the one which is contained
        /// in another string (the first argument) and is also closest to the start 
        /// of the other string. Returns an empty string if none of the strings from 
        /// the array are found in the other string.
        /// </summary>
        public static string Contains(this string src, string[] tokens)
        {
            var ret = string.Empty;
            var firstIndex = 9999;

            // Find the index of the first index encountered.
            foreach (var token in tokens)
            {
                var idx = src.IndexOf(token);

                if ((idx != -1) && (idx < firstIndex))
                {
                    ret = token;
                    firstIndex = idx;
                }
            }

            return ret;
        }

        // http://stackoverflow.com/questions/8868119/find-all-parent-types-both-base-classes-and-interfaces
        public static IEnumerable<Type> GetParentTypes(this Type type)
        {
            // is there any base type?
            if ((type == null) || (type.BaseType == null))
            {
                yield break;
            }

            // return all implemented or inherited interfaces
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }

            // return all inherited types
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }

        public static byte[] FromBase64(this char[] data) => Convert.FromBase64CharArray(data, 0, data.Length);

        public static byte[] FromBase64(this string data) => Convert.FromBase64String(data);

        public static string ToBase64(this byte[] data) => Convert.ToBase64String(data);

        public static byte[] to_Utf8(this string str) => Encoding.UTF8.GetBytes(str);

        // From here: http://www.codeproject.com/Articles/769741/Csharp-AES-bits-Encryption-Library-with-Salt
        public static string Encrypt(this string toEncrypt, string password, string salt)
        {
            byte[] encryptedBytes = null;

            var saltBytes = Encoding.ASCII.GetBytes(salt);
            var passwordBytes = Encoding.ASCII.GetBytes(password);
            var encryptBytes = Encoding.ASCII.GetBytes(toEncrypt);

            using (var ms = new MemoryStream())
            {
                using (var AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC; // Cipher Block Chaining.

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptBytes, 0, encryptBytes.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes.ToBase64();
        }

        public static string Decrypt(this string base64, string password, string salt)
        {
            string decryptedBytes = null;
            var saltBytes = Encoding.ASCII.GetBytes(salt);
            var passwordBytes = Encoding.ASCII.GetBytes(password);
            var decryptBytes = base64.FromBase64();

            using (var ms = new MemoryStream())
            {
                using (var AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC; // Cipher Block Chaining.

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(decryptBytes, 0, decryptBytes.Length);
                        cs.Close();
                    }

                    decryptedBytes = Encoding.Default.GetString(ms.ToArray());
                }
            }

            return decryptedBytes;
        }

        public static string SplitCamelCase(this string input)
        {
            // return Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
            // Replaced, because the version below also handles strings like "IBMMakeStuffAndSellIt", converting it to "IBM Make Stuff And Sell It"
            // See http://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
            return Regex.Replace(
                Regex.Replace(input, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"),
                    @"(\p{Ll})(\P{Ll})", "$1 $2");
        }

        /// <summary>
        /// Converts the first letter of the given string to lowercase. The rest 
        /// of the string remains unchanged.
        /// </summary>
        /// <param name="src"></param>
        /// <returns>string</returns>
        public static string CamelCase(this string src) => src[0].ToString().ToLower() + src.Substring(1);

        /// <summary>
        /// Converts the first letter of the given string to uppercase. The rest 
        /// of the string remains unchanged.
        /// </summary>
        /// <remarks>
        /// Currently used as a utility for the method PascalCaseWords().
        /// </remarks>
        /// <param name="src"></param>
        /// <returns>string</returns>
        public static string PascalCase(this string src)
        {
            var ret = string.Empty;

            if (!string.IsNullOrEmpty(src))
            {
                ret = src[0].ToString().ToUpper() + src.Substring(1);
            }

            return ret;
        }

        /// <summary>
        /// Returns a Pascal-cased string, i.e. the first letter of each word is in 
        /// uppercase, including the first word. The string is otherwise unchanged; 
        /// spaces and punctuation are preserved.
        /// </summary>
        /// <param name="src"></param>
        /// <returns>string</returns>
        public static string PascalCaseWords(this string src)
        {
            var sb = new StringBuilder();
            var s = src.Split(' ');
            var more = string.Empty;

            foreach (var s1 in s)
            {
                sb.Append(more);
                sb.Append(PascalCase(s1));
                more = " ";
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns true or false as to whether the first argument is greater
        /// than or equal to the second argument, and less than or equal to the
        /// third argument.
        /// </summary>
        /// <remarks>
        /// The name of this method is a bit misleading. It doesn't test only for
        /// 'between'.
        /// </remarks>
        /// <param name="b">The integer to test.</param>
        /// <param name="a">The integer that should be less than or equal to the test.</param>
        /// <param name="c">The integer that should be greater than or equal to the test.</param>
        /// <returns>bool</returns>
        public static bool Between(this int b, int a, int c) => b >= a && b <= c;

        /// <summary>
        /// Value cannot exceed max, otherwise max is returned.
        /// </summary>
        public static int Min(this int a, int max) => (a > max) ? max : a;

        public static int MinDelta(this int a, int delta) => a > a + delta ? a + delta : a;

        /// <summary>
        /// Value cannot be less than min, otherwise min is returned.
        /// </summary>
        public static int Max(this int a, int min) => (a < min) ? min : a;

        public static int MaxDelta(this int a, int delta) => a < a + delta ? a + delta : a;

        public static void Until(this int start, int max, Action<int> action)
        {
            for (var i = start; i < max; i++) action(i);
        }

        /// <summary>
        /// Returns iterable enumerator of all indices of the substring occurring in source.
        /// </summary>
        public static IEnumerable<int> AllIndexesOf(this string source, string substring)
        {
            for (var index = 0; ; index += substring.Length)
            {
                index = source.IndexOf(substring, index);

                if (index == -1)
                {
                    break;
                }

                yield return index;
            }
        }

        public static int CountOf(this string src, char c) => src.Count(q => q == c);

        public static T AsNotNull<T>(this T? src) where T : struct => (T)src;
    }
}