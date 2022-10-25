using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfAnimation
{
    public sealed class Animation : UserControl
    {
        public Image ImageCache = null;

        public Animation()
        {
            ImageCache = new Image
            {
                Visibility = Visibility.Collapsed
            };
            this.AddChild(ImageCache);
        }

        public static readonly DependencyProperty IsShowProperty = DependencyProperty.Register(
            "IsShow", typeof(bool), typeof(Animation),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsShowPropertyChange)));

        public bool IsShow
        {
            get => (bool)GetValue(IsShowProperty);
            set => SetValue(IsShowProperty, value);
        }
        private static void OnIsShowPropertyChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Animation obj = ((Animation)d as Animation);
            if (obj != null)
                obj.DoShowStageChange();
        }

        private void DoShowStageChange()
        {
            ImageCache.Visibility = IsShow ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
