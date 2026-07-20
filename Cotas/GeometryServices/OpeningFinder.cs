using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using G3Plugins.Models;
using G3Plugins.Utils;

namespace G3Plugins.GeometryServices
{
    /// <summary>
    /// Localiza aberturas hospedadas nas paredes de um alinhamento
    /// (hoje: portas e janelas) e resolve as referencias de inicio/fim
    /// (largura) de cada uma.
    ///
    /// A lista de categorias suportadas (CategoryMappings) e' o unico
    /// ponto que precisa crescer para suportar novas aberturas (shafts,
    /// nichos etc.) - o fluxo de coleta/resolucao/validacao e' o mesmo
    /// para qualquer categoria e nao precisa ser tocado.
    /// </summary>
    internal class OpeningFinder
    {
        /// <summary>
        /// Associa uma categoria do Revit ao OpeningCategory interno
        /// correspondente e ao nome usado nas descricoes dos pontos de
        /// cota. Para suportar uma nova categoria de abertura (shaft,
        /// nicho, etc.), basta adicionar uma entrada aqui - nenhum outro
        /// metodo desta classe precisa mudar.
        /// </summary>
        private static readonly OpeningCategoryMapping[] CategoryMappings =
        {
            new OpeningCategoryMapping(BuiltInCategory.OST_Doors, OpeningCategory.Porta, "Porta"),
            new OpeningCategoryMapping(BuiltInCategory.OST_Windows, OpeningCategory.Janela, "Janela"),

            // Reservado para futuras categorias, por exemplo:
            // new OpeningCategoryMapping(BuiltInCategory.OST_ShaftOpening, OpeningCategory.Shaft, "Shaft"),
        };

        private class OpeningCategoryMapping
        {
            internal BuiltInCategory RevitCategory { get; }
            internal OpeningCategory Category { get; }
            internal string DisplayName { get; }

            internal OpeningCategoryMapping(BuiltInCategory revitCategory, OpeningCategory category, string displayName)
            {
                RevitCategory = revitCategory;
                Category = category;
                DisplayName = displayName;
            }
        }

        internal List<OpeningInfo> FindOpenings(Document doc, WallAlignment alignment)
        {
            List<OpeningInfo> aberturas = new List<OpeningInfo>();
            if (alignment == null || alignment.Walls == null)
                return aberturas;

            foreach (Wall parede in alignment.Walls)
                aberturas.AddRange(FindOpeningsInWall(doc, parede, alignment.AxisDirection));

            return aberturas;
        }

        /// <summary>
        /// Coleta todas as aberturas suportadas (CategoryMappings)
        /// hospedadas em uma unica parede.
        /// </summary>
        private List<OpeningInfo> FindOpeningsInWall(Document doc, Wall parede, XYZ axis)
        {
            List<OpeningInfo> aberturas = new List<OpeningInfo>();

            foreach (OpeningCategoryMapping mapping in CategoryMappings)
            {
                foreach (ElementId idHospedado in CollectHostedElementIds(parede, mapping.RevitCategory))
                {
                    OpeningInfo abertura = TryBuildOpening(doc, idHospedado, mapping, parede.Id, axis);
                    if (abertura != null)
                        aberturas.Add(abertura);
                }
            }

            return aberturas;
        }

        /// <summary>
        /// Coleta os ids dos elementos hospedados na parede que pertencem
        /// a' categoria informada.
        /// </summary>
        private IEnumerable<ElementId> CollectHostedElementIds(Wall parede, BuiltInCategory categoria)
        {
            ElementCategoryFilter filtro = new ElementCategoryFilter(categoria);
            return parede.GetDependentElements(filtro);
        }

        /// <summary>
        /// Tenta montar a OpeningInfo de uma abertura hospedada: resolve
        /// o elemento, obtem as referencias de inicio/fim e monta os
        /// pontos de cota. Retorna null se qualquer validacao falhar
        /// (elemento invalido, faces insuficientes, ou largura nula).
        /// </summary>
        private OpeningInfo TryBuildOpening(Document doc, ElementId id, OpeningCategoryMapping mapping, ElementId hostWallId, XYZ axis)
        {
            FamilyInstance instancia = doc.GetElement(id) as FamilyInstance;
            if (instancia == null)
                return null;

            AxisFaceReference inicio, fim;
            if (!TryResolveEdgeReferences(instancia, axis, out inicio, out fim))
                return null;

            return CreateOpeningInfo(instancia, mapping, hostWallId, inicio, fim);
        }

        /// <summary>
        /// Resolve as duas referencias de borda (inicio/fim, ao longo do
        /// eixo de cota) de um elemento hospedado. Retorna false se nao
        /// houver ao menos 2 faces alinhadas ao eixo, ou se as duas
        /// referencias resolvidas coincidirem na mesma posicao (largura
        /// nula/degenerada).
        /// </summary>
        private bool TryResolveEdgeReferences(FamilyInstance instancia, XYZ axis, out AxisFaceReference inicio, out AxisFaceReference fim)
        {
            inicio = null;
            fim = null;

            List<AxisFaceReference> faces = FaceReferenceUtils.GetAxisAlignedFaces(instancia, axis);
            if (faces.Count < 2)
                return false;

            List<AxisFaceReference> ordenadas = faces.OrderBy(f => f.PositionOnAxis).ToList();
            inicio = ordenadas.First();
            fim = ordenadas.Last();

            return !GeometryMath.AreClose(inicio.PositionOnAxis, fim.PositionOnAxis);
        }

        /// <summary>
        /// Monta a OpeningInfo final, com os DimensionReferencePoint de
        /// inicio e fim ja com a descricao padrao usada para
        /// depuracao/relatorio.
        /// </summary>
        private OpeningInfo CreateOpeningInfo(FamilyInstance instancia, OpeningCategoryMapping mapping, ElementId hostWallId, AxisFaceReference inicio, AxisFaceReference fim)
        {
            ElementId id = instancia.Id;

            OpeningInfo abertura = new OpeningInfo(instancia, mapping.Category, hostWallId);
            abertura.StartPoint = new DimensionReferencePoint(
                inicio.Reference, inicio.PositionOnAxis, ReferencePointType.AberturaInicio, id,
                string.Format("{0} {1} (inicio)", mapping.DisplayName, id.Value));
            abertura.EndPoint = new DimensionReferencePoint(
                fim.Reference, fim.PositionOnAxis, ReferencePointType.AberturaFim, id,
                string.Format("{0} {1} (fim)", mapping.DisplayName, id.Value));

            return abertura;
        }
    }
}