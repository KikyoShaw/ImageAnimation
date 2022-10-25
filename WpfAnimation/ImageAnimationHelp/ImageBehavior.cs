using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfAnimation.ImageAnimationHelp
{
    public static class ImageBehavior
    {
        public static readonly DependencyProperty SourceKeyProperty =
            DependencyProperty.RegisterAttached(
                "SourceKey",
                typeof(string),
                typeof(ImageBehavior),
                new PropertyMetadata(
                    null,
                    SourceKeyChanged));
        public static string GetSourceKey(Image image)
        {
            return (string)image.GetValue(SourceKeyProperty);
        }

        public static void SetSourceKey(Image image, string value)
        {
            image.SetValue(SourceKeyProperty, value);
        }

        private static void SourceKeyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (!(o is Image image))
                throw new ArgumentException("Only image controls are available", nameof(o));

            var oldValue = e.OldValue as string;
            var newValue = e.NewValue as string;

            if (ReferenceEquals(oldValue, newValue))
            {
                if (image.IsLoaded)
                {
                    if (!GetIsAnimationLoaded(image))
                        InitAnimation(image);
                }

                return;
            }

            if (!string.IsNullOrEmpty(oldValue))
            {
                image.Loaded -= ImageOnLoaded;
                image.Unloaded -= ImageOnUnloaded;
                image.IsVisibleChanged -= ImageOnIsVisibleChanged;
            }

            if (!string.IsNullOrEmpty(newValue))
            {
                image.Loaded += ImageOnLoaded;
                image.Unloaded += ImageOnUnloaded;
                image.IsVisibleChanged += ImageOnIsVisibleChanged;

                if (image.IsLoaded)
                    InitAnimation(image);
            }
        }

        public static bool GetAutoStart(Image image)
        {
            return (bool)image.GetValue(AutoStartProperty);
        }

        public static void SetAutoStart(Image image, bool value)
        {
            image.SetValue(AutoStartProperty, value);
        }

        public static readonly DependencyProperty AutoStartProperty =
            DependencyProperty.RegisterAttached("AutoStart", typeof(bool), typeof(ImageBehavior), new PropertyMetadata(true));

        public static AnimationController GetAnimationController(Image image)
        {
            return (AnimationController)image.GetValue(AnimationControllerProperty.DependencyProperty);
        }

        private static void SetAnimationController(DependencyObject obj, AnimationController value)
        {
            obj.SetValue(AnimationControllerProperty, value);
        }

        private static readonly DependencyPropertyKey AnimationControllerProperty =
            DependencyProperty.RegisterAttachedReadOnly("AnimationController", typeof(AnimationController), typeof(ImageBehavior), new PropertyMetadata(null));

        private static void ImageOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is Image {IsLoaded: true} img)) return;

            var controller = GetAnimationController(img);
            if (controller == null) return;
            var isVisible = (bool)e.NewValue;
            controller.SetSuspended(!isVisible);
        }

        private static void ImageOnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is Image image))
                return;

            var controller = GetAnimationController(image);
            controller?.Dispose();
        }

        private static void ImageOnLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is Image image))
                return;

            InitAnimation(image);
        }

        private static void InitAnimation(Image image)
        {
            var controller = GetAnimationController(image);
            controller?.Dispose();
            SetAnimationController(image, null);
            SetIsAnimationLoaded(image, false);

            var sourceKey = GetSourceKey(image);
            var animation = GetAnimation(image, sourceKey);
            if (animation == null || animation.KeyFrames.Count < 1) return;

            // For some reason, it sometimes throws an exception the first time... the second time it works.
            TryTwice(() => image.Source = (ImageSource)animation.KeyFrames[0].Value);

            controller = new AnimationController(image, animation, GetAutoStart(image));
            SetAnimationController(image, controller);
            image.RaiseEvent(new RoutedEventArgs(AnimationLoadedEvent));
        }

        private static void TryTwice(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                action();
            }
        }

        private static ObjectAnimationUsingKeyFrames GetAnimation(Image image, string sourceKey)
        {
            if (!AnimationCache.Instance.GetCacheEntry(sourceKey, out var cacheEntry))
                return null;

            var animation = new ObjectAnimationUsingKeyFrames
            {
                KeyFrames = cacheEntry.KeyFrames,
                Duration = cacheEntry.Duration,
                RepeatBehavior = GetActualRepeatBehavior(image),
                SpeedRatio = 1.0
            };

            return animation;
        }

        public static RepeatBehavior GetRepeatBehavior(Image obj)
        {
            return (RepeatBehavior)obj.GetValue(RepeatBehaviorProperty);
        }

        /// <summary>
        /// Sets the value of the <c>RepeatBehavior</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">The repeat behavior of the animated image.</param>
        public static void SetRepeatBehavior(Image obj, RepeatBehavior value)
        {
            obj.SetValue(RepeatBehaviorProperty, value);
        }

        public static readonly DependencyProperty RepeatBehaviorProperty =
            DependencyProperty.RegisterAttached(
              "RepeatBehavior",
              typeof(RepeatBehavior),
              typeof(ImageBehavior),
              new PropertyMetadata(
                  default(RepeatBehavior),
                  AnimationPropertyChanged));

        private static void AnimationPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (!(o is Image image))
                return;

            var sourceKey = GetSourceKey(image);

            if (string.IsNullOrEmpty(sourceKey)) return;

            if (image.IsLoaded)
                InitAnimation(image);
        }

        private static RepeatBehavior GetActualRepeatBehavior(Image imageControl)
        {
            // If specified explicitly, use this value
            var repeatBehavior = GetRepeatBehavior(imageControl);
            if (repeatBehavior != default(RepeatBehavior))
                return repeatBehavior;

            return RepeatBehavior.Forever;
        }

        private static readonly DependencyPropertyKey IsAnimationLoadedPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("IsAnimationLoaded", typeof(bool), typeof(ImageBehavior), new PropertyMetadata(false));

        public static readonly DependencyProperty IsAnimationLoadedProperty = IsAnimationLoadedPropertyKey.DependencyProperty;

        private static void SetIsAnimationLoaded(Image image, bool value)
        {
            image.SetValue(IsAnimationLoadedPropertyKey, value);
        }

        public static bool GetIsAnimationLoaded(Image image)
        {
            return (bool)image.GetValue(IsAnimationLoadedProperty);
        }

        public static readonly RoutedEvent AnimationLoadedEvent =
            EventManager.RegisterRoutedEvent(
                "AnimationLoaded",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ImageBehavior));

        public static void AddAnimationLoadedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null || handler == null)
                return;

            image.AddHandler(AnimationLoadedEvent, handler);
        }

        public static void RemoveAnimationLoadedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null || handler == null)
                return;

            image.RemoveHandler(AnimationLoadedEvent, handler);
        }

        public static readonly RoutedEvent AnimationCompletedEvent =
            EventManager.RegisterRoutedEvent(
                "AnimationCompleted",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ImageBehavior));

        public static void AddAnimationCompletedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null || handler == null)
                return;

            image.AddHandler(AnimationCompletedEvent, handler);
        }

        public static void RemoveAnimationCompletedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null || handler == null)
                return;

            image.RemoveHandler(AnimationCompletedEvent, handler);
        }

    }
}
