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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;

namespace Kirin_Tool.UI
{
    public class FlashPartitionItem : INotifyPropertyChanged
    {
        private string _statusText = "Pending";
        private double _progressValue = 0;
        private bool _isCompleted = false;
        private bool _isSuccess = false;
        private Stopwatch _stopwatch;
        private DispatcherTimer _tickTimer;
        private string _timeElapsed = "";

        public PartitionInfo Partition { get; }
        public string Name { get; }
        public string FormattedSize { get; }
        public string DisplayName { get; set; }
        public string UniqueId { get; }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set
            {
                _isSuccess = value;
                OnPropertyChanged(nameof(IsSuccess));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(BackgroundBrush));
                OnPropertyChanged(nameof(BorderBrush));
            }
        }

        public string TimeElapsed
        {
            get => _timeElapsed;
            set
            {
                _timeElapsed = value;
                OnPropertyChanged(nameof(TimeElapsed));
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                if (IsCompleted)
                    return IsSuccess ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);

                if (ProgressValue > 0)
                    return new SolidColorBrush(Colors.Orange);

                return new SolidColorBrush(Colors.Gray);
            }
        }

        public SolidColorBrush BackgroundBrush
        {
            get
            {
                if (IsCompleted)
                    return IsSuccess ? new SolidColorBrush(Color.FromRgb(240, 255, 240)) : new SolidColorBrush(Color.FromRgb(255, 240, 240));

                if (ProgressValue > 0)
                    return new SolidColorBrush(Color.FromRgb(255, 250, 230));

                return new SolidColorBrush(Colors.White);
            }
        }

        public SolidColorBrush BorderBrush
        {
            get
            {
                if (IsCompleted)
                    return IsSuccess ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);

                if (ProgressValue > 0)
                    return new SolidColorBrush(Colors.Orange);

                return new SolidColorBrush(Colors.LightGray);
            }
        }

        public FlashPartitionItem(PartitionInfo partition)
        {
            Partition = partition;
            Name = partition.Name;
            FormattedSize = partition.FormattedSize;
            DisplayName = partition.Name;
            UniqueId = $"{partition.Name}_{partition.EntryOffset}";
            _stopwatch = new Stopwatch();
            InitializeTickTimer();
        }

        public FlashPartitionItem(PartitionInfo partition, string source)
        {
            Partition = partition;
            Name = partition.Name;
            FormattedSize = partition.FormattedSize;
            DisplayName = $"{partition.Name} ({source})";
            UniqueId = $"{partition.Name}_{partition.EntryOffset}";
            _stopwatch = new Stopwatch();
            InitializeTickTimer();
        }

        private void InitializeTickTimer()
        {
            _tickTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _tickTimer.Tick += (s, e) =>
            {
                if (_stopwatch != null && _stopwatch.IsRunning)
                {
                    TimeElapsed = _stopwatch.Elapsed.ToString(@"mm\:ss");
                }
            };
        }

        private void StartTimer()
        {
            if (!_stopwatch.IsRunning)
            {
                _stopwatch.Start();
            }
            if (!_tickTimer.IsEnabled)
            {
                _tickTimer.Start();
            }
        }

        private void StopTimer()
        {
            _tickTimer?.Stop();
            _stopwatch?.Stop();
        }

        public void UpdateStatus(string status)
        {
            if (ProgressValue > 0 && !_isCompleted)
            {
                StartTimer();
            }

            if (_stopwatch.IsRunning)
            {
                TimeElapsed = _stopwatch.Elapsed.ToString(@"mm\:ss");
            }

            StatusText = status;
        }

        public void Complete(bool success, string message = null)
        {
            StopTimer();
            IsCompleted = true;
            IsSuccess = success;
            ProgressValue = 100;
            StatusText = success ? "Done" : (message ?? "Failed");
            TimeElapsed = _stopwatch?.Elapsed.ToString(@"mm\:ss") ?? "";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
