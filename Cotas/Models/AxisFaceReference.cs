using Autodesk.Revit.DB;

namespace G3Plugins.Models
{
    /// <summary>
    /// Uma face plana referenciavel encontrada por FaceReferenceUtils, ja
    /// com a posicao projetada no eixo de cotagem.
    /// </summary>
    internal class AxisFaceReference
    {
        public Reference Reference { get; set; }
        public double PositionOnAxis { get; set; }

        public AxisFaceReference(Reference reference, double positionOnAxis)
        {
            Reference = reference;
            PositionOnAxis = positionOnAxis;
        }
    }
}