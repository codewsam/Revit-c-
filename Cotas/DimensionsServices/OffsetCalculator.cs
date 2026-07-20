```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using G3Plugins.GeometryServices;
using G3Plugins.Models;
using G3Plugins.Utils;

namespace G3Plugins.DimensionsServices
{
    /// <summary>
    /// Calcula a posição (Line 3D) das linhas de cota, posicionadas no lado
    /// externo da fachada, utilizando offsets fixos. 
    /// A arquitetura foi refatorada para isolar responsabilidades e escalar facilmente
    /// para a adição de múltiplas linhas de cota no futuro.
    /// </summary>
    internal class OffsetCalculator
    {
        // ==============================================================================
        // CONSTANTES E CONFIGURAÇÕES
        // ==============================================================================

        // Valores base em centímetros (facilitam a leitura e manutenção)
        private const double OFFSET_DETALHE_CM = 20.0;
        private const double OFFSET_TOTAL_CM = 60.0;
        private const double MARGEM_PONTA_CM = 20.0;

        // Valores convertidos para a unidade interna do Revit (Fractional Feet)
        private static readonly double OffsetDetalheFt = GeometryMath.FeetFromCm(OFFSET_DETALHE_CM);
        private static readonly double OffsetTotalFt = GeometryMath.FeetFromCm(OFFSET_TOTAL_CM);
        private static readonly double MargemPontaFt = GeometryMath.FeetFromCm(MARGEM_PONTA_CM);

        // ==============================================================================
        // API (INTERNAL)
        // ==============================================================================

        internal DimensionLineLayout CalcularLayout(
            WallAlignment alignment,
            List<DimensionReferencePoint> pontosDetalhe,
            IList<Wall> paredesDeContexto)
        {
            ValidarEntradas(alignment, pontosDetalhe);

            // 1. Calcula os limites (start/end) das linhas ao longo do eixo da parede
            CalcularLimitesDeExtensao(pontosDetalhe, out double rMin, out double rMax);

            // 2. Determina a direção (+1 ou -1) para posicionar a cota no lado externo
            double sinalExterno = CalcularSinalLadoExterno(alignment, paredesDeContexto);

            // 3. Constrói e retorna as linhas desejadas.
            // Para adicionar novas linhas no futuro (Ex: Estrutural, Vãos), basta criar
            // a propriedade em DimensionLineLayout e chamar CriarLinhaDeCota com o novo offset.
            return new DimensionLineLayout
            {
                LinhaDetalhe = CriarLinhaDeCota(alignment, rMin, rMax, OffsetDetalheFt, sinalExterno),
                LinhaTotal = CriarLinhaDeCota(alignment, rMin, rMax, OffsetTotalFt, sinalExterno)
            };
        }

        // ==============================================================================
        // CONSTRUÇÃO DAS LINHAS DE COTA
        // ==============================================================================

        /// <summary>
        /// Constrói uma linha de cota paralela ao eixo, deslocada por um offset.
        /// </summary>
        private Line CriarLinhaDeCota(WallAlignment alignment, double rMin, double rMax, double offsetFt, double sinalExterno)
        {
            XYZ axis = alignment.AxisDirection;
            XYZ perp = alignment.PerpendicularDirection;

            double perpAlinhamento = GeometryMath.ProjectOnAxis(alignment.StartPoint, perp);
            double perpPosicaoFinal = perpAlinhamento + (sinalExterno * offsetFt);

            XYZ startPoint = MontarPonto3D(axis, perp, rMin, perpPosicaoFinal);
            XYZ endPoint = MontarPonto3D(axis, perp, rMax, perpPosicaoFinal);

            return Line.CreateBound(startPoint, endPoint);
        }

        private XYZ MontarPonto3D(XYZ axis, XYZ perp, double distanciaNoEixo, double distanciaNaPerpendicular)
        {
            return new XYZ(
                axis.X * distanciaNoEixo + perp.X * distanciaNaPerpendicular,
                axis.Y * distanciaNoEixo + perp.Y * distanciaNaPerpendicular,
                axis.Z * distanciaNoEixo + perp.Z * distanciaNaPerpendicular);
        }

        // ==============================================================================
        // CÁLCULOS DOS OFFSETS E LÓGICA GEOMÉTRICA
        // ==============================================================================

        private void CalcularLimitesDeExtensao(List<DimensionReferencePoint> pontos, out double rMin, out double rMax)
        {
            double posicaoMinima = pontos.Min(p => p.PositionOnAxis);
            double posicaoMaxima = pontos.Max(p => p.PositionOnAxis);

            rMin = posicaoMinima - MargemPontaFt;
            rMax = posicaoMaxima + MargemPontaFt;
        }

        private double CalcularSinalLadoExterno(WallAlignment alignment, IList<Wall> paredesDeContexto)
        {
            XYZ perp = alignment.PerpendicularDirection;
            double perpAlinhamento = GeometryMath.ProjectOnAxis(alignment.StartPoint, perp);

            double perpCentroModelo = CalcularCentroPerpendicular(paredesDeContexto, perp, perpAlinhamento);

            // O lado mais distante do centro do modelo define o vetor que aponta para fora da fachada
            return perpAlinhamento >= perpCentroModelo ? 1.0 : -1.0;
        }

        private double CalcularCentroPerpendicular(IList<Wall> paredes, XYZ vetorPerpendicular, double valorDeFallback)
        {
            if (paredes == null || !paredes.Any())
                return valorDeFallback;

            List<double> posicoesProjetadas = new List<double>();

            foreach (Wall parede in paredes)
            {
                Line linhaDaParede = WallAnalyzer.GetWallLine(parede);
                if (linhaDaParede == null)
                    continue;

                posicoesProjetadas.Add(GeometryMath.ProjectOnAxis(linhaDaParede.GetEndPoint(0), vetorPerpendicular));
                posicoesProjetadas.Add(GeometryMath.ProjectOnAxis(linhaDaParede.GetEndPoint(1), vetorPerpendicular));
            }

            return posicoesProjetadas.Any() ? posicoesProjetadas.Average() : valorDeFallback;
        }

        // ==============================================================================
        // VALIDAÇÕES
        // ==============================================================================

        private void ValidarEntradas(WallAlignment alignment, List<DimensionReferencePoint> pontosDetalhe)
        {
            if (alignment == null)
                throw new ArgumentNullException(nameof(alignment), "O alinhamento da parede (WallAlignment) não pode ser nulo.");

            if (pontosDetalhe == null || !pontosDetalhe.Any())
                throw new ArgumentException("A lista de pontos de detalhe (DimensionReferencePoint) é inválida ou está vazia.", nameof(pontosDetalhe));
        }
    }
}

```