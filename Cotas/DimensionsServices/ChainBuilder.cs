using System.Collections.Generic;
using Autodesk.Revit.DB;
using G3Plugins.Models;

namespace G3Plugins.DimensionsServices
{
    /// <summary>
    /// Monta as ReferenceArray das linhas de cota a partir da cadeia
    /// ja ordenada pelo ReferenceCollector. Nao acessa a criacao de
    /// Dimension no Revit - isso e' responsabilidade do DimensionCreator.
    /// </summary>
    internal class ChainBuilder
    {
        internal DimensionChains Build(List<DimensionReferencePoint> pontosOrdenados)
        {
            return new DimensionChains
            {
                Detalhe = BuildDetailChain(pontosOrdenados),
                Total = BuildTotalChain(pontosOrdenados),
            };
        }

        /// <summary>
        /// Cadeia de detalhe: todas as referencias da cadeia, na ordem
        /// recebida - gera a cota segmentada (um segmento entre cada par
        /// de pontos consecutivos).
        /// </summary>
        private ReferenceArray BuildDetailChain(List<DimensionReferencePoint> pontosOrdenados)
        {
            ReferenceArray detalhe = new ReferenceArray();
            foreach (DimensionReferencePoint ponto in pontosOrdenados)
                detalhe.Append(ponto.Reference);

            return detalhe;
        }

        /// <summary>
        /// Cadeia total: somente a primeira e a ultima referencia da
        /// cadeia - gera a cota unica de ponta a ponta.
        /// </summary>
        private ReferenceArray BuildTotalChain(List<DimensionReferencePoint> pontosOrdenados)
        {
            ReferenceArray total = new ReferenceArray();
            total.Append(pontosOrdenados[0].Reference);
            total.Append(pontosOrdenados[pontosOrdenados.Count - 1].Reference);

            return total;
        }

        // Para adicionar uma nova cadeia (ex.: "Segmentos") no futuro:
        //   1. Criar um metodo privado BuildSegmentsChain(...) seguindo o
        //      mesmo padrao dos metodos acima (recebe a lista ordenada,
        //      devolve uma ReferenceArray).
        //   2. Adicionar a propriedade correspondente em DimensionChains.
        //   3. Preencher essa propriedade dentro de Build(...), chamando
        //      o novo metodo - sem tocar em BuildDetailChain/BuildTotalChain.
    }
}