using image_nintendo.CGFX;
using Kontract.Image;
using Kontract.Image.Format;
using Kontract.Image.Swizzle;
using Kontract.Interface;
using Kontract.IO;
using pk3DS.Core;
using pk3DS.Core.CTR;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PCTRGen6Tools;

internal partial class Helper
{
    private static byte[] EncodePixels(Bitmap image, IImageFormat format, byte orientation = 0)
    {
        var width = Math.Max(8, (int)BitOperations.RoundUpToPowerOf2((uint)image.Width));
        var height = Math.Max(8, (int)BitOperations.RoundUpToPowerOf2((uint)image.Height));
        using var padded = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var rectangle = new Rectangle(0, 0, image.Width, image.Height);
        var sourceData = image.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var paddedData = padded.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        var row = new byte[image.Width * 4];
        for (var y = 0; y < image.Height; y++)
        {
            Marshal.Copy(IntPtr.Add(sourceData.Scan0, y * sourceData.Stride), row, 0, row.Length);
            Marshal.Copy(row, 0, IntPtr.Add(paddedData.Scan0, y * paddedData.Stride), row.Length);
        }
        image.UnlockBits(sourceData);
        padded.UnlockBits(paddedData);
        return Common.Save(padded, new ImageSettings
        {
            Width = width,
            Height = height,
            Format = format,
            Swizzle = new CTRSwizzle(width, height, orientation),
        });
    }

    private static byte[] EncodeImage(string source, byte[] original)
    {
        var header = (CLIMHeader)new BCLIM(original).Footer;
        IImageFormat format = header.Format switch
        {
            XLIMEncoding.L8 => new LA(8, 0),
            XLIMEncoding.A8 => new LA(0, 8),
            XLIMEncoding.LA4 => new LA(4, 4),
            XLIMEncoding.LA8 => new LA(8, 8),
            XLIMEncoding.HILO8 => new HL(8, 8),
            XLIMEncoding.RGB565 => new RGBA(5, 6, 5),
            XLIMEncoding.RGBX8 => new RGBA(8, 8, 8),
            XLIMEncoding.RGB5A1 => new RGBA(5, 5, 5, 1),
            XLIMEncoding.RGBA4 => new RGBA(4, 4, 4, 4),
            XLIMEncoding.RGBA8 => new RGBA(8, 8, 8, 8),
            XLIMEncoding.ETC1 => new ETC1(),
            XLIMEncoding.ETC1A4 => new ETC1(true),
            XLIMEncoding.L4 => new LA(4, 0),
            XLIMEncoding.A4 => new LA(0, 4),
            _ => throw new NotSupportedException($"Unsupported BCLIM texture format: {header.Format}"),
        };
        using var image = new Bitmap(source);
        var pixels = EncodePixels(image, format, (byte)header.Orientation);
        header.Width = (ushort)image.Width;
        header.Height = (ushort)image.Height;
        header.DataSize = (uint)pixels.Length;
        header.totalLength = (uint)pixels.Length + CLIMHeader.SIZE;
        return [.. pixels, .. StructConverter.ToBytes(header)];
    }

    private static IEnumerable<(string Path, int Index)> GetDarcImages(DARC darc)
    {
        for (var i = 2; i < darc.Entries.Length;)
        {
            var directory = darc.FileNameTable[i].FileName;
            var end = (int)darc.Entries[i++].DataLength;
            for (; i < end; i++)
            {
                var name = darc.FileNameTable[i].FileName;
                if (Path.GetExtension(name) == ".bclim")
                {
                    yield return (Path.Combine(directory, name), i);
                }
            }
        }
    }

    public static void ImportImages(string inputRoot, string replaceRoot, string outputRoot)
    {
        if (Directory.GetDirectories(inputRoot).Length > 0)
        {
            foreach (var filePath in Directory.GetFiles(inputRoot, "*", SearchOption.AllDirectories))
            {
                var output = Path.Combine(outputRoot, Path.GetRelativePath(inputRoot, filePath));
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.Copy(filePath, output, true);
            }
        }
        var members = Directory.GetFiles(replaceRoot, "*.png", SearchOption.AllDirectories)
            .Select(path => Path.GetDirectoryName(path)!).Distinct().Order();
        foreach (var member in members)
        {
            var relativePath = $"{Path.GetRelativePath(replaceRoot, member)}.bin";
            var binary = File.ReadAllBytes(Path.Combine(inputRoot, relativePath));
            var position = DARC.GetDARCposition(binary);
            var darc = new DARC(binary[position..]);
            foreach (var (path, index) in GetDarcImages(darc))
            {
                var source = Path.Combine(member, $"{Path.GetFileNameWithoutExtension(path)}.png");
                if (!File.Exists(source))
                {
                    continue;
                }
                var entry = darc.Entries[index];
                var offset = (int)(entry.DataOffset - darc.Header.FileDataOffset);
                var original = darc.Data.AsSpan(offset, (int)entry.DataLength).ToArray();
                if (!DARC.InsertFile(ref darc, index, EncodeImage(source, original)))
                {
                    throw new InvalidDataException($"DARC image replacement failed: {source}");
                }
            }
            var destination = Path.Combine(outputRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllBytes(destination, [.. binary[..position], .. DARC.SetDARC(darc)]);
        }
    }

    public static void ImportBanner(string inputRoot, string replaceRoot, string outputRoot)
    {
        Directory.CreateDirectory(outputRoot);
        foreach (var model in Directory.GetFiles(inputRoot, "*.bcmdl").Order())
        {
            var scene = new CGFX(File.OpenRead(model));
            var textures = (List<TxobEntry>)typeof(CGFX).GetField("TxObs", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(scene)!;
            var texturesByName = new Dictionary<string, TxobEntry>();
            using (var names = new BinaryReaderX(File.OpenRead(model)))
            {
                foreach (var texture in textures)
                {
                    names.BaseStream.Position = texture.nameOffset;
                    texturesByName[names.ReadCStringA()] = texture;
                }
            }
            var binary = File.ReadAllBytes(model);
            foreach (var (name, texture) in texturesByName)
            {
                var source = Path.Combine(replaceRoot, Path.GetFileNameWithoutExtension(model), $"{name}.png");
                if (!File.Exists(source))
                {
                    continue;
                }
                IImageFormat format = texture.format switch
                {
                    4 => new RGBA(4, 4, 4, 4),
                    13 => new ETC1(true),
                    _ => throw new NotSupportedException($"Unsupported banner texture format: {texture.format}"),
                };
                using var image = new Bitmap(source);
                var pixels = EncodePixels(image, format);
                if (pixels.Length != texture.texDataSize)
                {
                    throw new InvalidDataException($"Banner texture size mismatch: {source}");
                }
                pixels.CopyTo(binary, (int)texture.texDataOffset);
            }
            File.WriteAllBytes(Path.Combine(outputRoot, Path.GetFileName(model)), binary);
            foreach (var bitmap in scene.bmps)
            {
                bitmap.Dispose();
            }
        }
    }
}
