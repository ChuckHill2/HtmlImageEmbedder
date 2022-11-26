# Html Image Embedder

Simple console app to convert image filenames in html into embedded base64 strings.
Required when external resource files are not possible.
Safely ignores images already embedded.
Other referenced non-image files are also ignored.

Usage: HtmlImageEmbedder.exe [-z] htmlfile<br/>
   -z = Compress to gzip.

NOTE: The html text encoding is expected to be UTF-8. The *WinWord* default html encoding is *Windows-1252*. This can be changed in the *WinWord* File->Options dialog box (left-bottom of page), click the Advanced category. At the bottom, click the 'Web Options...' button, and look at the Encoding tab.
Failure to change this will cause some characters to not be displayed correctly

This app is really useful when using the WinForms WebBrowser control. 
This is much more feature-laden than the old RichTextBox control.

## Build
This has been built with Microsoft Visual Studio 2019 with .Net Framework 4.8