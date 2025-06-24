using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SMT.Helpers;

/// <summary>
/// Simple Element pool for reusable UI Elements.
/// </summary>
/// <typeparam name="T">The pooled elements type, must be (derived from) Shape.</typeparam>
public class ElementPool<T> where T : UIElement, new()
{
    private readonly Stack<T> _pool = new();

    /// <summary>
    /// Get an element out of the pool or create a new one
    /// if there is none left.
    /// </summary>
    /// <returns>An element of the desired type.</returns>
    public T Get()
    {
        if (_pool.Count > 0)
        {
            T item = _pool.Pop();
            item.Visibility = Visibility.Visible;
            return item;
        }

        return new T();
    }

    /// <summary>
    /// Hides and element, removes it from the canvas and
    /// returns it to the pool.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="item"></param>
    public void Release(Canvas canvas, T item)
    {
        if (item == null) return;
        item.Visibility = Visibility.Collapsed;
        if (canvas.Children.Contains(item))
        {
            canvas.Children.Remove(item);
        }
        _pool.Push(item);
    }
}