﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SMTx.Views.Views;

public class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
