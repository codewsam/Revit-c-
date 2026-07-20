using Autodesk.Revit.DB;

namespace G3Plugins.Models
{
    internal enum OpeningCategory
    {
        Porta,
        Janela
    }

    /// <summary>
    /// Uma abertura hospedada (porta/janela) com os dois pontos de
    /// referencia (inicio/fim = largura) ja resolvidos.
    /// </summary>
    internal class OpeningInfo
    {
        public FamilyInstance Instance { get; set; }
        public OpeningCategory Category { get; set; }
        public ElementId HostWallId { get; set; }
        public DimensionReferencePoint StartPoint { get; set; }
        public DimensionReferencePoint EndPoint { get; set; }

        public OpeningInfo(FamilyInstance instance, OpeningCategory category, ElementId hostWallId)
        {
            Instance = instance;
            Category = category;
            HostWallId = hostWallId;
        }
    }
}