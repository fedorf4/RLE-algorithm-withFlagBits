using System;
using System.Runtime.ConstrainedExecution;

namespace test
{
    internal class Program
    {
        public static IEnumerable<byte> Encode(Stream dataStream)
        {
            List<byte> encodedBytes = new();
            var sr = new BinaryReader(dataStream);

            List<byte> bufferBytes = new();
            byte prev = sr.ReadByte();
            int countSameBytes = 1;
            while (sr.BaseStream.Position != sr.BaseStream.Length)
            {
                byte cur = sr.ReadByte();
                if (cur == prev)
                {
                    countSameBytes++;
                    continue;
                }

                if (countSameBytes >= 3)
                {
                    if (bufferBytes.Any())
                    {
                        encodedBytes.Add((byte)bufferBytes.Count);
                        encodedBytes.AddRange(bufferBytes);
                        bufferBytes.Clear();
                    }
                    while (countSameBytes > 127)
                    {
                        encodedBytes.Add(127 + 128);
                        encodedBytes.Add(prev);
                        countSameBytes -= 127;
                    }
                    encodedBytes.Add((byte)(countSameBytes + 128));
                    encodedBytes.Add(prev);
                    prev = cur;
                    countSameBytes = 1;
                }
                else
                {
                    bufferBytes.AddRange(Enumerable.Repeat(prev, countSameBytes));
                    if (bufferBytes.Count >= 127)
                    {
                        encodedBytes.Add(127);
                        encodedBytes.AddRange(bufferBytes.Take(127));
                        bufferBytes.RemoveRange(0, 127);
                    }
                    prev = cur;
                    countSameBytes = 1;
                }
            }
            if (countSameBytes >= 3)
            {
                while (countSameBytes > 127)
                {
                    encodedBytes.Add(127 + 128);
                    encodedBytes.Add(prev);
                    countSameBytes -= 127;
                }
                if (countSameBytes >= 3)
                {
                    encodedBytes.Add((byte)(countSameBytes + 128));
                    encodedBytes.Add(prev);
                }
                else
                    encodedBytes.AddRange(Enumerable.Repeat(prev, countSameBytes));
            }
            else
            if (bufferBytes.Any())
            {
                if (bufferBytes.Count >= 127)
                {
                    encodedBytes.Add(127);
                    encodedBytes.AddRange(bufferBytes.Take(127));
                    bufferBytes.RemoveRange(0, 127);
                }
                if (bufferBytes.Count >= 127)
                    throw new Exception(message: "Error buffer RLE encode"); // For sure, never called

                encodedBytes.Add((byte)(bufferBytes.Count + 1));
                encodedBytes.AddRange(bufferBytes);
                encodedBytes.Add(prev);
            }

            return encodedBytes;
        }

        public static IEnumerable<byte> Decode(Stream dataStream)
        {
            List<byte> decodedBytes = new();
            var sr = new BinaryReader(dataStream);

            while (sr.BaseStream.Position != sr.BaseStream.Length)
            {
                byte flagL = sr.ReadByte();
                if (flagL >= 128)
                {
                    decodedBytes.AddRange(Enumerable.Repeat(sr.ReadByte(), flagL - 128));
                    continue;
                }
                for (int i = 0; i < flagL; i++)
                    decodedBytes.Add(sr.ReadByte());

            }
            return decodedBytes;
        }

        static void Main(string[] args)
        {
            string path = @"F:\test.txt";
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            List<byte> encodedBytes = Encode(stream).ToList();
            stream.Close();

            string pathSave = @"F:\encoded.txt";
            File.WriteAllBytes(pathSave, encodedBytes.ToArray());

            string pathDecoded = @"F:\encoded.txt";
            Stream streamDecode = new FileStream(pathDecoded, FileMode.Open, FileAccess.Read);
            List<byte> decodedBytes = Decode(streamDecode).ToList();
            streamDecode.Close();
            string pathRes = @"F:\res.txt";
            File.WriteAllBytes(pathRes, decodedBytes.ToArray());
        }
    }
}