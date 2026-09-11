/*
 * Kirin-Tool Read-Only Reference License
 *
 * Licensor:
 * Kethily Daniel & NDX
 *
 * Licensed Work:
 * All files and contents of the Kirin-Tool repository, including source code, documentation, assets, configuration files, build scripts, and associated materials.
 *
 * Contact Information:
 * https://kirintool.cfd
 *
 *
 * Copyright (c) 2026 Kethily Daniel & NDX. All rights reserved.
 *
 * This source code and all associated resources (collectively, the "Work") are
 * published strictly for human review and reference purposes.
 *
 * 1. READ-ONLY ACCESS
 *    You are permitted only to view and read the Work as published in this
 *    repository. No license is granted to download, clone, fork, mirror, cache,
 *    or store the Work for any purpose beyond transient viewing, except where
 *    strictly necessary for standard, incidental operation of the hosting
 *    platform (e.g., your browser's normal rendering of the page).
 *
 * 2. PROHIBITED USES
 *    You are strictly prohibited from:
 *    - Copying, reproducing, or duplicating any part of the Work.
 *    - Modifying, altering, or creating derivative works based on the Work.
 *    - Distributing, publishing, sublicensing, or selling the Work or any
 *      portion or derivative of it.
 *    - Using the Work in any commercial or non-commercial product or project.
 *    - Using any automated means (scripts, scrapers, crawlers, or bots) to
 *      access, index, or extract the Work.
 *
 * 3. ARTIFICIAL INTELLIGENCE / MACHINE LEARNING RESTRICTION
 *    You may not use the Work, in whole or in part, to train, fine-tune,
 *    evaluate, prompt, augment (e.g., retrieval-augmented generation), or
 *    otherwise develop any artificial intelligence or machine learning model,
 *    including large language models ("LLMs"), whether by direct ingestion,
 *    automated scraping, dataset inclusion, or any other method. This includes,
 *    without limitation:
 *    - Submitting the Work as input/context to an AI system or LLM.
 *    - Including the Work in any training, fine-tuning, or evaluation corpus.
 *    - Using the Work to generate embeddings, summaries, or derived training
 *      signals of any kind.
 *
 *    TEXT AND DATA MINING RESERVATION: To the extent any applicable law
 *    (including, without limitation, Article 4 of Directive (EU) 2019/790)
 *    provides an exception or limitation permitting text and data mining absent
 *    an express reservation, the rights holder hereby expressly reserves all
 *    such rights and opts out of any such exception with respect to the Work.
 *
 * 4. NO IMPLIED LICENSE
 *    Nothing in this license shall be construed as granting any license or
 *    right, by implication, estoppel, or otherwise, to any intellectual
 *    property rights in the Work beyond the limited viewing right expressly
 *    stated in Section 1.
 *
 * 5. ENFORCEMENT
 *    Any use of the Work in violation of this license immediately terminates
 *    any permission granted herein and may subject the violator to legal
 *    action for copyright infringement and any other applicable claims.
 *
 * THE WORK IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE, AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHOR
 * OR COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER LIABILITY
 * ARISING FROM THE WORK OR THE USE OR OTHER DEALINGS IN THE WORK.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Kirin_Tool.Models;

namespace Kirin_Tool.Services.USBUpdate
{
    public class USBUpdateApp
    {
        private readonly Action<string> _log;
        private readonly string _dloadDirectory;

        public USBUpdateApp(Action<string> log, string dloadDirectory)
        {
            _log = log;
            _dloadDirectory = dloadDirectory;
        }

        public void ExtractUpToXloader(string updateAppPath)
        {
            Directory.CreateDirectory(_dloadDirectory);

            using (FileStream fs = new FileStream(updateAppPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int startAddr = 0;
                byte[] unlockCmd = FindUnlockCode(reader, ref startAddr);

                File.WriteAllBytes(Path.Combine(_dloadDirectory, "unlockcode"), unlockCmd);

                fs.Seek(startAddr, SeekOrigin.Begin);

                List<string> imageList = new List<string>();
                bool foundXloader = false;

                while (true)
                {
                    if (fs.Position + 4 > fs.Length)
                        break;

                    var (dataLength, partitionName, headerData) = ParseImageHeader(reader);

                    if (dataLength == 0 || string.IsNullOrEmpty(partitionName))
                        break;


                    string headerPath = Path.Combine(_dloadDirectory, $"{partitionName}.img.header");
                    File.WriteAllBytes(headerPath, headerData);

                    string imgPath = Path.Combine(_dloadDirectory, $"{partitionName}.img");
                    ExtractImageData(reader, imgPath, dataLength);

                    if (File.Exists(imgPath))
                    {
                        long fileSize = new FileInfo(imgPath).Length;
                        if (fileSize == 0)
                        {
                            try
                            {
                                File.Delete(imgPath);
                                File.Delete(headerPath);
                            }
                            catch { }
                            continue;
                        }
                    }

                    imageList.Add($"{partitionName} 1");

                    if (partitionName.Equals("XLOADER", StringComparison.OrdinalIgnoreCase))
                    {
                        foundXloader = true;
                        break;
                    }

                    long currentPos = fs.Position;
                    int padding = (int)(4 - (currentPos % 4)) % 4;
                    if (padding > 0)
                        fs.Seek(padding, SeekOrigin.Current);
                }

                if (!foundXloader)
                {
                    throw new Exception("XLOADER partition not found in UPDATE.APP");
                }

                string listPath = Path.Combine(_dloadDirectory, "list.txt");
                File.WriteAllLines(listPath, imageList);
            }
        }

        public void ExtractSinglePartition(string updateAppPath, string targetPartition)
        {
            Directory.CreateDirectory(_dloadDirectory);

            using (FileStream fs = new FileStream(updateAppPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int startAddr = 0;
                FindUnlockCode(reader, ref startAddr);
                fs.Seek(startAddr, SeekOrigin.Begin);

                while (true)
                {
                    if (fs.Position + 4 > fs.Length)
                        break;

                    var (dataLength, partitionName, headerData) = ParseImageHeader(reader);

                    if (dataLength == 0)
                        break;

                    if (partitionName.Equals(targetPartition, StringComparison.OrdinalIgnoreCase))
                    {
                        File.WriteAllBytes(Path.Combine(_dloadDirectory, $"{partitionName}.img.header"), headerData);

                        string imgPath = Path.Combine(_dloadDirectory, $"{partitionName}.img");
                        ExtractImageData(reader, imgPath, dataLength);
                        return;
                    }
                    else
                    {
                        long remaining = dataLength;
                        while (remaining > 0)
                        {
                            long toSkip = Math.Min(int.MaxValue, remaining);
                            fs.Seek(toSkip, SeekOrigin.Current);
                            remaining -= toSkip;
                        }
                    }

                    long currentPos = fs.Position;
                    int padding = (int)(4 - (currentPos % 4)) % 4;
                    if (padding > 0)
                        fs.Seek(padding, SeekOrigin.Current);
                }

                throw new Exception($"Partition {targetPartition} not found in package.");
            }
        }

        public (List<string> imageList, int startAddr) GetPartitionNames(string updateAppPath)
        {
            List<string> imageList = new List<string>();
            int foundAddr = 0;

            using (FileStream fs = new FileStream(updateAppPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                FindUnlockCode(reader, ref foundAddr);
                fs.Seek(foundAddr, SeekOrigin.Begin);

                while (true)
                {
                    if (fs.Position + 4 > fs.Length)
                        break;

                    var (dataLength, partitionName, _) = ParseImageHeader(reader);

                    if (dataLength == 0 || string.IsNullOrEmpty(partitionName))
                        break;

                    imageList.Add(partitionName);

                    long remaining = dataLength;
                    while (remaining > 0)
                    {
                        long toSkip = Math.Min(int.MaxValue, remaining);
                        fs.Seek(toSkip, SeekOrigin.Current);
                        remaining -= toSkip;
                    }

                    long currentPos = fs.Position;
                    int padding = (int)(4 - (currentPos % 4)) % 4;
                    if (padding > 0)
                        fs.Seek(padding, SeekOrigin.Current);
                }
            }

            return (imageList, foundAddr);
        }

        public List<string> ExtractAllPartitions(string updateAppPath, bool extractUnlockCode = false, int startAddr = -1, Action<int>? onProgress = null, List<string>? includePartitions = null)
        {
            Directory.CreateDirectory(_dloadDirectory);

            List<string> imageList = new List<string>();

            using (FileStream fs = new FileStream(updateAppPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                if (startAddr == -1)
                {
                    int foundAddr = 0;
                    byte[] unlockCmd = FindUnlockCode(reader, ref foundAddr);
                    startAddr = foundAddr;

                    if (extractUnlockCode)
                    {
                        string unlockPath = Path.Combine(_dloadDirectory, "unlockcode");
                        if (!File.Exists(unlockPath))
                        {
                            File.WriteAllBytes(unlockPath, unlockCmd);
                        }
                    }
                }

                fs.Seek(startAddr, SeekOrigin.Begin);

                while (true)
                {
                    if (fs.Position + 4 > fs.Length)
                        break;

                    var (dataLength, partitionName, headerData) = ParseImageHeader(reader);

                    if (dataLength == 0 || string.IsNullOrEmpty(partitionName))
                        break;

                    if (includePartitions != null && !includePartitions.Contains(partitionName, StringComparer.OrdinalIgnoreCase))
                    {
                        long remaining = dataLength;
                        while (remaining > 0)
                        {
                            long toSkip = Math.Min(int.MaxValue, remaining);
                            fs.Seek(toSkip, SeekOrigin.Current);
                            remaining -= toSkip;
                        }

                        long curr = fs.Position;
                        int pad = (int)(4 - (curr % 4)) % 4;
                        if (pad > 0)
                            fs.Seek(pad, SeekOrigin.Current);

                        continue;
                    }


                    string headerPath = Path.Combine(_dloadDirectory, $"{partitionName}.img.header");
                    File.WriteAllBytes(headerPath, headerData);

                    string imgPath = Path.Combine(_dloadDirectory, $"{partitionName}.img");
                    ExtractImageData(reader, imgPath, dataLength);

                    if (File.Exists(imgPath))
                    {
                        long fileSize = new FileInfo(imgPath).Length;
                        if (fileSize == 0)
                        {
                            try
                            {
                                File.Delete(imgPath);
                                File.Delete(headerPath);
                            }
                            catch { }
                            continue;
                        }
                    }

                    imageList.Add($"{partitionName} 1");
                    onProgress?.Invoke((int)((double)fs.Position / fs.Length * 100));

                    long currentPos = fs.Position;
                    int padding = (int)(4 - (currentPos % 4)) % 4;
                    if (padding > 0)
                        fs.Seek(padding, SeekOrigin.Current);
                }
            }

            return imageList;
        }

        private byte[] FindUnlockCode(BinaryReader reader, ref int startAddr)
        {
            long length = reader.BaseStream.Length;
            int bufferSize = 64 * 1024;
            byte[] buffer = new byte[bufferSize + 4];
            long currentPos = reader.BaseStream.Position;

            while (currentPos < length - 4)
            {
                reader.BaseStream.Seek(currentPos, SeekOrigin.Begin);
                int bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length);
                if (bytesRead < 4) break;

                for (int i = 0; i <= bytesRead - 4; i++)
                {
                    if (buffer[i] == 0x55 && buffer[i + 1] == 0xAA && buffer[i + 2] == 0x5A && buffer[i + 3] == 0xA5)
                    {
                        startAddr = (int)(currentPos + i);
                        reader.BaseStream.Seek(startAddr + 12, SeekOrigin.Begin);
                        byte[] unlockCode = reader.ReadBytes(8);
                        
                        string unlockStr = Encoding.ASCII.GetString(unlockCode).ToLower();
                        if (unlockStr.Contains("hw"))
                        {
                            return unlockCode;
                        }
                        
                        reader.BaseStream.Seek(startAddr + 1, SeekOrigin.Begin);
                    }
                }
                currentPos += (bytesRead - 3);
            }

            throw new Exception("Invalid UPDATE.APP file format: Magic not found");
        }

        private (long dataLength, string partitionName, byte[] headerData) ParseImageHeader(BinaryReader reader)
        {
            MemoryStream headerStream = new MemoryStream();
            BinaryWriter headerWriter = new BinaryWriter(headerStream);

            byte[] magic = reader.ReadBytes(4);

            if (magic.Length < 4)
                return (0, "", new byte[0]);

            headerWriter.Write(magic);

            if (magic[0] != 0x55 || magic[1] != 0xAA || magic[2] != 0x5A || magic[3] != 0xA5)
                return (0, "", new byte[0]);

            int headerLength = reader.ReadInt32();
            headerWriter.Write(headerLength);

            headerWriter.Write(reader.ReadBytes(4));
            headerWriter.Write(reader.ReadBytes(8));
            headerWriter.Write(reader.ReadBytes(4));

            uint dataLengthUint = reader.ReadUInt32();
            long dataLength = dataLengthUint;
            headerWriter.Write(dataLengthUint);

            headerWriter.Write(reader.ReadBytes(16));
            headerWriter.Write(reader.ReadBytes(16));

            byte[] nameBytes = reader.ReadBytes(32);
            headerWriter.Write(nameBytes);

            string partitionName = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');

            headerWriter.Write(reader.ReadBytes(6));

            int remainingHeaderLen = headerLength - 98;

            if (remainingHeaderLen > 0)
            {
                headerWriter.Write(reader.ReadBytes(remainingHeaderLen));
            }

            return (dataLength, partitionName, headerStream.ToArray());
        }

        private void ExtractImageData(BinaryReader reader, string outputPath, long dataLength)
        {
            using (FileStream outFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                long remaining = dataLength;
                int bufferSize = 1024 * 1024;
                byte[] buffer = new byte[bufferSize];

                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(bufferSize, remaining);
                    int bytesRead = reader.Read(buffer, 0, toRead);
                    if (bytesRead == 0) break;
                    outFile.Write(buffer, 0, bytesRead);
                    remaining -= bytesRead;
                }
            }
        }
    }
}
