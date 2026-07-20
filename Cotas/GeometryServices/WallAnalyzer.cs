using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using G3Plugins.Models;
using G3Plugins.Utils;

namespace G3Plugins.GeometryServices
{
    /// <summary>
    /// Encontra o alinhamento completo (paredes retas, colineares e
    /// conectadas ponta-a-ponta) ao qual uma parede selecionada pertence -
    /// expande a cadeia nos dois sentidos a partir da parede semente.
    /// </summary>
    internal class WallAnalyzer
    {
        private readonly double _toleranceDistance;
        private readonly double _toleranceDot;

        internal WallAnalyzer(double toleranceDistanceFeet = 0.05, double toleranceDot = 0.999)
        {
            _toleranceDistance = toleranceDistanceFeet;
            _toleranceDot = toleranceDot;
        }

        internal WallAlignment BuildAlignment(Document doc, View view, Wall seedWall)
        {
            Line seedLine = GetWallLine(seedWall);
            if (seedLine == null)
                return null;

            XYZ axis = (seedLine.GetEndPoint(1) - seedLine.GetEndPoint(0)).Normalize();

            List<Wall> candidatas = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .Where(w => w.Id != seedWall.Id && GetWallLine(w) != null)
                .ToList();

            HashSet<ElementId> idsNaCadeia = new HashSet<ElementId> { seedWall.Id };
            List<Wall> cadeia = new List<Wall> { seedWall };

            XYZ pontaInicial = seedLine.GetEndPoint(0);
            XYZ pontaFinal = seedLine.GetEndPoint(1);

            pontaFinal = ExpandirCadeia(pontaFinal, axis, candidatas, idsNaCadeia, cadeia);
            pontaInicial = ExpandirCadeia(pontaInicial, axis, candidatas, idsNaCadeia, cadeia);

            List<Wall> ordenadas = OrdenarPeloEixo(cadeia, axis);

            return new WallAlignment
            {
                Walls = ordenadas,
                AxisDirection = axis,
                PerpendicularDirection = GeometryMath.GetPerpendicular(axis),
                StartPoint = pontaInicial,
                EndPoint = pontaFinal
            };
        }

        /// <summary>
        /// A partir de uma ponta da cadeia, procura repetidamente (ate nao
        /// achar mais nada) uma parede candidata colinear cujo endpoint
        /// encoste nessa ponta - cobre prolongamentos retos e tambem o
        /// caso de varias paredes curtas em fileira formando a fachada.
        /// </summary>
        private XYZ ExpandirCadeia(XYZ pontaAtual, XYZ axis, List<Wall> candidatas, HashSet<ElementId> idsNaCadeia, List<Wall> cadeia)
        {
            bool encontrou = true;
            while (encontrou)
            {
                encontrou = false;
                foreach (Wall candidata in candidatas)
                {
                    if (idsNaCadeia.Contains(candidata.Id))
                        continue;

                    Line linha = GetWallLine(candidata);
                    if (linha == null)
                        continue;

                    XYZ direcao = (linha.GetEndPoint(1) - linha.GetEndPoint(0)).Normalize();
                    if (!GeometryMath.AreDirectionsCollinear(direcao, axis, _toleranceDot))
                        continue;

                    XYZ p0 = linha.GetEndPoint(0);
                    XYZ p1 = linha.GetEndPoint(1);

                    if (GeometryMath.ArePointsClose(p0, pontaAtual, _toleranceDistance))
                    {
                        pontaAtual = p1;
                        idsNaCadeia.Add(candidata.Id);
                        cadeia.Add(candidata);
                        encontrou = true;
                        break;
                    }
                    if (GeometryMath.ArePointsClose(p1, pontaAtual, _toleranceDistance))
                    {
                        pontaAtual = p0;
                        idsNaCadeia.Add(candidata.Id);
                        cadeia.Add(candidata);
                        encontrou = true;
                        break;
                    }
                }
            }
            return pontaAtual;
        }

        private List<Wall> OrdenarPeloEixo(List<Wall> paredes, XYZ axis)
        {
            return paredes
                .OrderBy(w => GeometryMath.ProjectOnAxis(GetWallLine(w).GetEndPoint(0), axis))
                .ToList();
        }

        /// <summary>
        /// Reaproveitada por IntersectionFinder e OffsetCalculator - evita
        /// duplicar a leitura de LocationCurve em cada service.
        /// </summary>
        internal static Line GetWallLine(Wall wall)
        {
            LocationCurve loc = wall.Location as LocationCurve;
            Line line = loc != null ? loc.Curve as Line : null;
            return line;
        }
    }
}