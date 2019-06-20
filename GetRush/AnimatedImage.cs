using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace GetRush
{
    /// <inheritdoc />
    /// <summary> 
    /// An Image control that supports animated GIFs. 
    /// </summary> 
    /// <remarks>
    /// https://stackoverflow.com/questions/210922/how-do-i-get-an-animated-gif-to-work-in-wpf?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa
    /// </remarks>
    public class AnimatedImage : Image
    {
        static AnimatedImage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedImage), new FrameworkPropertyMetadata(typeof(AnimatedImage)));
            VisibilityProperty.OverrideMetadata(typeof(AnimatedImage), new FrameworkPropertyMetadata(VisibilityPropertyChanged));
        }

        #region Public properties

        /// <summary> 
        /// Gets / sets the number of the current frame. 
        /// </summary> 
        public int FrameIndex
        {
            get => (int)GetValue(FrameIndexProperty);
            set => SetValue(FrameIndexProperty, value);
        }

        /// <summary>
        /// Get the BitmapFrame List.
        /// </summary>
        public List<BitmapFrame> Frames { get; private set; }

        /// <summary>
        /// Get or set the repeatBehavior of the animation when source is gif formart.This is a dependency object.
        /// </summary>
        public RepeatBehavior AnimationRepeatBehavior
        {
            get => (RepeatBehavior)GetValue(AnimationRepeatBehaviorProperty);
            set => SetValue(AnimationRepeatBehaviorProperty, value);
        }

        public new ImageSource Source
        {
            get => (ImageSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Uri UriSource
        {
            get => (Uri)GetValue(UriSourceProperty);
            set => SetValue(UriSourceProperty, value);
        }

        #endregion

        private static void VisibilityPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is AnimatedImage animatedImage)
            {
                if ((Visibility)e.NewValue == Visibility.Visible)
                {
                    (animatedImage).StartAnimation();
                }
                else
                {
                    (animatedImage).StopAnimation();
                }
            }
        }

        /// <summary>
        /// Starts the animation
        /// </summary>
        public void StartAnimation()
        {
            BeginAnimation(FrameIndexProperty, Animation);
        }

        /// <summary>
        /// Stops the animation
        /// </summary>
        public void StopAnimation()
        {
            BeginAnimation(FrameIndexProperty, null);
        }

        #region Protected interface

        /// <summary> 
        /// Provides derived classes an opportunity to handle changes to the Source property. 
        /// </summary> 
        protected virtual void OnSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            ClearAnimation();
            BitmapImage source;
            if (e.NewValue is Uri uri)
            {
                source = new BitmapImage();
                source.BeginInit();
                source.UriSource = uri;
                source.CacheOption = BitmapCacheOption.OnLoad;
                source.EndInit();
            }
            else if (e.NewValue is BitmapImage image)
            {
                source = image;
            }
            else if (e.NewValue is BitmapFrame bitmapFrame)
            {
                PrepareAnimation(bitmapFrame?.Decoder);
                return;
            }
            else
            {
                return;
            }
            if (source.StreamSource != null)
            {
                PrepareAnimation(BitmapDecoder.Create(source.StreamSource, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnLoad));
            }
            else if (source.UriSource != null)
            {
                PrepareAnimation(BitmapDecoder.Create(source.UriSource, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnLoad));
            }
        }

        #endregion

        #region Private properties

        private Int32Animation Animation { get; set; }
        private bool IsAnimationWorking { get; set; }

        #endregion

        #region Private methods

        private void ClearAnimation()
        {
            if (Animation != null)
            {
                BeginAnimation(FrameIndexProperty, null);
            }

            IsAnimationWorking = false;
            Animation = null;
            this.Frames = null;
        }

        private void PrepareAnimation(BitmapDecoder decoder)
        {
            if (decoder == null)
            {
                return;
            }
            if (decoder.Frames.Count == 1)
            {
                base.Source = decoder.Frames[0];
                return;
            }

            this.Frames = decoder.Frames.ToList();

            PrepareAnimation();
        }

        private void PrepareAnimation()
        {
            Animation =
                new Int32Animation(
                    0,
                    this.Frames.Count - 1,
                    new Duration(
                        new TimeSpan(
                            0,
                            0,
                            0,
                            this.Frames.Count / 20,
                            (this.Frames.Count % 20) * 100)))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                };

            base.Source = this.Frames[0];
            BeginAnimation(FrameIndexProperty, Animation);
            IsAnimationWorking = true;
        }

        private static void ChangingFrameIndex
            (DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            if (!(dp is AnimatedImage animatedImage) || !animatedImage.IsAnimationWorking)
            {
                return;
            }

            var frameIndex = (int)e.NewValue;
            ((Image)animatedImage).Source = animatedImage.Frames[frameIndex];
            animatedImage.InvalidateVisual();
        }

        /// <summary> 
        /// Handles changes to the Source property. 
        /// </summary> 
        private static void OnSourceChanged
            (DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            ((AnimatedImage)dp).OnSourceChanged(e);
        }

        #endregion

        #region Dependency Properties

        /// <summary> 
        /// FrameIndex Dependency Property 
        /// </summary> 
        public static readonly DependencyProperty FrameIndexProperty =
            DependencyProperty.Register(
                "FrameIndex",
                typeof(int),
                typeof(AnimatedImage),
                new UIPropertyMetadata(0, ChangingFrameIndex));

        /// <summary> 
        /// Source Dependency Property 
        /// </summary> 
        public new static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                "Source",
                typeof(ImageSource),
                typeof(AnimatedImage),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnSourceChanged));

        /// <summary>
        /// AnimationRepeatBehavior Dependency Property
        /// </summary>
        public static readonly DependencyProperty AnimationRepeatBehaviorProperty =
            DependencyProperty.Register(
            "AnimationRepeatBehavior",
            typeof(RepeatBehavior),
            typeof(AnimatedImage),
            new PropertyMetadata(null));

        public static readonly DependencyProperty UriSourceProperty =
            DependencyProperty.Register(
            "UriSource",
            typeof(Uri),
            typeof(AnimatedImage),
                    new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnSourceChanged));

        #endregion
    }
}
