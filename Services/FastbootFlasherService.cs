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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Kirin_Tool.Services
{
    public class FastbootFlasherService
    {
        private readonly FastbootClient _fastbootClient;

        public FastbootFlasherService(FastbootClient fastbootClient)
        {
            _fastbootClient = fastbootClient;
        }

        public async Task<List<FastbootPartition>> GetPartitionTableAsync()
        {
            var result = await _fastbootClient.GetVarAsync("ptable");

            if (string.IsNullOrEmpty(result))
            {
                throw new InvalidOperationException("Failed to get partition table from device");
            }

            var partitions = new List<FastbootPartition>();
            var lines = result.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var match = Regex.Match(line.Trim(), @"\(bootloader\)\s*:(.+)");
                if (match.Success)
                {
                    var partitionName = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(partitionName))
                    {
                        partitions.Add(new FastbootPartition(partitionName));
                    }
                }
            }

            return partitions;
        }

        public async Task<(bool IsSuccess, string Message)> DumpPartitionAsync(string partitionName, string savePath)
        {
            try
            {
                var result = await _fastbootClient.OemCommandAsync($"dump-emmc {partitionName} \"{savePath}\"", timeoutMinutes: 300);
                bool isSuccess = !string.IsNullOrEmpty(result) &&
                                !result.ToLower().Contains("fail") &&
                                !result.ToLower().Contains("error");

                if (isSuccess)
                    return (true, result ?? "Dump completed");

                var storageResult = await _fastbootClient.OemCommandAsync($"dump-storage {partitionName} \"{savePath}\"", timeoutMinutes: 300);
                bool storageSuccess = !string.IsNullOrEmpty(storageResult) &&
                                     !storageResult.ToLower().Contains("fail") &&
                                     !storageResult.ToLower().Contains("error");
                return (storageSuccess, storageResult ?? "Dump completed");
            }
            catch (TimeoutException)
            {
                return (false, "Dump operation timed out - partition may be too large");
            }
            catch (Exception ex)
            {
                return (false, $"Dump failed: {ex.Message}");
            }
        }


        public async Task<(bool IsSuccess, string Message)> FlashPartitionAsync(string partitionName, string imagePath)
        {
            try
            {
                var result = await _fastbootClient.FlashPartition(partitionName, imagePath);

                return (result.IsSuccess, result.Output);
            }
            catch (Exception ex)
            {
                // return (false, $"Flash failed: {ex.Message}");
                return (false, $"Failed");
            }
        }

        public void GenerateFlashingXml(List<FastbootPartition> partitions, string outputPath)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "gb2312", "yes"),
                new XElement("configurations",
                    new XElement("configuration",
                        new XElement("fastbootimage",
                            partitions.Select(p =>
                                new XElement("image",
                                    new XAttribute("name", p.Name.ToUpperInvariant()),
                                    new XAttribute("identifier", p.Identifier),
                                    $"fastbootimage/{p.ImageFileName}")
                            )
                        )
                    )
                )
            );

            doc.Save(outputPath);
        }

        public List<FastbootPartition> ParseFlashingXml(string xmlPath)
        {
            var partitions = new List<FastbootPartition>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var settings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            XDocument doc;
            using (var reader = XmlReader.Create(xmlPath, settings))
            {
                doc = XDocument.Load(reader);
            }

            var fastbootImageSection = doc.Descendants("fastbootimage").FirstOrDefault();
            if (fastbootImageSection == null)
                return partitions;

            var images = fastbootImageSection.Elements("image");

            foreach (var image in images)
            {
                var name = image.Attribute("name")?.Value;
                var identifier = image.Attribute("identifier")?.Value;
                var fileName = image.Value?.Trim();

                if (string.IsNullOrEmpty(fileName))
                    continue;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(identifier))
                {
                    if (identifier.Equals("huawei_crc_check", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var partition = new FastbootPartition(identifier)
                    {
                        Name = name,
                        IsSelected = true
                    };

                    fileName = fileName.Replace('/', '\\');

                    var xmlDirectory = Path.GetDirectoryName(xmlPath);
                    var parentDirectory = Path.GetDirectoryName(xmlDirectory);

                    if (Path.IsPathRooted(fileName))
                    {
                        partition.DumpPath = fileName;
                    }
                    else
                    {
                        var candidatePath = Path.Combine(xmlDirectory, fileName);

                        if (!File.Exists(candidatePath) && !string.IsNullOrEmpty(parentDirectory))
                        {
                            var parentCandidate = Path.Combine(parentDirectory, fileName);
                            if (File.Exists(parentCandidate))
                            {
                                candidatePath = parentCandidate;
                            }
                        }

                        partition.DumpPath = candidatePath;
                    }

                    partitions.Add(partition);
                }
            }

            return partitions;
        }

    }
}
