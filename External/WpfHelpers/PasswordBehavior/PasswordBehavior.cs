using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace WpfHelpers.PasswordBehavior
{
    /// <summary>
    /// Used for binding PasswordBox to string
    /// 
    /// <example>
    ///<PasswordBox>
    ///    <i:Interaction.Behaviors>
    ///        <style:PasswordBehavior Password = "{Binding Password, Mode=TwoWay}" />
    ///    </i:Interaction.Behaviors>
    ///</PasswordBox>
    /// </example>
    /// </summary>
    public class PasswordBehavior : Behavior<PasswordBox>
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(PasswordBehavior),
                new PropertyMetadata(default(string)));

        private bool _skipUpdate;

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        protected override void OnAttached()
        {
            AssociatedObject.PasswordChanged += PasswordBox_PasswordChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PasswordChanged -= PasswordBox_PasswordChanged;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == PasswordProperty)
                if (!_skipUpdate)
                {
                    _skipUpdate = true;
                    AssociatedObject.Password = e.NewValue as string;
                    _skipUpdate = false;
                }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _skipUpdate = true;
            Password = AssociatedObject.Password;
            _skipUpdate = false;
        }
    }
}