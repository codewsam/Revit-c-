using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using G3Plugins.Models;

namespace G3Plugins.Utils
{
    /// <summary>
    /// Extrai faces planas de um elemento cuja normal esta alinhada com um
    /// eixo de cotagem. Usado tanto para as faces de extremidade da parede
    /// (WallAnalyzer/ReferenceCollector/IntersectionFinder) quanto para as
    /// faces de largura de portas/janelas (OpeningFinder) - evita duplicar
    /// a mesma varredura de geometria em cada service.
    /// </summary>
    internal static class FaceReferenceUtils
    {
        private const double AreaMinimaM2 = 0.02;

        internal static List<AxisFaceReference> GetAxisAlignedFaces(Element element, XYZ axis, double threshold = 0.985)
        {
            List<AxisFaceReference> resultado = new List<AxisFaceReference>();

            Options opcoes = new Options();
            opcoes.ComputeReferences = true;

            GeometryElement geometria;
            try
            {
                geometria = element.get_Geometry(opcoes);
            }
            catch
            {
                return resultado;
            }
            if (geometria == null)
                return resultado;

            double areaMinimaFt2 = AreaMinimaM2 * 10.7639;

            foreach (GeometryObject geoObj in geometria)
            {
                Solid solidDireto = geoObj as Solid;
                if (solidDireto != null && solidDireto.Volume > 0)
                {
                    ExtrairFacesDoSolido(solidDireto, axis, threshold, areaMinimaFt2, resultado);
                    continue;
                }

                // Familias hospedadas (portas/janelas) normalmente vem
                // dentro de um GeometryInstance - precisa "abrir" pra
                // pegar os solidos reais.
                GeometryInstance instancia = geoObj as GeometryInstance;
                if (instancia != null)
                {
                    GeometryElement geometriaInstancia = instancia.GetInstanceGeometry();
                    if (geometriaInstancia == null)
                        continue;

                    foreach (GeometryObject geoObjInterno in geometriaInstancia)
                    {
                        Solid solidInterno = geoObjInterno as Solid;
                        if (solidInterno != null && solidInterno.Volume > 0)
                            ExtrairFacesDoSolido(solidInterno, axis, threshold, areaMinimaFt2, resultado);
                    }
                }
            }

            return resultado;
        }

        private static void ExtrairFacesDoSolido(Solid solid, XYZ axis, double threshold, double areaMinimaFt2, List<AxisFaceReference> resultado)
        {
            foreach (Face face in solid.Faces)
            {
                PlanarFace planarFace = face as PlanarFace;
                if (planarFace == null || planarFace.Reference == null)
                    continue;
                if (planarFace.Area < areaMinimaFt2)
                    continue;

                double alinhamento = Math.Abs(planarFace.FaceNormal.DotProduct(axis));
                if (alinhamento <= threshold)
                    continue;

                double posicao = planarFace.Origin.DotProduct(axis);
                resultado.Add(new AxisFaceReference(planarFace.Reference, posicao));
            }
        }
    }
}