using System.Buffers.Binary;

namespace YggdrasilWebApp.Helpers;

public static class PngHeaderReader
{
	private static readonly byte[] PngSig = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

	// Читает только заголовок PNG (IHDR) и возвращает width/height
	public static (int width, int height) ReadDimensions(Stream stream)
	{
		if (!stream.CanSeek) throw new InvalidOperationException("Stream must be seekable.");

		long startPos = stream.Position;
		try
		{
			Span<byte> sig = stackalloc byte[8];
			if (stream.Read(sig) != 8) throw new InvalidDataException("Bad PNG (signature).");
			if (!sig.SequenceEqual(PngSig)) throw new InvalidDataException("Not a PNG file.");

			// Первый chunk в PNG должен быть IHDR:
			// length(4) + type(4) + data(13) + crc(4)
			Span<byte> lenBuf = stackalloc byte[4];
			if (stream.Read(lenBuf) != 4) throw new InvalidDataException("Bad PNG (chunk length).");
			int len = BinaryPrimitives.ReadInt32BigEndian(lenBuf);

			Span<byte> typeBuf = stackalloc byte[4];
			if (stream.Read(typeBuf) != 4) throw new InvalidDataException("Bad PNG (chunk type).");

			var type = System.Text.Encoding.ASCII.GetString(typeBuf);
			if (type != "IHDR") throw new InvalidDataException("Bad PNG (IHDR not found).");
			if (len < 8) throw new InvalidDataException("Bad PNG (IHDR too short).");

			Span<byte> ihdr = stackalloc byte[13];
			int toRead = Math.Min(13, len);
			if (stream.Read(ihdr[..toRead]) != toRead) throw new InvalidDataException("Bad PNG (IHDR data).");

			int width = BinaryPrimitives.ReadInt32BigEndian(ihdr.Slice(0, 4));
			int height = BinaryPrimitives.ReadInt32BigEndian(ihdr.Slice(4, 4));

			if (width <= 0 || height <= 0) throw new InvalidDataException("Bad PNG (invalid dimensions).");

			return (width, height);
		}
		finally
		{
			stream.Position = startPos;
		}
	}
}