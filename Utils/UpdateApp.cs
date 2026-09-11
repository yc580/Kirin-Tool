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

using Kirin_Tool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kirin_Tool.Utils
{
    public class UpdateApp : IDisposable
    {
        private readonly string _filePath;
        private BinaryReader _binaryReader;
        private FileStream _fileStream;
        private bool _isUsbMode;
        private bool _disposed = false;

        public List<PartitionInfo> Partitions { get; private set; }

        public UpdateApp(string filePath, bool isUsbMode = false)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _isUsbMode = isUsbMode;

            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"File not found: {_filePath}");

            Partitions = new List<PartitionInfo>();

            try
            {
                _fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024 * 1024);
                _binaryReader = new BinaryReader(_fileStream);
                ParseFile();
            }
            catch (Exception ex)
            {
                _binaryReader?.Dispose();
                _fileStream?.Dispose();
                throw new InvalidOperationException($"Failed to parse UPDATE.APP file: {ex.Message}", ex);
            }
        }

        private void ParseFile()
        {
            const uint MAGIC = 0xA55AAA55;
            const int ALIGNMENT = 4;

            long fileLength = _fileStream.Length;

            long currentPosition = 0;

            while (currentPosition < fileLength)
            {
                _fileStream.Seek(currentPosition, SeekOrigin.Begin);

                if (fileLength - currentPosition < 4) break;

                var buffer = new byte[4];
                if (_binaryReader.Read(buffer, 0, 4) != 4) break;

                if (BitConverter.ToUInt32(buffer, 0) == MAGIC)
                {

                    long headerStartPosition = currentPosition;
                    var partition = ReadPartition(headerStartPosition);

                    if (partition != null)
                    {
                        Partitions.Add(partition);

                        currentPosition = _fileStream.Position;
                    }
                    else
                    {
                        currentPosition += 4;
                    }
                }
                else
                {
                    currentPosition++;
                }
            }

        }

        private PartitionInfo ReadPartition(long startPosition)
        {
            try
            {
                _fileStream.Seek(startPosition + 4, SeekOrigin.Begin);

                if (_fileStream.Length - _fileStream.Position < (4 + 4 + 8 + 4 + 4 + 16 + 16 + 16))
                    return null;

                var headerSize = _binaryReader.ReadUInt32();
                var unknown1 = _binaryReader.ReadUInt32();
                var hardwareId = _binaryReader.ReadUInt64();
                var sequence = _binaryReader.ReadUInt32();
                var size = _binaryReader.ReadUInt32();

                var date = ReadNullTerminatedString(16);
                var time = ReadNullTerminatedString(16);
                var type = ReadNullTerminatedString(32).Trim();

                
                long currentPosRelative = _fileStream.Position - startPosition;
                long remainingHeaderBytesToSkip = (long)headerSize - currentPosRelative;

                if (remainingHeaderBytesToSkip < 0) return null;

                if (_fileStream.Length - _fileStream.Position < remainingHeaderBytesToSkip + size)
                    return null;

                _fileStream.Seek(remainingHeaderBytesToSkip, SeekOrigin.Current);

                long dataOffset = _fileStream.Position;

                _fileStream.Seek(size, SeekOrigin.Current);

                var alignment = (4 - _fileStream.Position % 4) % 4;
                if (_fileStream.Length - _fileStream.Position < alignment)
                    return null;

                _fileStream.Seek(alignment, SeekOrigin.Current);

                string partitionName = _isUsbMode ? type : GetPartitionName(type);
                if (string.IsNullOrWhiteSpace(partitionName)) partitionName = "UNKNOWN";

                return new PartitionInfo
                {
                    Name = partitionName,
                    Size = size,
                    FormattedSize = FormatBytes(size),
                    UpdateAppFilePath = _filePath,
                    HeaderSize = headerSize,
                    DataOffset = dataOffset,
                    EntryOffset = startPosition,
                    IsSelected = true 
                };
            }
            catch (EndOfStreamException)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private string ReadNullTerminatedString(int maxLength)
        {
            var bytes = new List<byte>();
            int count = 0;
            byte b;

            while (count < maxLength && (b = _binaryReader.ReadByte()) != 0)
            {
                bytes.Add(b);
                count++;
            }

            if (count < maxLength)
            {
                _fileStream.Seek(maxLength - 1 - count, SeekOrigin.Current);
            }

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        private string GetPartitionName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return "UNKNOWN";

            string name = rawName.ToLowerInvariant().Trim();

            return name switch
            {
                "hisiufs_gpt" => "ptable",
                "efi" => "ptable",
                "ufsfw" => "ufs_fw",
                "erecovery_ramdis" => "erecovery_ramdisk",
                "recovery_ramdis" => "recovery_ramdisk",
                "vbmeta_hw_produc" => "vbmeta_hw_product",
                _ => name
            };
        }

        public async Task ExtractPartition(PartitionInfo partition, string outputPath)
        {
            if (partition == null) throw new ArgumentNullException(nameof(partition));
            if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            _fileStream.Seek(partition.DataOffset, SeekOrigin.Begin);

            const int bufferSize = 1024 * 1024;
            var buffer = new byte[bufferSize];
            long remaining = partition.Size;

            using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
            {
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(bufferSize, remaining);
                    int read = await _fileStream.ReadAsync(buffer, 0, toRead);
                    if (read == 0) break;

                    await outputStream.WriteAsync(buffer, 0, read);
                    remaining -= read;
                }
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";

            double size = bytes;
            string[] units = { "B", "KB", "MB", "GB" };
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:F1} {units[unitIndex]}";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _binaryReader?.Dispose();
                _fileStream?.Dispose();
                _disposed = true;
            }
        }
    }
}
