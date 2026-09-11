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
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Kirin_Tool.Utils
{
    public static class SuperMerger
    {
        public static async Task MergeSuperImages(string path1, string path2, string outputPath, Action<double> progressCallback = null)
        {
            await Task.Run(() => PerformMerge(path1, path2, outputPath, progressCallback));
        }

        private static void PerformMerge(string path1, string path2, string outputPath, Action<double> progressCallback)
        {
            var len1 = new FileInfo(path1).Length;
            var len2 = new FileInfo(path2).Length;

            string largePath = path1, smallPath = path2;
            long largeLen = len1, smallLen = len2;

            if (len2 > len1)
            {
                largePath = path2;
                smallPath = path1;
                largeLen = len2;
                smallLen = len1;
            }

            long totalSize = largeLen + smallLen;
            const int bufferSize = 4 * 1024 * 1024;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try
            {
                long lastChunkOffset = 0;
                SparseHeader hLarge, hSmall;

                using (var fs = new FileStream(largePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(fs))
                {
                    hLarge = ReadStruct<SparseHeader>(reader);
                    if (hLarge.Magic != SparseConstants.SPARSE_HEADER_MAGIC) 
                        throw new Exception($"File {Path.GetFileName(largePath)} is not a valid sparse image.");

                    for (int i = 0; i < hLarge.TotalChunks; i++)
                    {
                        lastChunkOffset = fs.Position;
                        var chunk = ReadStruct<ChunkHeader>(reader);
                        fs.Seek(chunk.TotalSize - SparseConstants.CHUNK_HEADER_SIZE, SeekOrigin.Current);
                    }
                }

                using (var src = new FileStream(largePath, FileMode.Open, FileAccess.Read))
                using (var dst = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    long remaining = lastChunkOffset;
                    while (remaining > 0)
                    {
                        int toRead = (int)Math.Min(bufferSize, remaining);
                        int read = src.Read(buffer, 0, toRead);
                        if (read == 0) break;
                        dst.Write(buffer, 0, read);
                        remaining -= read;
                        progressCallback?.Invoke((double)dst.Position * 100 / totalSize);
                    }
                }

                long newDataOffset = 0;
                using (var fs = new FileStream(smallPath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(fs))
                {
                    hSmall = ReadStruct<SparseHeader>(reader);
                    if (hSmall.Magic != SparseConstants.SPARSE_HEADER_MAGIC) 
                        throw new Exception($"File {Path.GetFileName(smallPath)} is not a valid sparse image.");

                    var firstChunk = ReadStruct<ChunkHeader>(reader);
                    newDataOffset = fs.Position + (firstChunk.TotalSize - SparseConstants.CHUNK_HEADER_SIZE);
                }

                using (var src = new FileStream(smallPath, FileMode.Open, FileAccess.Read))
                using (var dst = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
                {
                    src.Seek(newDataOffset, SeekOrigin.Begin);
                    int read;
                    while ((read = src.Read(buffer, 0, bufferSize)) > 0)
                    {
                        dst.Write(buffer, 0, read);
                        progressCallback?.Invoke((double)dst.Position * 100 / totalSize);
                    }
                }

                uint newTotalChunks = (hLarge.TotalChunks - 1) + (hSmall.TotalChunks - 1);
                using (var fs = new FileStream(outputPath, FileMode.Open, FileAccess.Write))
                using (var writer = new BinaryWriter(fs))
                {
                    fs.Seek(0x0C, SeekOrigin.Begin);
                    writer.Write(4096); 
                    
                    fs.Seek(0x14, SeekOrigin.Begin);
                    writer.Write(newTotalChunks);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static T ReadStruct<T>(BinaryReader reader) where T : struct
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf<T>());
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try { return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject()); }
            finally { handle.Free(); }
        }
    }
}
