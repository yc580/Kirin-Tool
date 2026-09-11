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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kirin_Tool.Services
{
    public class OemInfoEditor
    {
        private const int BlockSize = 0x400;
        private const int DataPayloadOffsetInBlock = 0x200;
        private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes("OEM_INFO");

        private readonly byte[] _binaryData;
        private readonly int _detectedVersion;

        private static readonly Dictionary<int, (List<int> ModelIds, int RegionId)> TargetIdsMap = new Dictionary<int, (List<int>, int)>
        {
            { 6, (new List<int>{0x5b, 0x61}, 0x12) },
            { 8, (new List<int>{0x5ee, 0x5ef}, 0x5de) },
            { 9, (new List<int>{0x301, 0x300}, 0x2f0) }
        };

        public OemInfoEditor(byte[] oeminfoData)
        {
            _binaryData = oeminfoData;
            _detectedVersion = DetectOemVersion();
            if (_detectedVersion == -1)
            {
                throw new InvalidDataException("Could not detect a supported OEMINFO version.");
            }
        }

        private int DetectOemVersion()
        {
            for (int offset = 0; offset < _binaryData.Length; offset += BlockSize)
            {
                if (offset + 12 > _binaryData.Length) break;

                if (_binaryData.Skip(offset).Take(8).SequenceEqual(MagicBytes))
                {
                    return System.BitConverter.ToInt32(_binaryData, offset + 8);
                }
            }
            return -1;
        }

        public byte[] EditEntries(string model, string regionVendor)
        {
            if (!TargetIdsMap.ContainsKey(_detectedVersion))
                throw new System.NotSupportedException($"OEMINFO version '{_detectedVersion}' is not supported for modification.");

            var targets = TargetIdsMap[_detectedVersion];
            var modifiedData = (byte[])_binaryData.Clone();

            for (int offset = 0; offset < modifiedData.Length; offset += BlockSize)
            {
                if (offset + 16 > modifiedData.Length) break;
                if (!modifiedData.Skip(offset).Take(8).SequenceEqual(MagicBytes)) continue;

                int entryVersion = System.BitConverter.ToInt32(modifiedData, offset + 8);
                int entryId = System.BitConverter.ToInt32(modifiedData, offset + 12);

                if (entryVersion == _detectedVersion)
                {
                    if (targets.ModelIds.Contains(entryId))
                        UpdateEntry(modifiedData, offset, model);
                    else if (targets.RegionId == entryId)
                        UpdateEntry(modifiedData, offset, regionVendor);
                }
            }
            return modifiedData;
        }

        private void UpdateEntry(byte[] data, int blockOffset, string newString)
        {
            var newBytes = Encoding.UTF8.GetBytes(newString);
            int newLen = newBytes.Length;

            System.Buffer.BlockCopy(System.BitConverter.GetBytes(newLen), 0, data, blockOffset + 20, 4);

            int dataStartOffset = blockOffset + DataPayloadOffsetInBlock;
            System.Buffer.BlockCopy(newBytes, 0, data, dataStartOffset, newLen);

            int paddingStart = dataStartOffset + newLen;
            int maxPayloadSize = BlockSize - DataPayloadOffsetInBlock;
            for (int i = paddingStart; i < blockOffset + DataPayloadOffsetInBlock + maxPayloadSize; i++)
            {
                data[i] = 0xFF;
            }
        }
    }
}