﻿using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace AnimationManager.Controls
{
    /// <summary>
    /// Based off https://searchcode.com/codesearch/view/10571351/
    /// </summary>
    public class TreeViewMultiSelectBehavior : Behavior<TreeView>
    {
        private TreeViewItem _anchorItem;

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(IList), typeof(TreeViewMultiSelectBehavior));

        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.RegisterAttached("IsSelected", typeof(bool), typeof(TreeViewMultiSelectBehavior),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedChanged));

        public static bool GetIsSelected(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSelectedProperty);
        }

        public static void SetIsSelected(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSelectedProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.AddHandler(TreeViewItem.UnselectedEvent, new RoutedEventHandler(OnTreeViewItemUnselected), true);
            AssociatedObject.AddHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(OnTreeViewItemSelected), true);
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RemoveHandler(TreeViewItem.UnselectedEvent, new RoutedEventHandler(OnTreeViewItemUnselected));
            AssociatedObject.RemoveHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(OnTreeViewItemSelected));
            base.OnDetaching();
        }

        private void OnTreeViewItemUnselected(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.None)
            {
                SetIsSelected((TreeViewItem)e.OriginalSource, false);
            }
        }

        private void OnTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as TreeViewItem;
            if (item.DataContext != null && item.DataContext.ToString() == "{DisconnectedItem}")
                return;

            if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) !=
                (ModifierKeys.Shift | ModifierKeys.Control))
            {
                switch ((Keyboard.Modifiers & ModifierKeys.Control))
                {
                    case ModifierKeys.Control:
                        ToggleSelect(item);
                        break;
                    default:
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                            AnchorMultiSelect(item);
                        else
                            SingleSelect(item);
                        break;
                }

            }
        }

        private static TreeView GetTree(TreeViewItem item)
        {
            FrameworkElement currentItem = item;
            while(!(VisualTreeHelper.GetParent(currentItem) is TreeView))
            {
                currentItem = (FrameworkElement)VisualTreeHelper.GetParent(currentItem);
            }

            return (TreeView)VisualTreeHelper.GetParent(currentItem);
        }

        private static void OnSelectedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var item = sender as TreeViewItem;
            var tree = GetTree(item);
            var msb = Interaction.GetBehaviors(tree).Single(b => b.GetType() == typeof(TreeViewMultiSelectBehavior)) as TreeViewMultiSelectBehavior;
            var selectedItems = msb?.SelectedItems;
            if (selectedItems != null)
            {
                if (GetIsSelected(item))
                    selectedItems.Add(item.DataContext ?? item);
                else
                    selectedItems.Remove(item.DataContext ?? item);
            }
        }

        private IEnumerable<TreeViewItem> GetExpandedTreeViewItems(ItemsControl container = null)
        {
            if (container == null)
                container = AssociatedObject;

            for(int i = 0; i < container.Items.Count; i++)
            {
                var item = container.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (item == null)
                    continue;

                yield return item;

                foreach (var subItem in GetExpandedTreeViewItems(item))
                    yield return subItem;
            }
        }

        private void AnchorMultiSelect(TreeViewItem newItem)
        {
            if (_anchorItem == null)
            {
                var selectedItems = GetExpandedTreeViewItems().Where(GetIsSelected).ToList();
                _anchorItem = (selectedItems.Count > 0 ? selectedItems[selectedItems.Count - 1] : GetExpandedTreeViewItems().FirstOrDefault());
                if (_anchorItem == null)
                    return;
            }

            var anchor = _anchorItem;
            var items = GetExpandedTreeViewItems();
            bool inSelectionRange = false;

            foreach(var item in items)
            {
                bool isEdge = item == anchor || item == newItem;
                if (isEdge)
                    inSelectionRange = !inSelectionRange;
                SetIsSelected(item, (inSelectionRange || isEdge));
            }
        }

        private void SingleSelect(TreeViewItem item)
        {
            foreach(var selectedItem in GetExpandedTreeViewItems().Where(x => x != null))
            {
                SetIsSelected(selectedItem, selectedItem == item);
            }

            _anchorItem = item;
        }

        private void ToggleSelect(TreeViewItem item)
        {
            SetIsSelected(item, !GetIsSelected(item));
            if (_anchorItem == null)
                _anchorItem = item;
        }
    }
}
