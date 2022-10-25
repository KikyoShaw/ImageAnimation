using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WpfAnimation.ImageAnimationHelp
{
    public class AnimationController : IDisposable
    {
        //private static readonly DependencyPropertyDescriptor SourceDescriptor;

        //static AnimationController()
        //{
        //    SourceDescriptor = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
        //}

        private readonly Image _image;
        private readonly ObjectAnimationUsingKeyFrames _animation;
        private readonly AnimationClock _clock;
        private readonly ClockController _clockController;

        public bool IsPaused { get; private set; }

        public AnimationController(Image image, ObjectAnimationUsingKeyFrames animation, bool autoStart)
        {
            _image = image;
            _animation = animation;
            _animation.Completed += AnimationOnCompleted;
            _clock = _animation.CreateClock();
            _clockController = _clock.Controller;
            //SourceDescriptor.AddValueChanged(image, );

            _clockController.Pause();
            _image.ApplyAnimationClock(Image.SourceProperty, _clock);

            IsPaused = !autoStart;
            if (autoStart)
                _clockController.Resume();
        }

        private void AnimationOnCompleted(object sender, EventArgs e)
        {
            _image.RaiseEvent(new RoutedEventArgs(ImageBehavior.AnimationCompletedEvent, _image));
        }

        public bool IsComplete => _clock.CurrentState == ClockState.Filling;
        public int FrameCount => _animation.KeyFrames.Count;
        public TimeSpan Duration =>
            _animation.Duration.HasTimeSpan
                ? _animation.Duration.TimeSpan
                : TimeSpan.Zero;

        public void Pause()
        {
            IsPaused = true;
            _clockController.Pause();
        }

        public void Play()
        {
            IsPaused = false;
            if (!_isSuspended)
                _clockController.Resume();
        }

        public void Stop()
        {
            Pause();

            GotoFrame(0);
        }

        private bool _isSuspended;
        public void SetSuspended(bool isSuspended)
        {
            if (isSuspended == _isSuspended)
                return;

            var wasSuspended = _isSuspended;
            _isSuspended = isSuspended;
            if (wasSuspended)
            {
                if (!IsPaused)
                {
                    _clockController.Resume();
                }
            }
            else
            {
                _clockController.Pause();
            }
        }

        public void GotoFrame(int index)
        {
            var frame = _animation.KeyFrames[index];
            _clockController.Seek(frame.KeyTime.TimeSpan, TimeSeekOrigin.BeginTime);
        }

        public int CurrentFrame
        {
            get
            {
                var time = _clock.CurrentTime;
                var frameAndIndex =
                    _animation.KeyFrames
                        .Cast<ObjectKeyFrame>()
                        .Select((f, i) => new { Time = f.KeyTime.TimeSpan, Index = i })
                        .FirstOrDefault(fi => fi.Time >= time);
                if (frameAndIndex != null)
                    return frameAndIndex.Index;
                return -1;
            }
        }

        ~AnimationController()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image.BeginAnimation(Image.SourceProperty, null);
                _animation.Completed -= AnimationOnCompleted;
                //SourceDescriptor.RemoveValueChanged(_image, ImageSourceChanged);
                _image.Source = null;
            }
        }
    }
}
