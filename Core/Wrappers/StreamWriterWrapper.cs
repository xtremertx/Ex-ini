#if !(NETFX_45 || NETFX_451)

using System;
using System.IO;
using System.Text;

namespace eX_INI.Wrapper
{
    // StreamWriter in .NET < 4.5 is automatically closing BaseStream used, this behavior is unwanted behavior
    // because our Save(...) method is saving data to stream and this stream can be memory stream or network stream
    // http://referencesource.microsoft.com/#mscorlib/system/io/streamwriter.cs,221 (Hey its .NET 4.5.2, but our library will be .NET 3.5)
    // [NOTE] Dont worry about Save(...) method saving to a filename, its closing file stream properly!
    internal sealed class StreamWriterWrapper : StreamWriter
    {
        // We're using just this constructor now!
        public StreamWriterWrapper(Stream stream, Encoding encoding)
            : base(stream, (encoding == null) ? UTF8Encoding.UTF8 : encoding)
        { }

        // Dont Close() BaseStream (underlying stream), just close StreamWriter itself!
        protected override void Dispose(bool disposing)
        {
            base.Dispose(false);
        }
    }
}

#endif
