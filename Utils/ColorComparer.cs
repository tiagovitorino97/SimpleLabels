using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleLabels.Utils
{
    /// <summary>
    /// Compares Color32 with tolerance for grouping similar colors (e.g. when sampling from item icons).
    /// </summary>
    public class ColorComparer : IEqualityComparer<Color32>
    {
        private const int ColorTolerance = 15;

        public bool Equals(Color32 x, Color32 y)
        {
            return Math.Abs(x.r - y.r) <= ColorTolerance &&
                   Math.Abs(x.g - y.g) <= ColorTolerance &&
                   Math.Abs(x.b - y.b) <= ColorTolerance;
        }

        public int GetHashCode(Color32 color)
        {
            var r = color.r / ColorTolerance;
            var g = color.g / ColorTolerance;
            var b = color.b / ColorTolerance;
            return (r << 16) | (g << 8) | b;
        }
    }
}
