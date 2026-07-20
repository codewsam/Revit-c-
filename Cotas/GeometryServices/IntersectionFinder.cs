using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using G3Plugins.Models;
using G3Plugins.Utils;

namespace G3Plugins.GeometryServices
{
    /// <summary>
    /// Localiza os pontos de encontro (juntas) entre paredes CONSECUTIVAS
    /// do mesmo alinhamento e resolve uma referencia de face valida em
    /// cada ponto.
    ///
    /// Hoje so o tipo de juncao "ponta a ponta" (paredes colineares que
    /// se tocam pelas extremidades) e' detectado e resolvido - e' o unico
    /// caso que o alinhamento atual (WallAlignment de paredes colineares
    /// consecutivas) pode produzir. A arquitetura ja separa "que tipo de
    /// juncao e' essa" (DetectJointKind) de "onde fica o ponto" (
    /// LocateJointPoint) para permitir, no futuro, plugar deteccao e
    /// resolucao de juncoes em L, T, X e sobreposicao parcial sem tocar
    /// em FindJoints nem no restante do pipeline.
    /// </summary>
    internal class IntersectionFinder
    {
        /// <summary>
        /// Tipos de juncao entre duas paredes. So EndToEnd esta
        /// implementado; os demais sao reservados para quando o
        /// alinhamento passar a detectar paredes em L/T/X ou com
        /// sobreposicao parcial.
        /// </summary>
        private enum WallJointKind
        {
            EndToEnd,

            // Reservados para implementacao futura - nenhum deles e'
            // produzido hoje por WallAlignment, entao DetectJointKind
            // nunca os retorna ainda:
            LShape,
            TShape,
            XShape,
            PartialOverlap,
        }

        internal List<DimensionReferencePoint> FindJoints(WallAlignment alignment)
        {
            List<DimensionReferencePoint> juntas = new List<DimensionReferencePoint>();
            if (alignment == null || alignment.Walls == null || alignment.Walls.Count < 2)
                return juntas;

            for (int i = 0; i < alignment.Walls.Count - 1; i++)
            {
                Wall paredeAtual = alignment.Walls[i];
                Wall proximaParede = alignment.Walls[i + 1];

                DimensionReferencePoint junta = TryBuildJoint(paredeAtual, proximaParede, alignment.AxisDirection);
                if (junta != null)
                    juntas.Add(junta);
            }

            return juntas;
        }

        /// <summary>
        /// Tenta montar o ponto de referencia de cotagem para a juncao
        /// entre duas paredes: localiza o ponto de encontro, resolve a
        /// melhor referencia de face naquele ponto e monta o
        /// DimensionReferencePoint. Retorna null se qualquer etapa falhar
        /// (linha invalida, paredes que nao se encontram, ou nenhuma face
        /// referenciavel proxima o suficiente).
        /// </summary>
        private DimensionReferencePoint TryBuildJoint(Wall paredeAtual, Wall proximaParede, XYZ axis)
        {
            Line linhaAtual = WallAnalyzer.GetWallLine(paredeAtual);
            Line linhaProxima = WallAnalyzer.GetWallLine(proximaParede);
            if (linhaAtual == null || linhaProxima == null)
                return null;

            WallJointKind tipoJunta = DetectJointKind(linhaAtual, linhaProxima);

            XYZ pontoJunta = LocateJointPoint(tipoJunta, linhaAtual, linhaProxima);
            if (pontoJunta == null)
                return null;

            double posicaoJunta = GeometryMath.ProjectOnAxis(pontoJunta, axis);

            Reference referencia = ResolveBestReference(paredeAtual, proximaParede, axis, posicaoJunta);
            if (referencia == null)
                return null;

            return CreateReferencePoint(referencia, posicaoJunta, paredeAtual, proximaParede);
        }

        /// <summary>
        /// Classifica o tipo de juncao entre as duas linhas de eixo.
        /// Hoje sempre retorna EndToEnd: e' o unico tipo que o
        /// alinhamento atual (paredes colineares consecutivas) pode
        /// gerar. Ponto de extensao para o dia em que WallAlignment (ou
        /// outro chamador) passar a agrupar paredes em L/T/X/sobreposicao
        /// parcial - a deteccao real de cada tipo entra aqui, sem mexer
        /// em FindJoints.
        /// </summary>
        private WallJointKind DetectJointKind(Line linhaA, Line linhaB)
        {
            return WallJointKind.EndToEnd;
        }

        /// <summary>
        /// Localiza o ponto fisico de encontro entre as duas linhas de
        /// eixo, de acordo com o tipo de juncao. Despacha para o metodo
        /// especifico de cada tipo; os tipos ainda nao implementados
        /// retornam null (nunca sao chamados hoje, ja que
        /// DetectJointKind so retorna EndToEnd).
        /// </summary>
        private XYZ LocateJointPoint(WallJointKind tipoJunta, Line linhaA, Line linhaB)
        {
            switch (tipoJunta)
            {
                case WallJointKind.EndToEnd:
                    return LocateEndToEndJoint(linhaA, linhaB);

                case WallJointKind.LShape:
                case WallJointKind.TShape:
                case WallJointKind.XShape:
                case WallJointKind.PartialOverlap:
                    // Ainda nao implementado - reservado para quando o
                    // alinhamento passar a produzir esses casos.
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Juncao ponta a ponta: acha, entre as extremidades das duas
        /// linhas, o par mais proximo - esse e' o ponto de encontro.
        /// </summary>
        private XYZ LocateEndToEndJoint(Line linhaA, Line linhaB)
        {
            XYZ[] pontosA = { linhaA.GetEndPoint(0), linhaA.GetEndPoint(1) };
            XYZ[] pontosB = { linhaB.GetEndPoint(0), linhaB.GetEndPoint(1) };

            double menorDistancia = double.MaxValue;
            XYZ melhorPonto = null;

            foreach (XYZ pa in pontosA)
            {
                foreach (XYZ pb in pontosB)
                {
                    double distancia = pa.DistanceTo(pb);
                    if (distancia < menorDistancia)
                    {
                        menorDistancia = distancia;
                        melhorPonto = pa;
                    }
                }
            }

            return melhorPonto;
        }

        /// <summary>
        /// Resolve, entre as faces referenciaveis das duas paredes
        /// alinhadas ao eixo de cota, a face cuja posicao no eixo mais se
        /// aproxima do ponto de juncao encontrado.
        /// </summary>
        private Reference ResolveBestReference(Wall paredeA, Wall paredeB, XYZ axis, double posicaoAlvo)
        {
            List<AxisFaceReference> faces = FaceReferenceUtils.GetAxisAlignedFaces(paredeA, axis);
            faces.AddRange(FaceReferenceUtils.GetAxisAlignedFaces(paredeB, axis));
            if (faces.Count == 0)
                return null;

            AxisFaceReference maisProxima = faces
                .OrderBy(f => System.Math.Abs(f.PositionOnAxis - posicaoAlvo))
                .First();

            return maisProxima.Reference;
        }

        /// <summary>
        /// Monta o DimensionReferencePoint final da juncao, com a
        /// descricao padrao usada para depuracao/relatorio.
        /// </summary>
        private DimensionReferencePoint CreateReferencePoint(Reference referencia, double posicaoJunta, Wall paredeAtual, Wall proximaParede)
        {
            string descricao = string.Format(
                "Intersecao entre parede {0} e parede {1}",
                paredeAtual.Id.Value, proximaParede.Id.Value);

            return new DimensionReferencePoint(
                referencia, posicaoJunta, ReferencePointType.Intersecao, paredeAtual.Id, descricao);
        }
    }
}