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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;
using System.Collections.Generic;
using Application = System.Windows.Application;

namespace Kirin_Tool.UI
{
    public partial class ProcessDialogUapp : ContentDialog, INotifyPropertyChanged
    {
        private string _overallStatusText;
        private double _overallProgress;
        private string _currentOperationText;
        private bool _canCancel = true;
        private bool _showCloseButton = false;
        private CancellationTokenSource _cancellationTokenSource;

        public ObservableCollection<FlashPartitionItem> PartitionItems { get; }
        public bool IsUsbUpdateMode { get; set; } = false;

        public event EventHandler RequestClose;

        public string OverallStatusText
        {
            get => _overallStatusText;
            set
            {
                _overallStatusText = value;
                OnPropertyChanged(nameof(OverallStatusText));
            }
        }

        public double OverallProgress
        {
            get => _overallProgress;
            set
            {
                _overallProgress = value;
                OnPropertyChanged(nameof(OverallProgress));
            }
        }

        public string CurrentOperationText
        {
            get => _currentOperationText;
            set
            {
                _currentOperationText = value;
                OnPropertyChanged(nameof(CurrentOperationText));
            }
        }

        public bool CanCancel
        {
            get => _canCancel;
            set
            {
                _canCancel = value;
                OnPropertyChanged(nameof(CanCancel));
            }
        }

        public bool ShowCloseButton
        {
            get => _showCloseButton;
            set
            {
                _showCloseButton = value;
                OnPropertyChanged(nameof(ShowCloseButton));
            }
        }

        public ProcessDialogUapp(List<PartitionInfo> selectedPartitions, CancellationTokenSource cancellationTokenSource)
        {
            InitializeComponent();
            this.DataContext = this;

            _cancellationTokenSource = cancellationTokenSource;

            PartitionItems = new ObservableCollection<FlashPartitionItem>();

            foreach (var partition in selectedPartitions)
            {
                PartitionItems.Add(new FlashPartitionItem(partition));
            }

            OverallStatusText = $"Ready to flash {PartitionItems.Count} partitions";
            CurrentOperationText = "Waiting to start...";
        }

        public ProcessDialogUapp(List<(PartitionInfo Partition, string Source)> partitionsWithSources, CancellationTokenSource cancellationTokenSource)
        {
            InitializeComponent();
            this.DataContext = this;

            _cancellationTokenSource = cancellationTokenSource;

            PartitionItems = new ObservableCollection<FlashPartitionItem>();

            foreach (var (partition, source) in partitionsWithSources)
            {
                PartitionItems.Add(new FlashPartitionItem(partition, source));
            }

            OverallStatusText = $"Ready to flash {PartitionItems.Count} partitions";
            CurrentOperationText = "Waiting to start...";
        }

        public void UpdateCurrentPartitionByIndex(int index, string status, double progress)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (index >= 0 && index < PartitionItems.Count)
                {
                    var item = PartitionItems[index];
                    item.StatusText = status;
                    item.ProgressValue = progress;
                    item.UpdateStatus(status);

                    ScrollToPartition(item);
                }

                if (IsUsbUpdateMode)
                {
                    OverallProgress = progress;
                }
                else
                {
                    var completedItems = PartitionItems.Count(p => p.ProgressValue >= 100);
                    var totalProgress = PartitionItems.Sum(p => p.ProgressValue) / PartitionItems.Count;
                    OverallProgress = totalProgress;
                }

                OverallStatusText = $"Processing partition {index + 1}/{PartitionItems.Count}...";
                CurrentOperationText = $"Flashing: {status}";
            }));
        }

        public void CompletePartitionByIndex(int index, bool success, string message = null)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (index >= 0 && index < PartitionItems.Count)
                {
                    var item = PartitionItems[index];
                    item.Complete(success, message);
                }

                var completedItems = PartitionItems.Count(p => p.IsCompleted);
                OverallStatusText = $"Completed {completedItems}/{PartitionItems.Count} partitions";
            }));
        }


        public void SetOverallStatus(string status)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                OverallStatusText = status;
            }));
        }

        public void UpdateCurrentOperation(string operation)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CurrentOperationText = operation;
            }));
        }

        public void ReplacePartitions(List<(PartitionInfo Partition, string Source)> newPartitions)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                PartitionItems.Clear();
                foreach (var (partition, source) in newPartitions)
                {
                    PartitionItems.Add(new FlashPartitionItem(partition, source));
                }
                OverallStatusText = $"Ready to flash {PartitionItems.Count} partitions";
            }));
        }



        public void UpdateCurrentPartition(string partitionName, string status, double progress)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var item = PartitionItems.FirstOrDefault(p => p.Name.Equals(partitionName, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    item.StatusText = status;
                    item.ProgressValue = progress;
                    item.UpdateStatus(status);

                    ScrollToPartition(item);
                }

                var completedItems = PartitionItems.Count(p => p.ProgressValue >= 100);
                if (IsUsbUpdateMode)
                {
                    OverallProgress = progress;
                }
                else
                {
                    var totalProgress = PartitionItems.Sum(p => p.ProgressValue) / PartitionItems.Count;
                    OverallProgress = totalProgress;
                }

                OverallStatusText = $"Processing {partitionName}... ({completedItems + 1}/{PartitionItems.Count})";
                CurrentOperationText = $"Flashing {partitionName}: {status}";
            }));
        }

        public void CompletePartition(string partitionName, bool success, string message = null)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var item = PartitionItems.FirstOrDefault(p => p.Name.Equals(partitionName, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    item.Complete(success, message);
                }

                var completedItems = PartitionItems.Count(p => p.IsCompleted);
                OverallStatusText = $"Completed {completedItems}/{PartitionItems.Count} partitions";
            }));
        }

        public void SetOverallComplete(bool success, string message)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CanCancel = false;
                ShowCloseButton = true;

                var successCount = PartitionItems.Count(p => p.IsSuccess);
                var totalCount = PartitionItems.Count;

                OverallProgress = 100;

                if (success && successCount == totalCount)
                {
                    OverallStatusText = $"Successfully flashed all {totalCount} partitions!";
                    CurrentOperationText = "Flashing completed successfully.";
                }
                else
                {
                    OverallStatusText = $"Completed with {successCount}/{totalCount} successful";
                    CurrentOperationText = message ?? "Flashing completed with errors.";
                }
            }));
        }

        private void ScrollToPartition(FlashPartitionItem item)
        {
            try
            {
                var index = PartitionItems.IndexOf(item);
                if (index >= 0 && PartitionScrollViewer != null)
                {
                    var itemHeight = 60;
                    var scrollPosition = index * itemHeight;
                    PartitionScrollViewer.ScrollToVerticalOffset(Math.Max(0, scrollPosition - 100));
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                CanCancel = false;
                CurrentOperationText = "Cancelling operation and cleaning up...";


                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in PartitionItems.Where(p => !p.IsCompleted))
                    {
                        if (item.ProgressValue > 0 && item.ProgressValue < 100)
                        {
                            item.StatusText = "Cancelling...";
                        }
                    }
                });
            }
            catch (Exception ex)
            {
            }
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RequestClose?.Invoke(this, EventArgs.Empty);

                if (this.IsLoaded)
                {
                    this.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
