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
using System.Linq;
using System.Threading.Tasks;
using Kirin_Tool.Services;

namespace Kirin_Tool.Services.USBUpdate
{
    public class XloaderPatcher
    {
        private static readonly string ResDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fastboot");
        private readonly Action<string> _log;
        private readonly string _dloadDirectory;
        private readonly FastbootClient _fastbootClient;

        public XloaderPatcher(Action<string> log, string dloadDirectory, FastbootClient fastbootClient)
        {
            _log = log;
            _dloadDirectory = dloadDirectory;
            _fastbootClient = fastbootClient;
        }

        public void PatchXloader(string xloaderPath)
        {
            string crcHackedPath = Path.Combine(ResDirectory, "payload");
            
            if (!File.Exists(crcHackedPath))
            {
                throw new Exception($"Crucial file not found");
            }

            byte[] xloaderData = File.ReadAllBytes(xloaderPath);
            byte[] crchacked = File.ReadAllBytes(crcHackedPath);

            if (crchacked.Length != 0x8000)
            {
                throw new Exception("Crucial file has invalid size");
            }

            if (xloaderData.Length < 0x8000)
            {
                throw new Exception("XLOADER.img is too small to patch");
            }

            byte[] a1 = new byte[0x8000];
            Array.Copy(xloaderData, 0, a1, 0, 0x8000);

            byte[] a2 = new byte[xloaderData.Length - 0x8000];
            Array.Copy(xloaderData, 0x8000, a2, 0, a2.Length);

            byte[] c = new byte[0x8000];
            for (int i = 0; i < 0x8000; i++)
            {
                c[i] = (byte)(crchacked[i] ^ a1[i]);
            }

            using (FileStream fs = new FileStream(xloaderPath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(c, 0, c.Length);
                fs.Write(a2, 0, a2.Length);
            }
        }

        public void PatchXloaderHeader(string headerPath)
        {
            if (!File.Exists(headerPath))
            {
                throw new Exception("XLOADER.img.header not found to patch");
            }

            byte[] headerData = File.ReadAllBytes(headerPath);
            if (headerData.Length < 92)
            {
                throw new Exception("XLOADER.img.header is too small to patch");
            }

            string name = "PRELOADER";
            byte[] nameBytes = System.Text.Encoding.ASCII.GetBytes(name);

            for (int i = 0; i < 32; i++)
            {
                headerData[60 + i] = 0x00;
            }

            Array.Copy(nameBytes, 0, headerData, 60, nameBytes.Length);

            File.WriteAllBytes(headerPath, headerData);
        }

        public bool VerifyPatch(string originalPath, string patchedPath)
        {
            byte[] originalData = File.ReadAllBytes(originalPath).Take(0x8000).ToArray();
            byte[] patchedData = File.ReadAllBytes(patchedPath).Take(0x8000).ToArray();

            if (originalData.SequenceEqual(patchedData))
            {
                return false;
            }

            ushort originalCrc = Crc16X25.Calculate(originalData);
            ushort patchedCrc = Crc16X25.Calculate(patchedData);

            if (originalCrc != patchedCrc)
            {
                return false;
            }

            return true;
        }

        public void ModifyListTxtStopAfterXloader()
        {
            string listPath = Path.Combine(_dloadDirectory, "list.txt");
            if (!File.Exists(listPath))
                return;

            var lines = File.ReadAllLines(listPath).ToList();
            bool foundXloader = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string[] parts = lines[i].Split(' ');
                if (parts.Length < 2)
                    continue;

                string name = parts[0];

                if (name.Equals("XLOADER", StringComparison.OrdinalIgnoreCase))
                {
                    foundXloader = true;
                    continue;
                }

                if (foundXloader)
                {
                    lines[i] = $"{name} 0";
                }
            }

            File.WriteAllLines(listPath, lines);
        }

        public void RestoreListTxt()
        {
            string listPath = Path.Combine(_dloadDirectory, "list.txt");
            if (!File.Exists(listPath))
                return;

            var lines = File.ReadAllLines(listPath).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                string[] parts = lines[i].Split(' ');
                if (parts.Length >= 2)
                {
                    string name = parts[0];
                    lines[i] = $"{name} 1";
                }
            }

            File.WriteAllLines(listPath, lines);
        }

        public async Task FlashXloaderViaFastbootAsync(string xloaderPath)
        {

            var result = await _fastbootClient.FlashPartition("xloader", xloaderPath);
            
            if (!result.IsSuccess)
            {
                throw new Exception($"Fastboot failed: {result.Output}");
            }
        }

        public async Task RebootViaFastbootAsync()
        {
            await _fastbootClient.RebootAsync();
        }
    }
}
