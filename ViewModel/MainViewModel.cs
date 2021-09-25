﻿using AnimationEditor.IO;
using PropertyTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AnimationEditor.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// File name without extension
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Full file path
        /// </summary>
        private string SavePath { get; set; }

        public bool UnsavedChanges { get; private set; }

        //public SpritePreviewWindow SpritePreviewWindow { get; } = new SpritePreviewWindow();

        public TextureAtlasesViewModel TextureAtlasesVM { get; }
        public MainCanvasViewModel MainCanvasVM { get; }
        public SpritePropertiesViewModel SpritePropertiesVM { get; }

        public ICommand ClosingCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }

        public MainViewModel()
        {
            TextureAtlasesVM = new TextureAtlasesViewModel();
            MainCanvasVM = new MainCanvasViewModel();
            SpritePropertiesVM = new SpritePropertiesViewModel();

            TextureAtlasesVM.SelectionChanged += MainCanvasVM.TextureAtlasSelectionChanged;
            TextureAtlasesVM.SelectionChanged += SpritePropertiesVM.TextureAtlasSelectionChanged;

            TextureAtlasesVM.OnFileModified += FileModifiedEvent;
            SpritePropertiesVM.OnFileModified += FileModifiedEvent;

            ClosingCommand = new RelayCommand(x => ClosingExecute(x));
            ExitCommand = new RelayCommand(x => ExitExecute(x));
            NewCommand = new RelayCommand(_ => NewExecute(null));
            SaveCommand = new RelayCommand(_ => SaveExecute(null));

            FileName = "";
            SavePath = "";
        }

        private void FileModifiedEvent(object sender)
        {
            UnsavedChanges = true;
        }

        private void ClosingExecute(object parameters)
        {
            var e = (CancelEventArgs)parameters;

            if (!PromptUnsavedChanges())
                e.Cancel = true;
        }

        private void ExitExecute(object parameters)
        {
            var window = (Window)parameters;
            window.Close();
        }

        private void NewExecute(object parameters)
        {
            if (PromptUnsavedChanges())
            {
                UnsavedChanges = false;
                FileName = "";
                SavePath = "";
                TextureAtlasesVM.Reset();
                SpritePropertiesVM.Reset();
            }
        }

        private void SaveExecute(object parameters)
        {
            PerformSave();
        }

        /// <summary>
        /// Prompts user to save file if unsaved changes exists.
        /// </summary>
        /// <returns>True if OK to close app. False if dialog was cancelled.</returns>
        private bool PromptUnsavedChanges()
        {
            // No unsaved changes detected
            if (!UnsavedChanges)
                return true;

            // Prompt for saving
            var result = MessageBox.Show($"Do you want to save changes to {FileName}? Unsaved changes will be lost!", "Save File?", MessageBoxButton.YesNoCancel);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    return PerformSave();
                case MessageBoxResult.No:
                    return true;
                case MessageBoxResult.Cancel:
                    return false;
            }

            return true;
        }

        private bool PerformSave()
        {
            /*if (SavePath.Length <= 0)
            {
                return PerformSaveFile("Save File");
            }
            else*/
            {
                FileWriter.Write(FileName, TextureAtlasesVM.AtlasRoot);
                UnsavedChanges = false;
                return true;
            }
        }

        private bool PerformSaveFile(string title)
        {
            /*var saveFileDialog = new SaveFileDialog()
            {
                Title = title,
                Filter = "animation files (*.anim)|*.anim",
                AddExtension = true
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                MainWindowVM.UnsavedChanges = false;
                MainWindowVM.SavePath = saveFileDialog.FileName;
                MainWindowVM.FileName = System.IO.Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                FileWriter.Write(saveFileDialog.FileName, TextureAtlasViewModel);

                return true;
            }

            return false;*/
            return true;
        }

        /*public void Clear()
        {
            FileName = "Untitled";
            SavePath = "";
            UnsavedChanges = false;
            SpritePreviewWindow.DataContext = null;
        }*/
    }
}
