using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

// https://en.wikipedia.org/wiki/Data_URI_scheme#Syntax
// <img src="data:{mimetype};base64,{base64-encoded-image}"

/// <summary>
/// Convert image filenames in html into embedded base64 string.
/// Required when external resource files are not possible.
/// </summary>
namespace HtmlImageEmbedder
{
    class Program
    {
        private static bool OpenedInExplorer = Console.CursorLeft == 0 && Console.CursorTop == 0;
        private static bool Gzipped = false;

        static void Main(string[] args)
        {
            var filename = ParseArgs(args);
            var dir = Path.GetDirectoryName(filename);

            //var encoding = Encoding.GetEncoding(1252); //Default WinWord saves html encoded with the Windows-1252 codepage!
            var encoding = Encoding.UTF8;
            var html = File.ReadAllText(filename,encoding);
            var prevdir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = dir; //Embedded resource files are relative to the html file.

            var html2 = Regex.Replace(html, @"(?<Y><img.+?src="")(?<X>[^""]+)""", match =>
             {
                 var f = match.Groups["X"].Value;
                 var value = match.Value;

                 if (f.StartsWith("data:")) return value; //already embedded

                 f = WebUtility.UrlDecode(f);
                 if (f.StartsWith("file://")) f = new Uri(f).LocalPath;
                 if (!File.Exists(f))
                 {
                     Console.WriteLine($"Warning: Image File \"{f}\" not found. Ignored.");
                     return value;
                 }

                 string mime = null;
                 try { mime = Registry.GetValue(@"HKEY_CLASSES_ROOT\" + Path.GetExtension(f), "Content Type", null) as string; } catch {}
                 if (string.IsNullOrEmpty(mime))
                 {
                     Console.WriteLine($"Warning: Image File \"{f}\" mime type not detected. Ignored.");
                     return value;
                 }

                 value = string.Concat(match.Groups["Y"].Value, "data:", mime, ";base64,", Convert.ToBase64String(File.ReadAllBytes(f)),"\"");
                 return value;
             }, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            Environment.CurrentDirectory = prevdir;

            if (Gzipped)
            {
                var fn = filename + ".gz";
                var newfn = filename+".bak.gz";
                if (File.Exists(fn))
                {
                    if (File.Exists(newfn)) File.Delete(newfn);
                    File.Move(fn, newfn);
                }

                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(html2)))
                using (var fs = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.None))
                using (var gz = new GZipStream(fs, CompressionMode.Compress))
                {
                    var preamble = Encoding.UTF8.GetPreamble();
                    gz.Write(preamble,0,preamble.Length);
                    ms.CopyTo(gz);
                }
            }
            else
            {
                var newfn = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".bak" + Path.GetExtension(filename));
                if (File.Exists(filename))
                {
                    if (File.Exists(newfn)) File.Delete(newfn);
                    File.Move(filename, newfn);
                }

                File.WriteAllText(filename, html2, Encoding.UTF8);
            }

            if (OpenedInExplorer)
            {
                Console.Write("Press any key to exit: ");
                Console.Read();
            }
        }

        private static string ParseArgs(string[] args)
        {
            string fn = null;
            if (args.Length == 0) Exit();
            for (int i=0; i<args.Length; i++)
            {
                var arg = args[i];
                if (arg[0] == '/' || arg[0] == '-')
                {
                    char option = arg.Length > 1 ? arg[1] : '\0';
                    if (option == 'Z' || option == 'z')
                    {
                        Gzipped = true;
                        continue;
                    }
                    else Exit($"Invalid Option {arg}");
                }

                if (fn==null) fn = arg;
            }
            if (fn == null) Exit("No file specified");

            try { fn = Path.GetFullPath(fn); } catch { Exit("Not a valid file path."); }
            if (!File.Exists(fn)) Exit("File Not found.");

            var ext = Path.GetExtension(fn);
            if (!ext.Equals(".htm", StringComparison.CurrentCultureIgnoreCase)
                && !ext.Equals(".html", StringComparison.CurrentCultureIgnoreCase)) Exit("Not an HTML file");

            return fn;
        }
        private static void Exit(string emsg=null)
        {
            Console.WriteLine();
            if (emsg!=null)
            {
                Console.Write("Error: ");
                Console.WriteLine(emsg);
            }

            Console.WriteLine(@"
Convert image filenames in html into embedded base64 string.
Required when external resource files are not possible.
Safely ignores images already embedded.
Other referenced non-image files are ignored.

Usage: HtmlImageEmbedder.exe [-z] htmlfile
       -z = Compress to gzip.

NOTE: The html text encoding is expected to be UTF-8. The WinWord default
html encoding is 'Windows-1252'. This can be changed in the WinWord File
->Options dialog box (left-bottom of page), click the Advanced category. At
the bottom, click the 'Web Options...' button, and look at the Encoding tab.
");
            if (OpenedInExplorer)
            {
                Console.Write("Press any key to exit: ");
                Console.Read();
            }
            Environment.Exit(1);
        }
    }
}
