using image_nintendo.CGFX;
using Kontract.IO;
using pk3DS.Core;
using pk3DS.Core.CTR;
using System.Drawing.Imaging;
using System.Reflection;

namespace PCTRGen6Tools;

internal partial class Helper
{
    public static void ExportImages(string inputRoot, string outputRoot)
    {
        foreach (var filePath in Directory.GetFiles(inputRoot, "*.bin", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var binary = File.ReadAllBytes(filePath);
            if (binary.Length < 4)
            {
                continue;
            }
            var position = DARC.GetDARCposition(binary[..(binary.Length & ~3)]);
            if (position < 0)
            {
                continue;
            }
            var memberPath = Path.ChangeExtension(Path.GetRelativePath(inputRoot, filePath), null);
            var darc = new DARC(binary[position..]);
            foreach (var (path, index) in GetDarcImages(darc))
            {
                var entry = darc.Entries[index];
                var offset = (int)(entry.DataOffset - darc.Header.FileDataOffset);
                var data = darc.Data.AsSpan(offset, (int)entry.DataLength).ToArray();
                var relative = Path.Combine(memberPath, $"{Path.GetFileNameWithoutExtension(path)}.png").Replace('\\', '/');
                var texture = new BCLIM(data);
                using var image = texture.Format is XLIMEncoding.LA4 or XLIMEncoding.ETC1 or XLIMEncoding.ETC1A4
                    ? new image_nintendo.BXLIM.BXLIM(new MemoryStream(data)).Image
                    : texture.GetBitmap();
                var output = Path.Combine(outputRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                image.Save(output, ImageFormat.Png);
            }
        }
    }

    public static void ExportBanner(string inputRoot, string outputRoot)
    {
        foreach (var model in Directory.GetFiles(inputRoot, "*.bcmdl").Order())
        {
            var scene = new CGFX(File.OpenRead(model));
            var textures = (List<TxobEntry>)typeof(CGFX).GetField("TxObs", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(scene)!;
            using var names = new BinaryReaderX(File.OpenRead(model));
            var bitmapIndex = 0;
            var directory = Path.GetFileNameWithoutExtension(model);
            foreach (var texture in textures)
            {
                names.BaseStream.Position = texture.nameOffset;
                var name = names.ReadCStringA();
                for (var level = 0; level < Math.Max(1, texture.mipmapLvls); level++)
                {
                    var destination = level == 0 ? directory : Path.Combine(directory, $"mipmap{level}");
                    var relative = Path.Combine(destination, $"{name}.png").Replace('\\', '/');
                    var output = Path.Combine(outputRoot, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                    scene.bmps[bitmapIndex++].Save(output, ImageFormat.Png);
                }
            }
            foreach (var bitmap in scene.bmps)
            {
                bitmap.Dispose();
            }
        }
    }
}
