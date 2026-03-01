using System.Collections.ObjectModel;
using CombasLauncherApp.Models;
using System.IO;
using System.Windows.Media.Imaging;

namespace CombasLauncherApp.Services.Implementations
{
    public interface IHoundBuildService
    {
        public ObservableCollection<BuildEntry> GetBuildEntries(string mcdFolderPath);

        //public List<BuildEntry> GetAllBuildEntries();

    }

    public class HoundBuildService : IHoundBuildService
    {
        public ObservableCollection<BuildEntry> GetBuildEntries(string mcdFolderPath)
        {
            //Get the loaded build entries from the specified MCD folder path

            var buildEntries = new ObservableCollection<BuildEntry>();

            if (!Directory.Exists(mcdFolderPath))
            {
                return buildEntries;
            }

            var mcdDirs = Directory.GetDirectories(mcdFolderPath, "*.mcd", SearchOption.TopDirectoryOnly);

            foreach (var dir in mcdDirs)
            {
                var thumbnailPath = Path.Combine(dir, "__thumbnail.png");
                var fromSoftwarePath = Path.Combine(dir, "fromsoftware.txt");

                if (File.Exists(thumbnailPath) && File.Exists(fromSoftwarePath))
                {

                    var (buildName, imageHeaderOffset, buildDate) = GetHeaderInfo(fromSoftwarePath);

                    var image = GetHoundImageFromFile(fromSoftwarePath, imageHeaderOffset);


                    buildEntries.Add(new BuildEntry(
                        buildName,
                        dir,
                        image,
                        buildDate
                    ));
                }
            }

            return buildEntries;
        }

        private (string name, int imageHeaderOffset, DateTime buildDate) GetHeaderInfo(string filePath)
        {
            // Header Byte structure
            //
            // Bytes 0 - 7 Unknown
            // Bytes 8 - 21 Build DateTime
            // Bytes 22 - 23 Unknown
            // Bytes 24 - 54 Build Name
            // Bytes 55 - 61 Unknown
            // Bytes 62 - 63 ImageHeaderDataOffset

            // Read the first 64 bytes to get the header offset
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var header = new byte[64];
            if (fs.Read(header, 0, 64) != 64)
            {
                throw new EndOfStreamException("Could not read the file header.");
            }  

            // Extract the build datetime, 2 bytes per characters

            var year = (header[8] << 8) | header[9];
            var month = (header[10] << 8) | header[11];
            var day = (header[14] << 8) | header[15];
            var hour = (header[16] << 8) | header[17];
            var minute = (header[18] << 8) | header[19];
            var second = (header[20] << 8) | header[21]; 
            var buildDate = new DateTime(year, month, day, hour, minute, second);

            // Extract file name (offset 24, 15 UTF-16BE chars: 30 bytes)
            
            var nameBytes = header[24..54];

            var fileName = System.Text.Encoding.BigEndianUnicode.GetString(nameBytes).TrimEnd('\0');

            // Extract the image header data offset (big-endian)
            var headerOffset = (header[62] << 8) | header[63];

            return (fileName, headerOffset, buildDate);
        }

        private BitmapSource GetHoundImageFromFile(string filePath, int headerOffset)
        {
            const int width = 256;
            const int height = 256;

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.", filePath);
            }
            
            var offset = headerOffset + 16;

            byte[] buffer;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(offset, 0);
                var bytes = (int)fs.Length - offset;
                buffer = new byte[bytes];
                var bytesRead = fs.Read(buffer, 0, bytes);
                if (bytesRead != bytes)
                {
                    throw new EndOfStreamException("Could not read the expected number of bytes from the file.");
                }
            }

