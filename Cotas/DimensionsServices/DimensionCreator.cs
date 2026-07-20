```csharp
using System;
using Autodesk.Revit.DB;
using G3Plugins.Models;

namespace G3Plugins.DimensionsServices
{
    /// <summary>
    /// Responsável exclusivamente pela criação e gravação dos elementos de cota (Dimension) no Revit.
    /// Atua estritamente como uma camada de execução da API do Revit, sem conter regras de negócio, 
    /// cálculo de posições ou seleção de referências.
    /// </summary>
    internal class DimensionCreator
    {
        private const string NOME_TRANSACAO = "Cotar Parede - Alinhamento";

        // ==============================================================================
        // API (INTERNAL)
        // ==============================================================================

        /// <summary>
        /// Executa a criação das cotas no documento do Revit dentro de uma transação isolada.
        /// </summary>
        internal void CriarCotas(Document doc, View view, DimensionLineLayout layout, DimensionChains cadeias)
        {
            ValidarEntradas(doc, view, layout, cadeias);

            using (Transaction transacao = new Transaction(doc, NOME_TRANSACAO))
            {
                try
                {
                    transacao.Start();

                    ExecutarCriacaoDasCotas(doc, view, layout, cadeias);

                    transacao.Commit();
                }
                catch (Exception)
                {
                    if (transacao.GetStatus() == TransactionStatus.Started)
                    {
                        transacao.RollBack();
                    }
                    throw;
                }
            }
        }

        // ==============================================================================
        // EXECUÇÃO DA CRIAÇÃO DAS COTAS
        // ==============================================================================

        private void ExecutarCriacaoDasCotas(Document doc, View view, DimensionLineLayout layout, DimensionChains cadeias)
        {
            // Cota de Detalhe
            CriarCotaSeValida(doc, view, layout.LinhaDetalhe, cadeias.Detalhe);

            // Cota Total
            CriarCotaSeValida(doc, view, layout.LinhaTotal, cadeias.Total);

            // ==========================================================================
            // ESPAÇO PARA NOVAS LINHAS DE COTA (Extensibilidade arquitetural)
            // ==========================================================================
            // CriarCotaSeValida(doc, view, layout.LinhaEstrutural, cadeias.Estrutural);
            // CriarCotaSeValida(doc, view, layout.LinhaSegmentos, cadeias.Segmentos);
            // ==========================================================================
        }

        /// <summary>
        /// Valida se a linha e a ReferenceArray atendem aos requisitos antes de instanciar a cota.
        /// </summary>
        private void CriarCotaSeValida(
            Document doc,
            View view,
            Line linha,
            ReferenceArray referencias,
            DimensionType tipoDeCota = null)
        {
            if (!ValidarLinhaEReferencias(linha, referencias))
                return;

            Dimension dimensaoCriada = CriarDimensaoNoDocumento(doc, view, linha, referencias);

            if (dimensaoCriada != null && tipoDeCota != null)
            {
                AplicarTipoDeCota(dimensaoCriada, tipoDeCota);
            }
        }

        /// <summary>
        /// Invoca diretamente o método da API do Revit que cria o elemento de cota.
        /// </summary>
        private Dimension CriarDimensaoNoDocumento(Document doc, View view, Line linha, ReferenceArray referencias)
        {
            return doc.Create.NewDimension(view, linha, referencias);
        }

        // ==============================================================================
        // APLICAÇÃO DE TIPOS / ESTILOS DE COTA
        // ==============================================================================

        /// <summary>
        /// Aplica um tipo específico de cota (DimensionType) ao elemento recém-criado.
        /// </summary>
        private void AplicarTipoDeCota(Dimension dimensao, DimensionType tipoDeCota)
        {
            if (dimensao != null && tipoDeCota != null)
            {
                dimensao.ChangeTypeId(tipoDeCota.Id);
            }
        }

        // ==============================================================================
        // VALIDAÇÕES
        // ==============================================================================

        private void ValidarEntradas(Document doc, View view, DimensionLineLayout layout, DimensionChains cadeias)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc), "O documento do Revit não pode ser nulo.");

            if (view == null)
                throw new ArgumentNullException(nameof(view), "A vista ativa não pode ser nula.");

            if (layout == null)
                throw new ArgumentNullException(nameof(layout), "O layout das linhas de cota (DimensionLineLayout) não pode ser nulo.");

            if (cadeias == null)
                throw new ArgumentNullException(nameof(cadeias), "As cadeias de referência (DimensionChains) não podem ser nulas.");
        }

        /// <summary>
        /// Valida se a geometria da linha e o conjunto de referências atendem aos requisitos mínimos da API do Revit.
        /// </summary>
        private bool ValidarLinhaEReferencias(Line linha, ReferenceArray referencias)
        {
            if (linha == null)
                return false;

            // A API do Revit exige pelo menos 2 referências válidas para criar uma cota
            if (referencias == null || referencias.Size < 2)
                return false;

            return true;
        }
    }
}

```