using UnityEngine;
using System.Collections.Generic;

namespace Mesocyclone
{
    /// <summary>
    /// Extension methods for <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Color.html">Color</a>
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Creates a new color from 8-bit RGBA values (0-255).
        /// </summary>
        /// <returns>A new color.</returns>
        public static Color Color8(this Color color, byte r, byte g, byte b, byte a = 255)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        /// <summary>
        /// Mixes a collection of colors together, returning the average color.
        /// </summary>
        /// <param name="colors">The collection of colors to mix.</param>
        /// <returns>The mixed color.</returns>
        public static Color Mix(this IEnumerable<Color> colors) // cant use params since this is the legacy extension method :/
        {
            float r = 0f, g = 0f, b = 0f, a = 0f;
            int count = 0;

            foreach (Color color in colors)
            {
                Color linear = color.linear;
                
                r += linear.r;
                g += linear.g;
                b += linear.b;
                a += linear.a;

                count++;
            }

            if (count is 0)
                return new Color().linear;
            
            return new Color(r / count, g / count, b / count, a / count).gamma;
        }
    }
}