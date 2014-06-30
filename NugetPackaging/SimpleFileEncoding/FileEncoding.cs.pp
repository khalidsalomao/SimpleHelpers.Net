#region *   License     *
/*
    SimpleHelpers - ScriptEvaluator   

    Copyright © 2014 Khalid Salomão

    Permission is hereby granted, free of charge, to any person
    obtaining a copy of this software and associated documentation
    files (the “Software”), to deal in the Software without
    restriction, including without limitation the rights to use,
    copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
    OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
    OTHER DEALINGS IN THE SOFTWARE. 

    License: http://www.opensource.org/licenses/mit-license.php
    Website: https://github.com/khalidsalomao/SimpleHelpers.Net
 */
#endregion

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace $rootnamespace$.SimpleHelpers
{
    public class FileEncoding
    {
        const int DEFAULT_BUFFER_SIZE = 256 * 1024;

        public static bool HasByteOrderMarkUtf8 (string inputFilename)
        {
            using (var stream = System.IO.File.OpenRead (inputFilename))
            {
                var preamble = new System.Text.UTF8Encoding (true).GetPreamble ();
                var buffer = new byte[preamble.Length];
                var len = stream.Read (buffer, 0, buffer.Length);
                if (len < buffer.Length)
                    return false;

                for (var i = 0; i < preamble.Length; i++)
                    if (preamble[i] != buffer[i])
                        return false;
                return true;
            }
        }

        public static string DetectFileEncoding (string inputFilename)
        {
            using (var stream = new System.IO.FileStream (inputFilename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete, DEFAULT_BUFFER_SIZE))
            {
                return DetectFileEncoding (stream);
            }
        }

        public static string DetectFileEncoding (Stream inputStream)
        {
            // execute charset detector
            var ude = new Ude.CharsetDetector ();
            ude.Feed (inputStream);
            ude.DataEnd ();
            // return detected chaset
            return TreatDetectedCharset (ude.Charset);
        }

        public static string TreatDetectedCharset (string detectedCharset)
        {
            // in case of null charset, assume UTF-8 as default
            if (String.IsNullOrWhiteSpace (detectedCharset))
            {
                detectedCharset = "UTF-8";
            }
            // in case of ASCII, assume the more comprehesive ISO-8859-1 charset
            else if (detectedCharset == "ASCII")
            {
                detectedCharset = "ISO-8859-1";
            }

            return detectedCharset;
        }
    }
}
