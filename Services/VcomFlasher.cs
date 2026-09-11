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

using Kirin_Tool.Utils;
using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Kirin_Tool.Services
{
    public class VcomFlasher : IDisposable
    {
        private const int IDT_VID = 0x12D1;
        private const int IDT_PID = 0x3609;
        private const int MAX_DATA_LEN = 0x400;

        private SerialPort _serialPort;

        public void Connect()
        {
            string portName = FindIdtDevicePort();
            if (portName == null)
            {
                throw new IOException("No device in VCOM mode (HUAWEI USB COM 1.0) was found.");
            }

            _serialPort = new SerialPort(portName, 115200)
            {
                Handshake = Handshake.RequestToSend,

                DtrEnable = true,

                ReadTimeout = 10000,
                WriteTimeout = 10000
            };
            _serialPort.Open();
        }

        public async Task SendStartFrame()
        {
            var startCmd = new byte[] { 0xFE, 0x00, 0xFF, 0x01, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x02, 0x01, 0x1D, 0x0F };
            await WriteAndVerify(startCmd, false);
            await Task.Delay(50);
        }

        public async Task UploadData(byte[] data, int address, (bool shouldHeadResend, string Cpu) exploitInfo, IProgress<(long sent, long total)> progress)
        {
            await WriteAndVerify(BuildHeadCmd(address, data.Length));

            int seq = 0;
            long offset = 0;
            while (offset < data.Length)
            {
                int chunkSize = (int)Math.Min(MAX_DATA_LEN, data.Length - offset);
                var chunk = new byte[chunkSize];
                Array.Copy(data, offset, chunk, 0, chunkSize);
                seq++;
                await WriteAndVerify(BuildDataCmd(seq, chunk));
                offset += chunkSize;
                progress?.Report((offset, data.Length));
            }

            if (exploitInfo.shouldHeadResend)
            {
                int xloaderStart = exploitInfo.Cpu switch
                {
                    "hisi710" => 0x2316D,
                    "hisi710a" => 0x23155,
                    "hisi970" => 0x2316D,
                    "hisi980" => 0x2316D,
                    "hisi810" => 0x23155,
                    "hisi820" => 0x23155,
                    "hisi985" => 0x23155,
                    "hisi990" => 0x23155,
                    _ => throw new NotSupportedException($"Exploit requested for an unsupported CPU: {exploitInfo.Cpu}."),
                };
                await ExploitBootrom(xloaderStart, exploitInfo.Cpu);
            }
            else
            {
                await WriteAndVerify(BuildTailCmd(seq + 1), false);
            }
            await Task.Delay(500);
        }

        private async Task ExploitBootrom(int xloaderStartAddr, string cpu)
        {
            await WriteAndVerify(BuildHeadCmd(0x22000, 4), true);
            await Task.Delay(100);

            int returnAddr = cpu switch {
                "hisi970" => 0x4DBC8,
                "hisi980" => 0x4DBC8,
                "hisi710" => 0x49BC8,
                "hisi710a" => 0x49BC8,
                "hisi810" => 0x4DBC8,
                "hisi820" => 0x673c8,
                "hisi985" => 0x673c8,
                "hisi990" => 0x673c8,
                _ => throw new NotSupportedException($"Exploit requested for an unsupported CPU: {cpu}."),
            };

            await WriteAndVerify(BuildHeadCmd(returnAddr, 4), false);

            if (_serialPort.BytesToRead > 0)
            {
                byte[] rsp = new byte[_serialPort.BytesToRead];
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
                try
                {
                    int read = await _serialPort.BaseStream.ReadAsync(rsp, 0, rsp.Length, cts.Token);
                    if ((cpu == "hisi970" || cpu == "hisi980") && (read == 0 || rsp[0] != 0x07)) {}
                }
                catch (OperationCanceledException) { }
            }

            byte[] payloadBytes = BitConverter.GetBytes(xloaderStartAddr);
            if (!BitConverter.IsLittleEndian) Array.Reverse(payloadBytes);
            await WriteAndVerify(BuildDataCmd(1, payloadBytes), true);
            await Task.Delay(10);

            await WriteAndVerify(BuildTailCmd(2), false);
            await Task.Delay(100);
        }

        private async Task WriteAndVerify(byte[] command, bool expectAck = true)
        {
            await _serialPort.BaseStream.WriteAsync(command, 0, command.Length);
            await _serialPort.BaseStream.FlushAsync();

            if (expectAck)
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var ackBuffer = new byte[1];
                int bytesRead = await _serialPort.BaseStream.ReadAsync(ackBuffer, 0, 1, cts.Token);
                //if (bytesRead == 0 || ackBuffer[0] != 0xAA || ackBuffer[0] != 0x55)
                //{
                //    throw new IOException($"Failed to receive ACK from device. Received: {(bytesRead > 0 ? ackBuffer[0].ToString("X2") : "timeout")}");
                //}
            }
        }

        private byte[] BuildHeadCmd(int address, int length)
        {
            var cmd = new byte[12]; cmd[0] = 0xFE; cmd[1] = 0x00; cmd[2] = 0xFF; cmd[3] = 0x01;
            Array.Copy(BitConverter.GetBytes(length).Reverse().ToArray(), 0, cmd, 4, 4);
            Array.Copy(BitConverter.GetBytes(address).Reverse().ToArray(), 0, cmd, 8, 4);
            ushort crc = Crc16.CrcHqx(cmd);
            var fullCmd = new byte[14]; Array.Copy(cmd, fullCmd, 12);
            Array.Copy(BitConverter.GetBytes(crc).Reverse().ToArray(), 0, fullCmd, 12, 2);
            return fullCmd;
        }

        private byte[] BuildDataCmd(int seq, byte[] data)
        {
            var cmd = new byte[3 + data.Length]; cmd[0] = 0xDA; cmd[1] = (byte)(seq & 0xFF); cmd[2] = (byte)(~seq & 0xFF);
            Array.Copy(data, 0, cmd, 3, data.Length);
            ushort crc = Crc16.CrcHqx(cmd);
            var fullCmd = new byte[cmd.Length + 2]; Array.Copy(cmd, fullCmd, cmd.Length);
            Array.Copy(BitConverter.GetBytes(crc).Reverse().ToArray(), 0, fullCmd, cmd.Length, 2);
            return fullCmd;
        }

        private byte[] BuildTailCmd(int seq)
        {
            var cmd = new byte[3]; cmd[0] = 0xED; cmd[1] = (byte)(seq & 0xFF); cmd[2] = (byte)(~seq & 0xFF);
            ushort crc = Crc16.CrcHqx(cmd);
            var fullCmd = new byte[5]; Array.Copy(cmd, fullCmd, 3);
            Array.Copy(BitConverter.GetBytes(crc).Reverse().ToArray(), 0, fullCmd, 3, 2);
            return fullCmd;
        }

        private string FindIdtDevicePort()
        {
            string pattern = $"VID_{IDT_VID:X4}&PID_{IDT_PID:X4}";
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT Name FROM Win32_PnPEntity WHERE DeviceID LIKE '%{pattern}%'"))
                {
                    return searcher.Get().Cast<ManagementBaseObject>().Select(d => d["Name"]?.ToString()).FirstOrDefault(n => n?.Contains("COM") ?? false)?.Split('(', ')')[1];
                }
            }
            catch { return SerialPort.GetPortNames().FirstOrDefault(); }
        }

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
        }
    }
}