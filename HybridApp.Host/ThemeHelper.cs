using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace HybridApp.Host
{
    internal class ThemeHelper
    {
        public static void ApplyColorAnimationThemeColor(string storyboardTarget, int index, Color color)
        {
            if (Application.Current.Resources[storyboardTarget] is Storyboard storyboard)
            {
                var colorAnimation = storyboard.Children[index] as ColorAnimation;
                if (colorAnimation != null)
                {
                    colorAnimation.To = color;
                }
            }
        }
    }

    public class DynamicButtonAnimator
    {
        private readonly Button _button;
        private Storyboard _hoverStoryboard;
        private Storyboard _pressedStoryboard;
        private Storyboard _exitStoryboard;

        public DynamicButtonAnimator(Button button)
        {
            _button = button;

            _button.Background = new SolidColorBrush(Colors.Transparent);

            _button.PointerEntered += OnPointerEntered;
            _button.PointerExited += OnPointerExited;
            _button.PointerPressed += OnPointerPressed;
            _button.PointerReleased += OnPointerReleased;
        }

        public void UpdateColors(Color hoverColor, Color pressedColor)
        {
            // Dispose of any existing storyboards to prevent conflicts
            _hoverStoryboard?.Stop();
            _pressedStoryboard?.Stop();
            _exitStoryboard?.Stop();

            // Create new animations with updated colors
            _hoverStoryboard = CreateStoryboard(Colors.Transparent, hoverColor, TimeSpan.FromMilliseconds(200));
            _pressedStoryboard = CreateStoryboard(Colors.Transparent, pressedColor, TimeSpan.FromMilliseconds(200));
            _exitStoryboard = CreateStoryboard(hoverColor, Colors.Transparent, TimeSpan.FromMilliseconds(200));
        }

        private void OnPointerEntered(object sender, RoutedEventArgs e)
        {
            PlayStoryboard(_hoverStoryboard);
        }

        private void OnPointerExited(object sender, RoutedEventArgs e)
        {
            PlayStoryboard(_exitStoryboard);
        }

        private void OnPointerPressed(object sender, RoutedEventArgs e)
        {
            PlayStoryboard(_pressedStoryboard);
        }

        private void OnPointerReleased(object sender, RoutedEventArgs e)
        {
            PlayStoryboard(_hoverStoryboard);
        }

        private Storyboard CreateStoryboard(Color from, Color to, TimeSpan duration)
        {
            var animation = new ColorAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(duration),
                EnableDependentAnimation = true // Required for UI event-based animations
            };

            var storyboard = new Storyboard();
            var brush = _button.Background as SolidColorBrush;

            // Ensure the button's background is initialized
            if (brush == null || brush.Color == Colors.Transparent)
            {
                brush = new SolidColorBrush(Colors.Transparent);
                _button.Background = brush;
            }

            Storyboard.SetTarget(animation, brush);
            Storyboard.SetTargetProperty(animation, "Color");

            storyboard.Children.Add(animation);
            return storyboard;
        }

        private void PlayStoryboard(Storyboard storyboard)
        {
            storyboard.Stop();
            storyboard.Begin();
        }
    }

}
