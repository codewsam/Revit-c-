using Autodesk.Revit.DB;

namespace G3Plugins.Models
{
    /// <summary>
    /// As duas ReferenceArray prontas para virar Dimension: a linha de
    /// detalhe (cadeia completa) e a linha total (so as extremidades).
    /// </summary>
    internal class DimensionChains
    {
        public ReferenceArray Detalhe { get; set; }
        public ReferenceArray Total { get; set; }
    }
}