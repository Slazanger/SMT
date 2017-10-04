using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfHelpers.WpfControls.EditableLabel
{
    [TemplatePart(Name = @"PART_mainLabel", Type = typeof (TextBox))]
    [TemplatePart(Name = @"PART_mainGrid", Type = typeof (Grid))]
    public class EditableLabel : Control
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof (string), typeof (EditableLabel),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty EnableEditingProperty =
            DependencyProperty.Register("EnableEditing", typeof (bool), typeof (EditableLabel),
                new PropertyMetadata(default(bool), EnableEdititngChanged));

        public static readonly DependencyProperty EditButtonContentProperty =
            DependencyProperty.Register("EditButtonContent", typeof (object), typeof (EditableLabel),
                new PropertyMetadata("X"));

        /// <summary>
        ///     Gets or sets text
        /// </summary>
        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Gets or sets flag wheither enable or disable edit mode
        /// </summary>
        public bool EnableEditing
        {
            get { return (bool) GetValue(EnableEditingProperty); }
            set { SetValue(EnableEditingProperty, value); }
        }

        public object EditButtonContent
        {
            get { return GetValue(EditButtonContentProperty); }
            set { SetValue(EditButtonContentProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var txt = GetTemplateChild("PART_mainLabel") as TextBox;

            if (txt != null)
            {
                txt.KeyUp += txt_KeyUp;
            }

            var grid = GetTemplateChild("PART_mainGrid") as Grid;
            if (grid != null)
            {
                grid.MouseLeftButtonDown += GridOnMouseLeftButtonDown;
            }
        }

        private void GridOnMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        {
            if (args.ClickCount > 1)
            {
                args.Handled = true;
                EnableEditing = true;
            }
        }

        private void txt_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EnableEditing = false;
            }
            else
            {
                if (!IsMouseCaptured)
                    Mouse.Capture(this, CaptureMode.SubTree);
            }
        }

        private static void EnableEdititngChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            if ((bool) args.NewValue)
            {
                var edit = dependencyObject as EditableLabel;

                if (edit != null)
                {
                    //var txt = edit.GetTemplateChild("PART_mainLabel") as TextBox;
                    Mouse.Capture(edit, CaptureMode.SubTree);
                    edit.AddHandler();
                    //if (txt != null) txt.Focus();
                }
            }
        }

        private void AddHandler()
        {
            AddHandler(Mouse.PreviewMouseDownOutsideCapturedElementEvent,
                new MouseButtonEventHandler(HandleClickOutsideOfControl), true);
        }

        private void HandleClickOutsideOfControl(object sender, MouseButtonEventArgs e)
        {
            //do stuff (eg close drop down)
            EnableEditing = false;
            ReleaseMouseCapture();
        }
    }
}