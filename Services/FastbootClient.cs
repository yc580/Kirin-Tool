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
using Kirin_Tool.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kirin_Tool.Services
{
    public class FastbootClient
    {
        private readonly string _fastbootPath;

        public FastbootClient(string executablePath = "fastboot/fastboot.exe")
        {
            _fastbootPath = Path.Combine(Directory.GetCurrentDirectory(), executablePath);
        }

        public async Task<bool> IsDeviceConnected()
        {
            if (!File.Exists(_fastbootPath)) return false;
            var result = await ProcessRunner.RunAsync(_fastbootPath, "devices");
            return result.IsSuccess && result.Output.Contains("fastboot");
        }

        public async Task<string> ReadNveVariable(string variable)
        {
            var result = await ProcessRunner.RunAsync(_fastbootPath, $"getvar nve:{variable}");
            if (result.IsSuccess && result.Output.Contains($"nve:{variable}:"))
            {
                return result.Output.Split('\n')
                    .FirstOrDefault(line => line.StartsWith($"nve:{variable}:"))
                    ?.Substring($"nve:{variable}:".Length).Trim() ?? string.Empty;
            }
            return string.Empty;
        }

        public async Task<ProcessResult> WriteNveVariable(string variable, string value)
        {
            return await ProcessRunner.RunAsync(_fastbootPath, $"getvar nve:{variable}@{value}");
        }

        public async Task<ProcessResult> FlashPartition(string partitionName, string filePath)
        {
            return await ProcessRunner.RunAsync(_fastbootPath, $"flash {partitionName} \"{filePath}\"");
        }

        public async Task<FrpBypassResult> EraseFrpWithSteps()
        {
            var result = new FrpBypassResult();

            var step1Result = await ProcessRunner.RunAsync(_fastbootPath, "erase frp");
            result.Step1Success = step1Result.IsSuccess && step1Result.Output.ToUpper().Contains("OKAY");
            result.Step1Output = step1Result.Output;

            var step2Result = await ProcessRunner.RunAsync(_fastbootPath, "erase config");
            result.Step2Success = step2Result.IsSuccess && step2Result.Output.ToUpper().Contains("OKAY");
            result.Step2Output = step2Result.Output;

            var step3Result = await ProcessRunner.RunAsync(_fastbootPath, "oem frp-erase");
            result.Step3Success = step3Result.IsSuccess && step3Result.Output.ToUpper().Contains("OKAY");
            result.Step3Output = step3Result.Output;


            return result;
        }

        public async Task<EnableDowngradeResult> EnableDowngradeWithSteps()
        {
            var result = new EnableDowngradeResult();

            var step1Result = await ProcessRunner.RunAsync(_fastbootPath, "oem oeminfoerase-amssver");
            result.Step1Success = step1Result.IsSuccess && step1Result.Output.ToUpper().Contains("OKAY");
            result.Step1Output = step1Result.Output;

            var step2Result = await ProcessRunner.RunAsync(_fastbootPath, "oem oeminfoerase-basever");
            result.Step2Success = step2Result.IsSuccess && step2Result.Output.ToUpper().Contains("OKAY");
            result.Step2Output = step2Result.Output;

            var step3Result = await ProcessRunner.RunAsync(_fastbootPath, "oem oeminfoerase-custver");
            result.Step3Success = step3Result.IsSuccess && step3Result.Output.ToUpper().Contains("OKAY");
            result.Step3Output = step3Result.Output;

            var step4Result = await ProcessRunner.RunAsync(_fastbootPath, "oem oeminfoerase-preloadver");
            result.Step4Success = step4Result.IsSuccess && step4Result.Output.ToUpper().Contains("OKAY");
            result.Step4Output = step4Result.Output;

            return result;
        }

        public async Task<ProcessResult> PullOemInfo(string outputPath)
        {
            return await ProcessRunner.RunAsync(_fastbootPath, $"oem dump-emmc oeminfo \"{outputPath}\"");
        }

        public async Task<ProcessResult> FlashOemInfo(string filePath)
        {
            return await ProcessRunner.RunAsync(_fastbootPath, $"flash oeminfo \"{filePath}\"");
        }

        public async Task<string> GetVarAsync(string variable)
        {
            var result = await ProcessRunner.RunAsync(_fastbootPath, $"getvar {variable}", timeoutSeconds: 10);
            return result.Output;
        }

        public async Task<string> OemCommandAsync(string command, int timeoutMinutes = 300)
        {
            var result = await ProcessRunner.RunAsync(_fastbootPath, $"oem {command}", timeoutMinutes: timeoutMinutes);
            return result.Output;
        }

        public async Task<string> CommandAsync(string arguments, int timeoutMinutes = 300)
        {
            var result = await ProcessRunner.RunAsync(_fastbootPath, arguments, timeoutMinutes: timeoutMinutes);
            return result.Output;
        }

        public async Task<string> RebootBootloaderAsync()
        {
            return await CommandAsync("reboot-bootloader");
        }

        public async Task<(bool IsSuccess, string Output)> UnlockBootloaderAsync(IProgress<string> progress = null)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _fastbootPath,
                Arguments = "oem unlock UUUUUUUUUUUUUUUU",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = processStartInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var combinedBuilder = new StringBuilder();
            var outputLock = new object();

            process.Start();

            var outputTask = Task.Run(async () =>
            {
                try
                {
                    using var reader = process.StandardOutput;
                    char[] buffer = new char[1];
                    while (true)
                    {
                        int read = await reader.ReadAsync(buffer, 0, 1);
                        if (read == 0)
                        {
                            break;
                        }
                        
                        char c = buffer[0];
                        
                        lock (outputLock)
                        {
                            outputBuilder.Append(c);
                            combinedBuilder.Append(c);
                            progress?.Report(combinedBuilder.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            });

            var errorTask = Task.Run(async () =>
            {
                try
                {
                    using var reader = process.StandardError;
                    char[] buffer = new char[1];
                    while (true)
                    {
                        int read = await reader.ReadAsync(buffer, 0, 1);
                        if (read == 0)
                        {
                            break;
                        }
                        
                        char c = buffer[0];
                        
                        lock (outputLock)
                        {
                            errorBuilder.Append(c);
                            combinedBuilder.Append(c);
                            progress?.Report(combinedBuilder.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            });

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(300));
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                return (false, "Process timed out after 5 minutes");
            }

            await Task.Delay(500);
            try
            {
                await Task.WhenAny(outputTask, errorTask, Task.Delay(2000));
            }
            catch { }

            string output = combinedBuilder.ToString();
            return (process.ExitCode == 0, output);
        }

        public async Task<ProcessResult> RebootAsync()
        {
            return await ProcessRunner.RunAsync(_fastbootPath, "reboot");
        }
    }
}