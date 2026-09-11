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
using Kirin_Tool.Security;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kirin_Tool.Services
{

    public class UnlockResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
    public class FirmwareUnlocker
    {
        public static readonly Dictionary<string, List<(string Name, int Address, bool PwnFlag)>> CpuAddresses = new Dictionary<string, List<(string, int, bool)>>
        {
            { "hisi65x_a", new List<(string, int, bool)>{ ("XLOADER", 0x00020000, false), ("FASTBOOT", 0x10000000, false) } },
            { "hisi65x_b", new List<(string, int, bool)>{ ("XLOADER", 0x00020000, false), ("FASTBOOT", 0x10000000, false) } },
            { "hisi620", new List<(string, int, bool)>{ ("XLOADER", unchecked((int)0xF9800800), false), ("FASTBOOT", 0x06800000, false) } },
            { "hisi620c", new List<(string, int, bool)>{ ("XLOADER", unchecked((int)0xF9800800), false), ("FASTBOOT", 0x06800000, false) } },
            { "hisi925", new List<(string, int, bool)>{ ("XLOADER", 0x00020000, false), ("FASTBOOT", 0x10000000, false) } },
            { "hisi935", new List<(string, int, bool)>{ ("XLOADER", 0x00020000, false), ("FASTBOOT", 0x10000000, false) } },
            { "hisi950", new List<(string, int, bool)>{ ("XLOADER", 0x00020000, false), ("FASTBOOT", 0x10000000, false) } },
            { "hisi955", new List<(string, int, bool)>{ ("XLOADER", 0x00020000, false), ("FASTBOOT", 0x10000000, false) } },
            { "hisi960", new List<(string, int, bool)>{ ("XLOADER", 0x00020000, false), ("UCE", unchecked((int)0x6A908000), false), ("FASTBOOT", 0x1AC00000, false) } },
            { "hisi970", new List<(string, int, bool)>{ ("null", 0x22000, false), ("XLOADER", 0x22000, true), ("UCE", unchecked((int)0x60049000), false), ("FASTBOOT", 0x16800000, false) } },
            { "hisi980", new List<(string, int, bool)>{ ("null", 0x22000, false), ("XLOADER", 0x22000, true), ("UCE", unchecked((int)0x60049000), false), ("FASTBOOT", unchecked((int)0x1A400000), false) } },
            { "hisik3v2", new List<(string, int, bool)>{ ("USBLOADER", unchecked((int)0xF8000000), false) } },
            { "hisi710", new List<(string, int, bool)>{ ("null", 0x22000, false), ("XLOADER", 0x22000, true), ("UCE", unchecked((int)0x6000D000), false), ("FASTBOOT", 0x1C000000, false) } },
            { "hisi710a", new List<(string, int, bool)>{ ("null", 0x22000, false), ("XLOADER", 0x22000, true), ("UCE", unchecked((int)0x6000D000), false), ("FASTBOOT", 0x1C000000, false) } },
            { "hisi810", new List<(string, int, bool)>{ ("null", 0x22000, false), ("XLOADER", 0x22000, true), ("UCE", unchecked((int)0x60000000), false), ("FASTBOOT", 0x1C000000, false) } },
            { "hisi820", new List<(string, int, bool)>{ ("null", 0x22000, false), ("XLOADER", 0x22000, true), ("UCE", unchecked((int)0x60000000), false), ("FASTBOOT", 0x1A400000, false), ("BL2", 0x1E400000, false) } },
            { "hisi985", new List<(string, int, bool)>{ ("null", 0x22000, false), ("XLOADER", 0x22000, true), ("UCE", unchecked((int)0x60000000), false), ("FASTBOOT", 0x1A400000, false), ("BL2", 0x1E400000, false) } },
            { "hisi990", new List<(string, int, bool)>{ ("null", 0x22000, false), ("XLOADER", 0x22000, true), ("UCE", unchecked((int)0x60000000), false), ("FASTBOOT", 0x1A400000, false), ("BL2", 0x1E400000, false) } },
        };

        public async Task<UnlockResult> UnlockFastboot(string cpu, ObservableCollection<ProgressItemViewModel> progressItems, IProgress<string> overallProgress, Func<string, Task<bool>> interactionHandler = null, bool useFastFlashLoader = false)
        {
            try
            {
                var loaderDir = Path.Combine(Directory.GetCurrentDirectory(), "loaders", cpu);
                if (!Directory.Exists(loaderDir))
                {
                    return new UnlockResult { IsSuccess = false, Message = $"Loader directory for {cpu} not found at '{loaderDir}'" };
                }

                using (var flasher = new VcomFlasher())
                {
                    overallProgress.Report("Attempting to connect to device in VCOM mode...");
                    flasher.Connect();
                    overallProgress.Report("Device connected successfully! Sending handshake...");
                    await flasher.SendStartFrame();
                    overallProgress.Report("Handshake sent. Starting flash process...");

                    foreach (var loaderInfo in CpuAddresses[cpu])
                    {
                        var currentItem = progressItems.FirstOrDefault(p => p.FileName == loaderInfo.Name);
                        if (currentItem == null) continue;

                        string fileName = $"{loaderInfo.Name.ToLower()}.ktl";
                        if (useFastFlashLoader && loaderInfo.Name == "FASTBOOT")
                        {
                            fileName = "fastbootf.ktl";
                        }

                        var filePath = Path.Combine(loaderDir, fileName);
                        if (!File.Exists(filePath))
                        {
                            currentItem.ProgressValue = 100;
                            currentItem.StatusText = "Skipped";
                            overallProgress.Report($"Skipping missing file: {Path.GetFileName(filePath)}");
                            continue;
                        }

                        currentItem.StatusText = "Decrypting";
                        overallProgress.Report($"Decrypting {Path.GetFileName(filePath)}...");
                        var decryptedData = CryptoUtil.DTL(await File.ReadAllBytesAsync(filePath));

                        currentItem.StatusText = "Uploading...";
                        overallProgress.Report($"Uploading {loaderInfo.Name}...");

                        var fileProgress = new Progress<(long sent, long total)>(fp => {
                            if (fp.total > 0) currentItem.ProgressValue = ((double)fp.sent / fp.total) * 100;
                        });

                        await flasher.UploadData(decryptedData, loaderInfo.Address, (loaderInfo.PwnFlag, cpu), fileProgress);

                        currentItem.ProgressValue = 100;
                        currentItem.StatusText = "Done";
                        overallProgress.Report($"Finished uploading {loaderInfo.Name}.");

                        if ((cpu == "hisi980" || cpu == "hisi810" || cpu == "hisi820" || cpu == "hisi985" || cpu == "hisi990") && loaderInfo.Name == "null")
                        {
                            overallProgress.Report("Waiting for cable manipulation...");
                            await Task.Delay(1000);
                            if (interactionHandler != null)
                            {
                                bool proceed = await interactionHandler("If you are using a modified cable (Harmony TP), please unplug it from the COMPUTER SIDE, wait a few seconds, then plug it back in.");
                                if (!proceed) return new UnlockResult { IsSuccess = false, Message = "Operation cancelled by user." };
                            }
                        }
                    }

                    if ((cpu == "hisi980" || cpu == "hisi810" || cpu == "hisi820" || cpu == "hisi985" || cpu == "hisi990") && interactionHandler != null)
                    {
                        string replugStr = useFastFlashLoader ? "If you are using a modified cable (Harmony TP), please unplug it from BOTH SIDES, and connect the device with a standard cable." : "If you are using a modified cable (Harmony TP), please unplug it from the COMPUTER SIDE, wait a few seconds, then plug it back in.";
                        if (cpu == "hisi810" || cpu == "hisi820" || cpu == "hisi985" || cpu == "hisi990")
                        {
                            replugStr = "If you are using a modified cable (Harmony TP), please unplug it from BOTH SIDES, and connect the device with a standard cable.";
                        }

                        // await interactionHandler("If you are using a modified cable (Harmony TP):\n1. Unplug it from both sides\n2. Connect the device to the computer with a normal cable\n3. Wait a few seconds\n4. Reconnect using the modified cable");
                        await interactionHandler(replugStr);
                    }
                }

                overallProgress.Report("Unlock process completed successfully!");
                return new UnlockResult { IsSuccess = true, Message = "Unlocked fastboot should be loaded now." };
            }
            catch (Exception ex)
            {
                overallProgress.Report($"Error: {ex.Message}");
                return new UnlockResult { IsSuccess = false, Message = $"An error occurred: {ex.Message}" };
            }
        }
    }
}