            return LoadHoundImage(buffer, width, height);
        }

        private BitmapSource LoadHoundImage(byte[] dxt1Data, int width, int height, int stageToVisualize = 0)
        {
            // Structure is as follows

            // The image is separated into 4 quarter sectors each 8192 bytes long in the following order
            //
            // +---+---+
            // | 0 | 1 |
            // +---+---+
            // | 2 | 3 |
            // +---+---+
            //
            //
            // Each one of these sectors is separated into 4 rows of 2048 bytes long in the following order
            //
            // +---+
            // | 0 |
            // +---+
            // | 2 |
            // +---+
            // | 1 |
            // +---+
            // | 3 |
            // +---+
            //
            //
            // Each one of these rows is separated into 4 finer rows of 512 bytes long in the following order
            // 
            // +---+
            // | 0 |
            // +---+
            // | 1 |
            // +---+
            // | 2 |
            // +---+
            // | 3 |
            // +---+
            //
            //
            // Each one of these finer rows is separated into 8 blocks of 64 bytes in the following order
            //
            // +---+---+---+---+---+---+---+---+
            // | 0 | 4 | 1 | 5 | 2 | 6 | 3 | 7 |
            // +---+---+---+---+---+---+---+---+
            //
            // & it alternates to this pattern WHY!!!!
            //
            // +---+---+---+---+---+---+---+---+
            // | 2 | 6 | 3 | 7 | 0 | 4 | 1 | 5 |
            // +---+---+---+---+---+---+---+---+
            //
            // Each one of these blocks is split into 8 Dxt1 blocks of 8 bytes. 
            //
            // +----+----+----+----+
            // | 0  | 1  | 4  | 5  |
            // +----+----+----+----+
            // | 2  | 3  | 6  | 7  |
            // +----+----+----+----+
            //
            //
            // Each DXT1 block corresponds to a 4x4 pixel area in the final image, with the following pixel order within the block: 
            // +----+----+----+----+
            // | 7  | 6  | 5  | 4  |
            // +----+----+----+----+
            // | 3  | 2  | 1  | 0  |
            // +----+----+----+----+
            // | 15 | 14 | 13 | 12 |
            // +----+----+----+----+
            // | 11 | 10 | 9  | 8  |
            // +----+----+----+----+




            // Output buffer: BGRA32
            var dst = new byte[width * height * 4];

            var blocksWide = (width + 3) / 4;
            var blocksHigh = (height + 3) / 4;



            for (var by = 0; by < blocksHigh; by++)
            {
                for (var bx = 0; bx < blocksWide; bx++)
                {
                    // --- Swizzle Stage Calculations ---
                    var sectorSizeX = blocksWide / 2;
                    var sectorSizeY = blocksHigh / 2;

                    // Stage 1: Sector (quarter)
                    var stage1X = bx / sectorSizeX;
                    var stage1Y = by / sectorSizeY;
                    var stage1 = stage1Y * 2 + stage1X;

                    // Stage 2: Row in sector
                    var localX1 = bx % sectorSizeX;
                    var localY1 = by % sectorSizeY;
                    var rowsPerStage2 = sectorSizeY / 4;
                    var stage2 = localY1 / rowsPerStage2;
                    var stage2Order = new[] { 0, 2, 1, 3 }[stage2];

                    // Stage 3: Finer row in Stage 2
                    var localY2 = localY1 % rowsPerStage2;
                    var rowsPerStage3 = rowsPerStage2 / 4;
                    var stage3 = localY2 / rowsPerStage3;


                    // Stage 4: Block group in Stage 3 (local to finer row)

                    // Each group is 4x2 blocks (8 blocks total)
                    var groupWidth = 4;
                    var groupHeight = 2;
                    var groupsPerRow = sectorSizeX / groupWidth; // e.g., 32/4 = 8

                    var groupX = (localX1 % sectorSizeX) / groupWidth;
                    var groupY = (localY1 % sectorSizeY) / groupHeight;
                    var stage4 = groupY * groupsPerRow + groupX; // 0..7 for 8 groups
                    // Alternating block order every 2 stage2
                    int[] patternA = [0, 4, 1, 5, 2, 6, 3, 7];
                    int[] patternB = [2, 6, 3, 7, 0, 4, 1, 5];
                    var usePatternB = (stage2 % 2) == 1;
                    var stage4Order = usePatternB ? patternB[stage4 % 8] : patternA[stage4 % 8];

                    // Stage 5: DXT1 block in 4x2 group
                    var localXInGroup = (localX1 % sectorSizeX) % groupWidth; // 0..3
                    var localYInGroup = (localY1 % sectorSizeY) % groupHeight; // 0..1
                    var stage5 = localYInGroup * groupWidth + localXInGroup; // 0..7
                    var stage5Order = new[] { 0, 1, 4, 5, 2, 3, 6, 7 }[stage5];

                    // Nested offset calculation
                    var sectorOffset = stage1 * (sectorSizeX * sectorSizeY);
                    var rowOffset = stage2Order * (sectorSizeX * rowsPerStage2);
                    var finerRowOffset = stage3 * (sectorSizeX * rowsPerStage3);
                    var blockGroupOffset = stage4Order * 8;

                    var swizzleIndex = sectorOffset + rowOffset + finerRowOffset + blockGroupOffset + stage5Order;


                    // --- Visualization ---
                    byte[] color;
                    
                    // Choose which stage to visualize: 1–5, or 0 for actual DXT1, 99 is for other parts of the stages
                    switch (stageToVisualize)
                    {
                        case 1:
                            color = stage1 switch
                            {
                                0 => [0, 0, 255, 255],     // BGRA: Red
                                1 => [0, 128, 255, 255],   // BGRA: Orange
                                2 => [0, 255, 255, 255],   // BGRA: Yellow
                                3 => [0, 255, 0, 255],     // BGRA: Green
                                _ => [255, 255, 255, 255]
                            };
                            break;
                        case 2:
                            if (stage1 > 0)
                            {
                                goto case 99;
                            }
                            color = stage2Order switch
                            {
                                0 => [0, 0, 255, 255],     // BGRA: Red
                                1 => [0, 128, 255, 255],   // BGRA: Orange
                                2 => [0, 255, 255, 255],   // BGRA: Yellow
                                3 => [0, 255, 0, 255],     // BGRA: Green
                                _ => [255, 255, 255, 255]
                            };
                            break;
                        case 3:
                            if (stage1 > 0 || stage2 > 0)
                            {
                                goto case 99;
                            }
                            color = stage3 switch
                            {
                                0 => [0, 0, 255, 255],     // BGRA: Red
                                1 => [0, 128, 255, 255],   // BGRA: Orange
                                2 => [0, 255, 255, 255],   // BGRA: Yellow
                                3 => [0, 255, 0, 255],     // BGRA: Green
                                _ => [255, 255, 255, 255]
                            };
                            break;
                        case 4:
                            if (stage1 > 0 || stage2 > 4 || stage3 > 0)
                            {
                                goto case 99;
                            }
                            color = stage4Order switch
                            {
                                0 => [0, 0, 255, 255],     // BGRA: Red
                                1 => [0, 128, 255, 255],   // BGRA: Orange
                                2 => [0, 255, 255, 255],   // BGRA: Yellow
                                3 => [0, 255, 0, 255],     // BGRA: Green
                                4 => [255, 255, 0, 255],   // BGRA: Cyan
                                5 => [255, 128, 0, 255],   // BGRA: Light Blue
                                6 => [255, 0, 0, 255],     // BGRA: Blue
                                7 => [128, 0, 255, 255],   // BGRA: Purple
                                _ => [255, 255, 255, 255]
                            };

                            break;
                        case 5:
                            if (stage1 > 0 || stage2 > 0 || stage3 > 0 || stage4 > 0)
                            {
                                goto case 99;
                            }
                            color = stage5Order switch
                            {
                                0 => [0, 0, 255, 255],     // BGRA: Red
                                1 => [0, 128, 255, 255],   // BGRA: Orange
                                2 => [0, 255, 255, 255],   // BGRA: Yellow
                                3 => [0, 255, 0, 255],     // BGRA: Green
                                4 => [255, 255, 0, 255],   // BGRA: Cyan
                                5 => [255, 128, 0, 255],   // BGRA: Light Blue
                                6 => [255, 0, 0, 255],     // BGRA: Blue
                                7 => [128, 0, 255, 255],   // BGRA: Purple
                                _ => [255, 255, 255, 255]
                            };
                            break;
                        case 99:
                            // Visualize block boundaries
                            color = (localX1 == 0 || localX1 == sectorSizeX - 1 || localY1 == 0 || localY1 == sectorSizeY - 1) ?
                            [
                                255, 255, 255, 255
                            ]
                            : [0, 0, 0, 255];
                            break;
                        default:
                            // Actual DXT1 decompression
                            var blockOffset = swizzleIndex * 8;
                            //  Debug.WriteLine($"X= {bx}, Y= {by}, offset= {blockOffset}");
                            // Buffer bounds check: skip block if out of range
                            if (blockOffset + 7 < dxt1Data.Length)
                            {
                                DecompressDxt1Block(dxt1Data, blockOffset, bx * 4, by * 4, width, height, dst);
                            }
                            else
                            {

                            }
                            continue;
                    }

                    // Fill the 4x4 block with the chosen color
                    for (var py = 0; py < 4; py++)
                    {
                        for (var px = 0; px < 4; px++)
                        {
                            var dstX = bx * 4 + px;
                            var dstY = by * 4 + py;
                            if (dstX < width && dstY < height)
                            {
                                var dstOffset = (dstY * width + dstX) * 4;
                                dst[dstOffset + 0] = color[0];
                                dst[dstOffset + 1] = color[1];
                                dst[dstOffset + 2] = color[2];
                                dst[dstOffset + 3] = color[3];
                            }
                        }
                    }
                }
            }

            var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            bitmap.WritePixels(
                new System.Windows.Int32Rect(0, 0, width, height),
                dst,
                width * 4,
                0
            );
            bitmap.Freeze();
            return bitmap;
        }

        // DXT1 block decompression for a single 4x4 block
        private static readonly int[] _dxt1PixelOrder = [4, 5, 6, 7, 0, 1, 2, 3, 12, 13, 14, 15, 8, 9, 10, 11];

        private void DecompressDxt1Block(byte[] data, int index, int x, int y, int width, int height, byte[] output)
        {
            var color0 = (ushort)(data[index + 1] | (data[index + 0] << 8));
            var color1 = (ushort)(data[index + 3] | (data[index + 2] << 8));
            var bits = BitConverter.ToUInt32(data, index + 4);

            var colors = new uint[4];
            colors[0] = ConvertRgb565ToBgra32(color0);
            colors[1] = ConvertRgb565ToBgra32(color1);

            var hasAlpha = color0 <= color1; // DXT1A: color3 is transparent if color0 <= color1

            if (!hasAlpha)
            {
                colors[2] = InterpolateBgra32(colors[0], colors[1], 2, 1);
                colors[3] = InterpolateBgra32(colors[0], colors[1], 1, 2);
            }
            else
            {
                colors[2] = InterpolateBgra32(colors[0], colors[1], 1, 1);
                colors[3] = 0x00000000; // Transparent
            }

            for (var i = 0; i < 16; i++)
            {
                var code = (int)((bits >> (2 * _dxt1PixelOrder[i])) & 0x03);
                var px = i % 4;
                var py = i / 4;
                var dstX = x + px;
                var dstY = y + py;
                if (dstX < width && dstY < height)
                {
                    var dstOffset = (dstY * width + dstX) * 4;
                    var color = colors[code];
                    output[dstOffset + 0] = (byte)(color & 0xFF);         // B
                    output[dstOffset + 1] = (byte)((color >> 8) & 0xFF);  // G
                    output[dstOffset + 2] = (byte)((color >> 16) & 0xFF); // R
                    output[dstOffset + 3] = (byte)((color >> 24) & 0xFF); // A
                }
            }
        }

        private uint ConvertRgb565ToBgra32(ushort c)
        {
            // Extract components
            var r = (c >> 11) & 0x1F;
            var g = (c >> 5) & 0x3F;
            var b = c & 0x1F;

            // Scale to 8 bits
            r = (r << 3) | (r >> 2);
            g = (g << 2) | (g >> 4);
            b = (b << 3) | (b >> 2);

            // Pack as BGRA (Blue, Green, Red, Alpha)
            return (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
        }

        private uint InterpolateBgra32(uint c0, uint c1, int w0, int w1)
        {
            var b = ((int)(c0 & 0xFF) * w0 + (int)(c1 & 0xFF) * w1) / (w0 + w1);
            var g = (((int)(c0 >> 8) & 0xFF) * w0 + ((int)(c1 >> 8) & 0xFF) * w1) / (w0 + w1);
            var r = (((int)(c0 >> 16) & 0xFF) * w0 + ((int)(c1 >> 16) & 0xFF) * w1) / (w0 + w1);
            return (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
        }

    }
}
