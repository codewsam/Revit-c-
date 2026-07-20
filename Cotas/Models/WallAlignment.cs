using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace G3Plugins.Models
{
    /// <summary>
    /// Conjunto de paredes retas, colineares e conectadas ponta-a-ponta,
    /// ja ordenadas ao longo do eixo, com as extremidades reais do
    /// alinhamento completo.
    /// </summary>
    internal class WallAlignment
    {
        public List<Wall> Walls { get; set; }
        public XYZ AxisDirection { get; set; }
        public XYZ PerpendicularDirection { get; set; }
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }

        public WallAlignment()
        {
            Walls = new List<Wall>();
        }
    }
}