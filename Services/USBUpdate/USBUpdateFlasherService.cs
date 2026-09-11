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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Kirin_Tool.Utils;

namespace Kirin_Tool.Services.USBUpdate
{
    public class USBUpdateFlasherService
    {
        private readonly Action<string> _log;
        private readonly string _dloadDirectory;
        private readonly Func<string, string, Task<bool>> _onRetryRequired;

        public Action<int>? OnProgressUpdate;
        public Action<List<(string PartitionName, string SourceLabel)>>? OnPartitionsDiscovered;
        public Action<int, string, int>? OnPartitionProgress;
        public Action<int, string, bool, string>? OnPartitionCompleted;
        public Action<string>? OnExtractionStarted;
        public Action<int>? OnExtractionProgress;

        public USBUpdateFlasherService(Action<string> log, string dloadDirectory, Func<string, string, Task<bool>> onRetryRequired)
        {
            _log = log;
            _dloadDirectory = dloadDirectory;
            _onRetryRequired = onRetryRequired;
        }

        public async Task FlashPartitions(IEnumerable<(string FilePath, string Label, List<string> SelectedPartitions)> filesData, CancellationToken cancellationToken = default)
        {
            if (Directory.Exists(_dloadDirectory))
            {
                try { Directory.Delete(_dloadDirectory, true); } catch { }
            }
            Directory.CreateDirectory(_dloadDirectory);

            var sw = Stopwatch.StartNew();
            List<(string partitionName, string sourceLabel)> allPartitions = new List<(string, string)>();
            List<string> mappingLines = new List<string>();
            bool unlockCodeExtracted = false;

            int fileIndex = 0;
            foreach (var data in filesData)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(data.FilePath) || !File.Exists(data.FilePath))
                {
                    fileIndex++;
                    continue;
                }

                string subDir = Path.Combine(_dloadDirectory, $"part{fileIndex}");
                Directory.CreateDirectory(subDir);

                var extractor = new USBUpdateApp(_log, subDir);
                OnExtractionStarted?.Invoke(data.Label);
                
                var extracted = extractor.ExtractAllPartitions(data.FilePath, 
                    extractUnlockCode: !unlockCodeExtracted,
                    onProgress: (p) => OnExtractionProgress?.Invoke(p),
                    includePartitions: data.SelectedPartitions);

                if (!unlockCodeExtracted)
                {
                    string unlockPath = Path.Combine(subDir, "unlockcode");
                    if (File.Exists(unlockPath))
                    {
                        File.Copy(unlockPath, Path.Combine(_dloadDirectory, "unlockcode"), true);
                        unlockCodeExtracted = true;
                    }
                }

                foreach (var line in extracted)
                {
                    var parts = line.Split(' ');
                    if (parts.Length >= 1)
                    {
                        allPartitions.Add((parts[0], data.Label));
                        mappingLines.Add($"{parts[0]}|{subDir}");
                    }
                }
                fileIndex++;
            }

            // Super partition merging - detect and merge multiple super partitions
            var superIndices = new List<int>();
            for (int i = 0; i < allPartitions.Count; i++)
            {
                if (allPartitions[i].partitionName.Equals("super", StringComparison.OrdinalIgnoreCase))
                {
                    superIndices.Add(i);
                }
            }

