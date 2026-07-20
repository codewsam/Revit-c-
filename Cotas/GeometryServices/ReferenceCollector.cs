```csharp
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using G3Plugins.Models;
using G3Plugins.Utils;

namespace G3Plugins.GeometryServices
{
    /// <summary>
    /// Orquestra a coleta de referências (pontos) ao longo de um alinhamento.
    /// A arquitetura foi refatorada para isolar as responsabilidades de coleta 
    /// (extremidades, encontros, portas, janelas), validação, ordenação e 
    /// deduplicação. Isso prepara a classe para suportar novos coletores no futuro.
    /// </summary>
    internal class ReferenceCollector
    {
        private readonly OpeningFinder _openingFinder;
        private readonly IntersectionFinder _intersectionFinder;

        // ==============================================================================
        // CONSTRUTOR
        // ==============================================================================

        internal ReferenceCollector()
        {
            _openingFinder = new OpeningFinder();
            _intersectionFinder = new IntersectionFinder();
        }

        // ==============================================================================
        // API (INTERNAL)
        // ==============================================================================

        internal List<DimensionReferencePoint> CollectChain(Document doc, WallAlignment alignment)
        {
            // 1. Validação de entradas
            if (!AlinhamentoValido(alignment))
                return new List<DimensionReferencePoint>();

            List<DimensionReferencePoint> pontosBrutos = new List<DimensionReferencePoint>();

            // 2. Coleta das referências obrigatórias (Extremidades)
            if (!TentarColetarExtremidades(alignment, out DimensionReferencePoint inicio, out DimensionReferencePoint fim))
                return pontosBrutos; // Sem extremidades válidas, não há o que cotar

            pontosBrutos.Add(inicio);
            pontosBrutos.Add(fim);

            // 3. Coleta de elementos arquitetônicos e estruturais
            pontosBrutos.AddRange(ColetarIntersecoesDeParedes(alignment));
            pontosBrutos.AddRange(ColetarPortasEJanelas(doc, alignment));

            // ==========================================================================
            // ESPAÇO PARA FUTUROS COLETORES (Extensibilidade arquitetural)
            // ==========================================================================
            // pontosBrutos.AddRange(ColetarPilares(doc, alignment));
            // pontosBrutos.AddRange(ColetarShafts(doc, alignment));
            // pontosBrutos.AddRange(ColetarEixosEstruturais(doc, alignment));
            // pontosBrutos.AddRange(ColetarFacesDeLaje(doc, alignment));
            // ==========================================================================

            // 4. Processamento da cadeia final (Ordenação e Limpeza)
            List<DimensionReferencePoint> pontosOrdenados = OrdenarPontos(pontosBrutos);

            return RemoverDuplicidades(pontosOrdenados);
        }

        // ==============================================================================
        // VALIDAÇÕES
        // ==============================================================================

        private bool AlinhamentoValido(WallAlignment alignment)
        {
            return alignment != null && alignment.Walls != null && alignment.Walls.Any();
        }

        // ==============================================================================
        // COLETORES DE REFERÊNCIAS
        // ==============================================================================

        private bool TentarColetarExtremidades(
            WallAlignment alignment,
            out DimensionReferencePoint extremidadeInicial,
            out DimensionReferencePoint extremidadeFinal)
        {
            Wall primeiraParede = alignment.Walls.First();
            Wall ultimaParede = alignment.Walls.Last();

            extremidadeInicial = ResolverFaceDaExtremidade(
                primeiraParede, alignment.AxisDirection, alignment.StartPoint, "inicial");

            extremidadeFinal = ResolverFaceDaExtremidade(
                ultimaParede, alignment.AxisDirection, alignment.EndPoint, "final");

            return extremidadeInicial != null && extremidadeFinal != null;
        }

        private List<DimensionReferencePoint> ColetarIntersecoesDeParedes(WallAlignment alignment)
        {
            return _intersectionFinder.FindJoints(alignment) ?? new List<DimensionReferencePoint>();
        }

        /// <summary>
        /// Coleta referências de portas e janelas. O OpeningFinder agrupa a lógica de 
        /// leitura dos vãos no Revit, mas aqui extraímos as pontas de maneira padronizada.
        /// </summary>
        private List<DimensionReferencePoint> ColetarPortasEJanelas(Document doc, WallAlignment alignment)
        {
            List<DimensionReferencePoint> pontosDeAbertura = new List<DimensionReferencePoint>();
            List<OpeningInfo> aberturas = _openingFinder.FindOpenings(doc, alignment);

            if (aberturas == null)
                return pontosDeAbertura;

            foreach (OpeningInfo abertura in aberturas)
            {
                pontosDeAbertura.Add(abertura.StartPoint);
                pontosDeAbertura.Add(abertura.EndPoint);
            }

            return pontosDeAbertura;
        }

        private DimensionReferencePoint ResolverFaceDaExtremidade(Wall parede, XYZ eixoDirecao, XYZ pontoEsperado, string rotulo)
        {
            List<AxisFaceReference> facesAlinhadas = FaceReferenceUtils.GetAxisAlignedFaces(parede, eixoDirecao);
            if (facesAlinhadas == null || !facesAlinhadas.Any())
                return null;

            double posicaoEsperada = GeometryMath.ProjectOnAxis(pontoEsperado, eixoDirecao);

            AxisFaceReference faceMaisProxima = facesAlinhadas
                .OrderBy(f => System.Math.Abs(f.PositionOnAxis - posicaoEsperada))
                .First();

            string descricao = string.Format("Extremidade {0} do alinhamento (parede {1})", rotulo, parede.Id.Value);

            return new DimensionReferencePoint(
                faceMaisProxima.Reference,
                faceMaisProxima.PositionOnAxis,
                ReferencePointType.Extremidade,
                parede.Id,
                descricao);
        }

        // ==============================================================================
        // ORDENAÇÃO E LIMPEZA
        // ==============================================================================

        private List<DimensionReferencePoint> OrdenarPontos(List<DimensionReferencePoint> pontos)
        {
            return pontos
                .OrderBy(p => p.PositionOnAxis)
                .ThenBy(p => ObterPrioridadeDoPonto(p.Type))
                .ToList();
        }

        /// <summary>
        /// Mantém apenas um ponto quando vários coincidem espacialmente (dentro da tolerância),
        /// priorizando a referência de maior relevância (Ex: parede ganha de janela).
        /// </summary>
        private List<DimensionReferencePoint> RemoverDuplicidades(List<DimensionReferencePoint> pontosOrdenados)
        {
            List<DimensionReferencePoint> resultado = new List<DimensionReferencePoint>();

            foreach (DimensionReferencePoint pontoAtual in pontosOrdenados)
            {
                if (resultado.Count == 0)
                {
                    resultado.Add(pontoAtual);
                    continue;
                }

                int indiceUltimo = resultado.Count - 1;
                DimensionReferencePoint ultimoAdicionado = resultado[indiceUltimo];

                // Se o ponto atual cai no mesmo lugar do último inserido
                if (GeometryMath.AreClose(ultimoAdicionado.PositionOnAxis, pontoAtual.PositionOnAxis))
                {
                    // Substitui se o ponto atual tiver maior prioridade (número menor)
                    if (ObterPrioridadeDoPonto(pontoAtual.Type) < ObterPrioridadeDoPonto(ultimoAdicionado.Type))
                    {
                        resultado[indiceUltimo] = pontoAtual;
                    }
                    // Caso contrário, apenas descarta a duplicata
                }
                else
                {
                    resultado.Add(pontoAtual);
                }
            }

            return resultado;
        }

        private int ObterPrioridadeDoPonto(ReferencePointType tipo)
        {
            // Quanto menor o número, maior a prioridade para desempate
            switch (tipo)
            {
                case ReferencePointType.Extremidade:
                    return 0;
                case ReferencePointType.Intersecao:
                    return 1;
                case ReferencePointType.AberturaInicio:
                case ReferencePointType.AberturaFim:
                    return 2;
                default:
                    return 3;
            }
        }
    }
}

```