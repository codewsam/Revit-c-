using Autodesk.Revit.DB;

namespace G3Plugins.Models
{
    internal enum ReferencePointType
    {
        Extremidade,
        Intersecao,
        AberturaInicio,
        AberturaFim
    }

    /// <summary>
    /// Um ponto da cadeia de cotagem: referencia valida do Revit + posicao
    /// ao longo do eixo do alinhamento + tipo (usado para prioridade e
    /// deduplicacao no ReferenceCollector).
    /// </summary>
    internal class DimensionReferencePoint
    {
        public Reference Reference { get; set; }
        public double PositionOnAxis { get; set; }
        public ReferencePointType Type { get; set; }
        public ElementId SourceElementId { get; set; }
        public string Description { get; set; }

        public DimensionReferencePoint(Reference reference, double positionOnAxis, ReferencePointType type, ElementId sourceElementId, string description)
        {
            Reference = reference;
            PositionOnAxis = positionOnAxis;
            Type = type;
            SourceElementId = sourceElementId;
            Description = description;
        }
    }
}