            if (superIndices.Count > 1)
            {
                OnExtractionStarted?.Invoke("Merging super partitions");
                OnExtractionProgress?.Invoke(0);

                // Collect the super .img paths from each source directory
                var superImgPaths = new List<string>();
                string firstSuperHeaderPath = null;

                foreach (var idx in superIndices)
                {
                    var mappingParts = mappingLines[idx].Split('|');
                    if (mappingParts.Length >= 2)
                    {
                        string sourceDir = mappingParts[1];
                        string imgPath = Path.Combine(sourceDir, "super.img");
                        if (File.Exists(imgPath))
                        {
                            superImgPaths.Add(imgPath);
                            if (firstSuperHeaderPath == null)
                            {
                                string headerPath = Path.Combine(sourceDir, "super.img.header");
                                if (File.Exists(headerPath))
                                    firstSuperHeaderPath = headerPath;
                            }
                        }
                    }
                }

                if (superImgPaths.Count > 1)
                {
                    string mergedDir = Path.Combine(_dloadDirectory, "super_merged");
                    Directory.CreateDirectory(mergedDir);
                    string mergedSuperPath = Path.Combine(mergedDir, "super.img");

                    string currentInput = superImgPaths[0];
                    for (int i = 1; i < superImgPaths.Count; i++)
                    {
                        string nextOutput = i == superImgPaths.Count - 1
                            ? mergedSuperPath
                            : Path.Combine(mergedDir, $"super_intermediate_{i}.img");

                        SuperMerger.MergeSuperImages(currentInput, superImgPaths[i], nextOutput, p =>
                        {
                            OnExtractionProgress?.Invoke((int)p);
                        }).GetAwaiter().GetResult();

                        if (i > 1 && currentInput.StartsWith(mergedDir) && File.Exists(currentInput))
                        {
                            try { File.Delete(currentInput); } catch { }
                        }

                        currentInput = nextOutput;
                    }

                    if (firstSuperHeaderPath != null)
                    {
                        string mergedHeaderPath = Path.Combine(mergedDir, "super.img.header");
                        File.Copy(firstSuperHeaderPath, mergedHeaderPath, true);

                        long mergedSize = new FileInfo(mergedSuperPath).Length;
                        byte[] headerBytes = File.ReadAllBytes(mergedHeaderPath);
                        if (headerBytes.Length >= 28)
                        {
                            headerBytes[24] = (byte)(mergedSize & 0xFF);
                            headerBytes[25] = (byte)((mergedSize >> 8) & 0xFF);
                            headerBytes[26] = (byte)((mergedSize >> 16) & 0xFF);
                            headerBytes[27] = (byte)((mergedSize >> 24) & 0xFF);
                            File.WriteAllBytes(mergedHeaderPath, headerBytes);
                        }
                    }

                    int firstSuperIndex = superIndices[0];
                    for (int i = superIndices.Count - 1; i >= 0; i--)
                    {
                        allPartitions.RemoveAt(superIndices[i]);
                        mappingLines.RemoveAt(superIndices[i]);
                    }

                    allPartitions.Insert(firstSuperIndex, ("super", "Merged Super"));
                    mappingLines.Insert(firstSuperIndex, $"super|{mergedDir}");

                    OnExtractionProgress?.Invoke(100);
                }
            }

            if (allPartitions.Count == 0)
            {
                throw new Exception("No partitions found to flash.");
            }

            OnPartitionsDiscovered?.Invoke(allPartitions);

            var listLines = allPartitions.Select(p => $"{p.partitionName} 1").ToList();
            File.WriteAllLines(Path.Combine(_dloadDirectory, "list.txt"), listLines);
            File.WriteAllLines(Path.Combine(_dloadDirectory, "partition_mapping.txt"), mappingLines);

            cancellationToken.ThrowIfCancellationRequested();

            var usbService = new UsbDownloadService(_log, _dloadDirectory);
            usbService.OnRetryRequired += (title, message) =>
            {
                return _onRetryRequired(title, message).GetAwaiter().GetResult();
            };

            usbService.OnProgressUpdate += (progress) => OnProgressUpdate?.Invoke(progress);
            usbService.OnPartitionStarted += (index, name) => OnPartitionProgress?.Invoke(index, name, 0);
            usbService.OnPartitionProgressUpdate += (index, name, prog) => OnPartitionProgress?.Invoke(index, name, prog);
            usbService.OnPartitionCompleted += (index, name, success, msg) => OnPartitionCompleted?.Invoke(index, name, success, msg);

            bool flashSuccess = false;
            try
            {
                flashSuccess = await Task.Run(() => usbService.FlashImages(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"USB Flashing failed: {ex.Message}", ex);
            }

            if (!flashSuccess)
            {
                throw new Exception("One or more partitions failed to flash.");
            }
        }
    }
}
