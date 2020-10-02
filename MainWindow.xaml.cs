﻿using AnimationManager.Controls;
using AnimationManager.Graphics;
using DungeonSphere.Graphics;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AnimationManager
{
    public partial class MainWindow : Window
    {
        private TreeView _atlasTreeView;
        private Button _buttonRemoveAtlas;
        private Canvas _mainRender;
        private ScaleTransform _mainRenderScale;
        private DispatcherTimer _gameTickTimer;
        private Rectangle _spriteOutline;
        private Rectangle _spriteDisplay;
        private DockPanel _propertiesPanel;
        private NumericUpDown _originX;
        private NumericUpDown _originY;
        private Image _originMarker;

        private SpritePreviewWindow _spritePreviewWindow;

        public ViewModel ViewModel { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            _gameTickTimer = new DispatcherTimer();
            _gameTickTimer.Tick += GameTickTimer_Tick;
            _gameTickTimer.Interval = TimeSpan.FromSeconds(1 / 60.0);
            _gameTickTimer.IsEnabled = true;

            _atlasTreeView = (TreeView)FindName("trv_Atlas");
            _buttonRemoveAtlas = (Button)FindName("btn_RemoveAtlas");
            _mainRender = (Canvas)FindName("cv_MainRender");
            _spriteOutline = (Rectangle)FindName("rect_SpriteOutline");
            _spriteDisplay = (Rectangle)FindName("rect_SpriteDisplay");
            _propertiesPanel = (DockPanel)FindName("dock_Properties");
            _originMarker = (Image)FindName("img_Origin");
            _originX = (NumericUpDown)FindName("num_OriginX");
            _originY = (NumericUpDown)FindName("num_OriginY");

            RenderOptions.SetBitmapScalingMode(_spriteOutline, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(_spriteDisplay, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(_originMarker, BitmapScalingMode.NearestNeighbor);

            _mainRenderScale = new ScaleTransform(3, 3);

            _spritePreviewWindow = new SpritePreviewWindow();
            _spritePreviewWindow.Show();
            _spritePreviewWindow.Topmost = true;

            StartNewFile();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _spritePreviewWindow.Close();
        }

        private void AddAtlas_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                AddExtension = true,
                Filter = "texture atlas files (*.atlas)|*.atlas",
                Title = "Add Texture Atlas",
                Multiselect = true
            };

            _spritePreviewWindow.Topmost = false;
            if (openFileDialog.ShowDialog() == true)
            {
                var invalidFiles = new List<string>();
                foreach (var filename in openFileDialog.FileNames)
                {
                    AddTextureAtlas(filename, invalidFiles);
                }

                if (invalidFiles.Count > 0)
                {
                    MessageBox.Show("Cannot add '" + string.Join("', '", invalidFiles)  + "' because it already exists.", "Add Texture Atlas", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            _spritePreviewWindow.Topmost = true;
        }

        private void RemoveAtlas_Click(object sender, RoutedEventArgs e)
        {
            // Remove all selected texture atlases
            foreach(var x in ViewModel.SelectedItems.ToList())
            {
                if (x is WpfTextureAtlas atlas)
                {
                    RemoveTextureAtlas(atlas);
                }
            }
        }

        private void TreeView_ItemsSelected(object sender, RoutedEventArgs e)
        {
            // Toggle Remove Atlas button based on which items are selected
            bool onlyTextureAtlasesSelected = true;
            foreach(var item in ViewModel.SelectedItems)
            {
                if (!(item is WpfTextureAtlas))
                {
                    onlyTextureAtlasesSelected = false;
                    break;
                }
            }

            _buttonRemoveAtlas.IsEnabled = onlyTextureAtlasesSelected;

            if (ViewModel.SelectedItems.Count == 1) // Single Item Selected
            {
                if (ViewModel.SelectedItems[0] is WpfSprite sprite)
                {
                    _spriteOutline.StrokeThickness = 1;

                    _spriteDisplay.Width = sprite.Regions[0].width;
                    _spriteDisplay.Height = sprite.Regions[0].height;
                    _spriteDisplay.Fill = sprite.Regions[0].ImageBrush;

                    UpdateMainRender();

                    _propertiesPanel.DataContext = sprite;

                    _spritePreviewWindow.SetSprite(sprite);
                }
            }
            else
            {
                _spriteDisplay.Fill = null;
                _spriteOutline.StrokeThickness = 0;
            }
        }

        /// <summary>
        /// Adds a single texture atlas into the project.
        /// </summary>
        private void AddTextureAtlas(string file, List<string> invalidFiles = null)
        {
            var atlasName = System.IO.Path.GetFileNameWithoutExtension(file);

            if (ViewModel.RegisteredTextureAtlases.Contains(atlasName))
            {
                if (invalidFiles != null)
                    invalidFiles.Add(atlasName);
                return;
            }

            // Load the atlas
            var atlas = new WpfTextureAtlas(file);
            ViewModel.RegisteredTextureAtlases.Add(atlasName);
            ViewModel.TextureAtlases.Add(atlas);
        }

        /// <summary>
        /// Removes a single texture atlas from the project
        /// </summary>
        private void RemoveTextureAtlas(WpfTextureAtlas atlas)
        {
            ViewModel.RegisteredTextureAtlases.Remove(atlas.Name);
            ViewModel.TextureAtlases.Remove(atlas);
        }

        private void StartNewFile()
        {
            ViewModel = new ViewModel();
            _atlasTreeView.Items.Clear();
            _atlasTreeView.DataContext = ViewModel;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawMainRender();
        }

        private void OriginX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
        {
            UpdateOriginMarker();
        }

        private void OriginY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
        {
            UpdateOriginMarker();
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            if (ViewModel.SelectedItems.Count == 1)
            {
                if (ViewModel.SelectedItems[0] is WpfSprite sprite)
                {
                    sprite.Update(_gameTickTimer.Interval.TotalMilliseconds);
                }
            }
        }

        private void DrawMainRender()
        {
            /*bool drawFinished = false;

            while (!drawFinished)
            {
                // Draw currently selected sprite if only one is selected
                if (ViewModel.SelectedItems.Count == 1)
                {
                    if (ViewModel.SelectedItems[0] is WpfSprite)
                    {
                        var sprite = ViewModel.SelectedItems[0] as WpfSprite;
                        _mainRender.Children.Clear();
                    }
                }
                drawFinished = true;
            }*/

        }

        private void MainRender_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMainRender();
            UpdateOriginMarker();
        }

        private void UpdateMainRender()
        {
            int x = (int)(_mainRender.ActualWidth - _spriteDisplay.Width) / 2;
            int y = (int)(_mainRender.ActualHeight - _spriteDisplay.Height) / 2;

            _mainRenderScale.CenterX = _spriteDisplay.Width / 2;
            _mainRenderScale.CenterY = _spriteDisplay.Height / 2;

            _spriteDisplay.RenderTransform = _mainRenderScale;

            Canvas.SetLeft(_spriteDisplay, x);
            Canvas.SetTop(_spriteDisplay, y);

            // Render the outline
            _spriteOutline.Width = _spriteDisplay.Width * _mainRenderScale.ScaleX + 2;
            _spriteOutline.Height = _spriteDisplay.Height * _mainRenderScale.ScaleY + 2;

            x = (int)(_mainRender.ActualWidth - _spriteDisplay.Width * _mainRenderScale.ScaleX) / 2;
            y = (int)(_mainRender.ActualHeight - _spriteDisplay.Height * _mainRenderScale.ScaleY) / 2;

            Canvas.SetLeft(_spriteOutline, x - 1);
            Canvas.SetTop(_spriteOutline, y - 1);
        }

        private void UpdateOriginMarker()
        {
            // Update the origin marker position
            int originx = _originX.Value;
            int originy = _originY.Value;

            int x = (int)(_mainRender.ActualWidth - _spriteDisplay.Width * _mainRenderScale.ScaleX) / 2;
            int y = (int)(_mainRender.ActualHeight - _spriteDisplay.Height * _mainRenderScale.ScaleY) / 2;

            x -= (int)_originMarker.Source.Width / 2;
            y -= (int)_originMarker.Source.Height / 2;

            x += (int)(originx * _mainRenderScale.ScaleX);
            y += (int)(originy * _mainRenderScale.ScaleY);

            Canvas.SetLeft(_originMarker, x);
            Canvas.SetTop(_originMarker, y);

            _spritePreviewWindow.UpdatePreviewRender();
        }
    }
}
