#region

using System;
using System.Windows;
using System.Windows.Media.Animation;

#endregion

namespace WpfHelpers.Animation
{




    /// <summary>
    ///     Animates a grid length value just like the DoubleAnimation animates a double value
    /// 
    /// ColumnDefinition column = parent.ColumnDefinitions.First();
    /// Storyboard storyboard = new Storyboard();
    ///
    /// Duration duration = new Duration(TimeSpan.FromMilliseconds(500));
    /// CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseOut };
    ///
    /// GridLengthAnimation animation = new GridLengthAnimation
    /// {
    ///     Duration = duration
    /// };
    /// storyboard.Children.Add(animation);
    ///         animation.From = new GridLength(1.0, GridUnitType.Star);
    /// animation.To = new GridLength(0.0);
    /// Storyboard.SetTarget(animation, column);
    /// Storyboard.SetTargetProperty(animation, new PropertyPath("Width"));
    /// 
    /// storyboard.Begin();
    /// 
    /// </summary>
    public class GridLengthAnimation : AnimationTimeline
    {
        /// <summary>
        ///     Dependency property for the From property
        /// </summary>
        public static readonly DependencyProperty FromProperty = DependencyProperty.Register("From", typeof (GridLength),
            typeof (GridLengthAnimation));

        /// <summary>
        ///     Dependency property for the To property
        /// </summary>
        public static readonly DependencyProperty ToProperty = DependencyProperty.Register("To", typeof (GridLength),
            typeof (GridLengthAnimation));

        /// <summary>
        ///     Returns the type of object to animate
        /// </summary>
        public override Type TargetPropertyType
        {
            get { return typeof (GridLength); }
        }

        /// <summary>
        ///     CLR Wrapper for the From depenendency property
        /// </summary>
        public GridLength From
        {
            get { return (GridLength) GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        /// <summary>
        ///     CLR Wrapper for the To property
        /// </summary>
        public GridLength To
        {
            get { return (GridLength) GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        /// <summary>
        ///     Creates an instance of the animation object
        /// </summary>
        /// <returns>Returns the instance of the GridLengthAnimation</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }

        /// <summary>
        ///     Animates the grid let set
        /// </summary>
        /// <param name="defaultOriginValue">The original value to animate</param>
        /// <param name="defaultDestinationValue">The final value</param>
        /// <param name="animationClock">The animation clock (timer)</param>
        /// <returns>Returns the new grid length to set</returns>
        public override object GetCurrentValue(object defaultOriginValue,
            object defaultDestinationValue, AnimationClock animationClock)
        {
            var fromVal = ((GridLength) GetValue(FromProperty)).Value;
            //check that from was set from the caller
            if (fromVal == 1)
                //set the from as the actual value
                fromVal = ((GridLength) defaultDestinationValue).Value;

            var toVal = ((GridLength) GetValue(ToProperty)).Value;

            if (fromVal > toVal)
                return new GridLength((1 - animationClock.CurrentProgress.Value)*(fromVal - toVal) + toVal,
                    GridUnitType.Star);
            return new GridLength(animationClock.CurrentProgress.Value*(toVal - fromVal) + fromVal, GridUnitType.Star);
        }
    }
}