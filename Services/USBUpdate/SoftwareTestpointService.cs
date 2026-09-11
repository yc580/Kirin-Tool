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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kirin_Tool.Services;

namespace Kirin_Tool.Services.USBUpdate
{
    public class SoftwareTestpointService
    {
        private readonly Action<string> _log;
        private readonly string _dloadDirectory;
        private readonly FastbootClient _fastbootClient;
        private readonly Func<string, string, Task<bool>> _onRetryRequired;

        public SoftwareTestpointService(Action<string> log, string dloadDirectory, FastbootClient fastbootClient, Func<string, string, Task<bool>> onRetryRequired)
        {
            _log = log;
            _dloadDirectory = dloadDirectory;
            _fastbootClient = fastbootClient;
            _onRetryRequired = onRetryRequired;
        }

        public async Task EnterSoftwareTestpoint(string updateAppPath)
        {
            if (Directory.Exists(_dloadDirectory))
            {
                try { Directory.Delete(_dloadDirectory, true); } catch { }
            }
            Directory.CreateDirectory(_dloadDirectory);

            var extractor = new USBUpdateApp(_log, _dloadDirectory);
            var patcher = new XloaderPatcher(_log, _dloadDirectory, _fastbootClient);
            var usbService = new UsbDownloadService(_log, _dloadDirectory);

            usbService.OnRetryRequired += (title, message) =>
            {
                return _onRetryRequired(title, message).GetAwaiter().GetResult();
            };


            extractor.ExtractUpToXloader(updateAppPath);

            string xloaderPath = Path.Combine(_dloadDirectory, "XLOADER.img");
            string xloaderHeaderPath = Path.Combine(_dloadDirectory, "XLOADER.img.header");
            if (!File.Exists(xloaderPath))
            {
                throw new Exception("Extraction failed, XLOADER not found in Base UPDATE.");
            }
            if (!File.Exists(xloaderHeaderPath))
            {
                throw new Exception("Extraction failed, XLOADER header not found in Base UPDATE.");
            }

            string xloaderBackupPath = Path.Combine(_dloadDirectory, "XLOADER_BAK.img");
            string xloaderHeaderBackupPath = Path.Combine(_dloadDirectory, "XLOADER_HEADER_BAK.img");
            File.Copy(xloaderPath, xloaderBackupPath, true);
            File.Copy(xloaderHeaderPath, xloaderHeaderBackupPath, true);

            try
            {
                patcher.PatchXloader(xloaderPath);

                if (!patcher.VerifyPatch(xloaderBackupPath, xloaderPath))
                {
                    throw new Exception("Failed to patch XLOADER");
                }

                patcher.PatchXloaderHeader(xloaderHeaderPath);

                patcher.ModifyListTxtStopAfterXloader();


                bool success = await Task.Run(() => usbService.FlashImages());
                if (!success)
                {
                    throw new Exception("Failed to Software Testpoint the device!\n\nMake sure you are using the Base UPDATE from the exact or newer firmware that your device is running.");
                }


            }
            finally
            {
                if (File.Exists(xloaderBackupPath))
                {
                    File.Copy(xloaderBackupPath, xloaderPath, true);
                    File.Delete(xloaderBackupPath);
                }
                if (File.Exists(xloaderHeaderBackupPath))
                {
                    File.Copy(xloaderHeaderBackupPath, xloaderHeaderPath, true);
                    File.Delete(xloaderHeaderBackupPath);
                }
            }
        }

        public async Task ExitSoftwareTestpoint(string updateAppPath)
        {
            if (Directory.Exists(_dloadDirectory))
            {
                try { Directory.Delete(_dloadDirectory, true); } catch { }
            }
            Directory.CreateDirectory(_dloadDirectory);

            var extractor = new USBUpdateApp(_log, _dloadDirectory);
            var patcher = new XloaderPatcher(_log, _dloadDirectory, _fastbootClient);


            extractor.ExtractSinglePartition(updateAppPath, "XLOADER");

            string xloaderPath = Path.Combine(_dloadDirectory, "XLOADER.img");
            if (!File.Exists(xloaderPath))
            {
                throw new Exception("XLOADER.img extraction failed");
            }


            await patcher.FlashXloaderViaFastbootAsync(xloaderPath);
        }
    }
}
