﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SMTx.Views.Documents;

public class DocumentView : UserControl
{
    public DocumentView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
