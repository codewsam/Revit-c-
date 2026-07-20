using Autodesk.Revit.DB;

namespace G3Plugins.Utils
{
    /// <summary>
    /// Funcoes matematicas puras reaproveitadas por GeometryServices e
    /// DimensionsServices - projecao em eixo, tolerancias, colinearidade e
    /// conversao de unidades. Nao acessa o documento nem cria elementos.
    /// </summary>
    internal static class GeometryMath
    {
        internal const double DefaultTolerance = 0.01; // pes (~3 mm)

        internal static double ProjectOnAxis(XYZ point, XYZ axisDirection)
        {
            return point.DotProduct(axisDirection);
        }

        internal static bool AreClose(double a, double b, double tolerance = DefaultTolerance)
        {
            return System.Math.Abs(a - b) <= tolerance;
        }

        internal static bool ArePointsClose(XYZ a, XYZ b, double tolerance = DefaultTolerance)
        {
            return a.DistanceTo(b) <= tolerance;
        }

        internal static bool AreDirectionsCollinear(XYZ d1, XYZ d2, double toleranceDot = 0.999)
        {
            XYZ n1 = d1.Normalize();
            XYZ n2 = d2.Normalize();
            double dot = System.Math.Abs(n1.DotProduct(n2));
            return dot >= toleranceDot;
        }

        internal static XYZ GetPerpendicular(XYZ axisDirection)
        {
            return new XYZ(-axisDirection.Y, axisDirection.X, 0.0);
        }

        internal static double FeetFromCm(double cm)
        {
            return UnitUtils.ConvertToInternalUnits(cm, UnitTypeId.Centimeters);
        }
    }
}