using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using G3Plugins.DimensionsServices;
using G3Plugins.GeometryServices;
using G3Plugins.Models;

namespace G3Plugins.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class DimensionWallCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            if (!(view is ViewPlan))
            {
                TaskDialog.Show("Cotar Parede", "O comando só funciona em vistas de planta.");
                return Result.Cancelled;
            }

            Wall paredeSelecionada;
            try
            {
                Reference referenciaSelecionada = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new FiltroSelecaoParede(),
                    "Selecione UMA parede para cotar o alinhamento");
                paredeSelecionada = doc.GetElement(referenciaSelecionada) as Wall;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            if (paredeSelecionada == null)
            {
                TaskDialog.Show("Cotar Parede", "Nenhuma parede valida foi selecionada.");
                return Result.Cancelled;
            }

            WallAnalyzer wallAnalyzer = new WallAnalyzer();
            WallAlignment alinhamento = wallAnalyzer.BuildAlignment(doc, view, paredeSelecionada);

            if (alinhamento == null || alinhamento.Walls == null || alinhamento.Walls.Count == 0)
            {
                TaskDialog.Show("Cotar Parede",
                    "Nao foi possivel identificar um alinhamento reto valido para a parede selecionada.\n" +
                    "Verifique se ela e uma parede reta.");
                return Result.Cancelled;
            }

            ReferenceCollector referenceCollector = new ReferenceCollector();
            List<DimensionReferencePoint> cadeiaDeReferencias = referenceCollector.CollectChain(doc, alinhamento);

            if (cadeiaDeReferencias.Count < 2)
            {
                TaskDialog.Show("Cotar Parede", "Nao foi possivel montar referencias suficientes para gerar a cota.");
                return Result.Cancelled;
            }

            ChainBuilder chainBuilder = new ChainBuilder();
            DimensionChains cadeias = chainBuilder.Build(cadeiaDeReferencias);

            List<Wall> paredesDeContexto = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .ToList();

            OffsetCalculator offsetCalculator = new OffsetCalculator();
            DimensionLineLayout layout = offsetCalculator.CalcularLayout(alinhamento, cadeiaDeReferencias, paredesDeContexto);

            DimensionCreator dimensionCreator = new DimensionCreator();
            dimensionCreator.CriarCotas(doc, view, layout, cadeias);

            return Result.Succeeded;
        }
    }

    /// <summary>
    /// Restringe a selecao inicial a apenas paredes.
    /// </summary>
    internal class FiltroSelecaoParede : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Wall;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}