using Autodesk.Revit.DB;

namespace G3Plugins.Models
{
    /// <summary>
    /// As duas Line 3D (ja com offset perpendicular aplicado) onde as
    /// cotas de detalhe e total serao desenhadas.
    /// </summary>
    internal class DimensionLineLayout
    {
        public Line LinhaDetalhe { get; set; }
        public Line LinhaTotal { get; set; }
    }
}