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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Wpf.Ui.Controls;

namespace Kirin_Tool.UI
{
    public partial class PartitionSelectorDialog : ContentDialog, INotifyPropertyChanged
    {
        private ObservableCollection<PartitionInfo> _partitions;
        private string _statusText;
        private bool _updatingSelectAll = false;
        private bool? _selectAllState = false;

        public ObservableCollection<PartitionInfo> Partitions
        {
            get => _partitions;
            set
            {
                _partitions = value;
                OnPropertyChanged(nameof(Partitions));
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public bool HasSelectedPartitions => Partitions?.Any(p => p.IsSelected) == true;

        public bool? SelectAllState
        {
            get => _selectAllState;
            set
            {
                if (_selectAllState != value)
                {
                    _selectAllState = value;
                    OnPropertyChanged(nameof(SelectAllState));

                    if (!_updatingSelectAll && value.HasValue)
                    {
                        ApplySelectAllState(value.Value);
                    }
                }
            }
        }

        public List<PartitionInfo> SelectedPartitions { get; private set; }
        public bool DialogResult { get; private set; }

        public PartitionSelectorDialog(List<PartitionInfo> partitions)
        {
            InitializeComponent();

            Partitions = new ObservableCollection<PartitionInfo>(partitions ?? new List<PartitionInfo>());

            foreach (var partition in Partitions)
            {
                partition.PropertyChanged += OnPartitionPropertyChanged;
            }

            DataContext = this;
            UpdateStatusText();
            UpdateSelectAllState();

            this.Closed += OnDialogClosed;
        }

        private void OnPartitionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PartitionInfo.IsSelected))
            {
                if (!_updatingSelectAll)
                {
                    OnPropertyChanged(nameof(HasSelectedPartitions));
                    UpdateSelectAllState();
                    UpdateStatusText();
                }
            }
        }

        private void UpdateSelectAllState()
        {
            if (Partitions == null || !Partitions.Any())
            {
                SelectAllState = false;
                return;
            }

            var selectedCount = Partitions.Count(p => p.IsSelected);

            if (selectedCount == 0)
                SelectAllState = false;
            else if (selectedCount == Partitions.Count)
                SelectAllState = true;
            else
                SelectAllState = null;
        }

        private void ApplySelectAllState(bool selectAll)
        {
            _updatingSelectAll = true;

            try
            {
                foreach (var partition in Partitions)
                {
                    partition.IsSelected = selectAll;
                }
            }
            finally
            {
                _updatingSelectAll = false;
            }

            OnPropertyChanged(nameof(HasSelectedPartitions));
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (Partitions == null)
            {
                StatusText = "No partitions available";
                return;
            }

            var selectedCount = Partitions.Count(p => p.IsSelected);
            var totalCount = Partitions.Count;

            if (selectedCount == 0)
            {
                StatusText = $"{totalCount} partitions available";
            }
            else
            {
                var totalSize = Partitions.Where(p => p.IsSelected).Sum(p => (long)p.Size);
                StatusText = $"{selectedCount} of {totalCount} partitions selected ({FormatBytes(totalSize)})";
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";

            double size = bytes;
            string[] units = { "B", "KB", "MB", "GB" };
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:F1} {units[unitIndex]}";
        }

        private void FlashButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            SetSelectedPartitions();
            this.Hide();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Hide();
        }

        public void SetSelectedPartitions()
        {
            SelectedPartitions = Partitions?.Where(p => p.IsSelected).ToList() ?? new List<PartitionInfo>();
        }

        private void OnDialogClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            if (Partitions != null)
            {
                foreach (var partition in Partitions)
                {
                    partition.PropertyChanged -= OnPartitionPropertyChanged;
                }
            }

            this.Closed -= OnDialogClosed;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
