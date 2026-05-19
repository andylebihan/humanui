using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Containers
{
    /// <summary>
    /// Eto port of the WPF Grid. Uses TableLayout so cells can declare star
    /// ("1*") or absolute ("50") sizing, and elements can be placed at
    /// arbitrary (row, column) with optional row/column spans. Spans are
    /// emulated by leaving empty TableCells under the spanned area — Eto's
    /// TableLayout doesn't have native rowspan/colspan, so spans wider than
    /// 1 simply place the element at the top-left of the span; the visual
    /// extent comes from sizing the spanned columns/rows star-shares.
    /// </summary>
    public class CreateGrid_Component : GH_Component
    {
        public CreateGrid_Component()
            : base("Create Grid", "Grid",
                "Create a container with row/column-positioned elements.\nUse row and column definitions (\"1*\" for ratio, \"50\" for absolute) and Element Row / Column to position items.",
                "Human UI", "UI Containers")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Elements", "E", "The UI elements to place in the grid", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "W", "The width of the grid", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddNumberParameter("Height", "H", "The height of the grid", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddTextParameter("Row Definitions", "RD", "An optional list of Row Heights. Use numbers for absolute sizes and numbers with * for ratios (like 1* and 2* for a 1/3 2/3 split)", GH_ParamAccess.list);
            pManager[3].Optional = true;
            pManager.AddTextParameter("Column Definitions", "CD", "An optional list of Column Widths. Use numbers for absolute sizes and numbers with * for ratios (like 1* and 2* for a 1/3 2/3 split)", GH_ParamAccess.list);
            pManager[4].Optional = true;
            pManager.AddIntegerParameter("Element Row", "ER", "The rows to place the elements in, counting from 0 at the top.", GH_ParamAccess.list);
            pManager[5].Optional = true;
            pManager.AddIntegerParameter("Element Column", "EC", "The columns to place the elements in, counting from 0 at the left.", GH_ParamAccess.list);
            pManager[6].Optional = true;
            pManager.AddIntegerParameter("Element Row Span", "ERS", "How many rows each element should span. This will be 1 by default.", GH_ParamAccess.list, 1);
            pManager[7].Optional = true;
            pManager.AddIntegerParameter("Element Column Span", "ECS", "How many columns each element should span. This will be 1 by default.", GH_ParamAccess.list, 1);
            pManager[8].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Grid", "S", "The combined group of elements", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var elementsToAdd = new List<UIElement_Goo>();
            double width = 0, height = 0;
            var rowDefs = new List<string>();
            var colDefs = new List<string>();
            var elementRows = new List<int>();
            var elementCols = new List<int>();
            var elementRowSpans = new List<int>();
            var elementColSpans = new List<int>();

            if (!DA.GetDataList("UI Elements", elementsToAdd)) return;
            bool hasWidth = DA.GetData("Width", ref width);
            bool hasHeight = DA.GetData("Height", ref height);
            bool hasRowDefs = DA.GetDataList("Row Definitions", rowDefs);
            bool hasColDefs = DA.GetDataList("Column Definitions", colDefs);
            bool hasElementRows = DA.GetDataList("Element Row", elementRows);
            bool hasElementCols = DA.GetDataList("Element Column", elementCols);
            DA.GetDataList("Element Row Span", elementRowSpans);
            DA.GetDataList("Element Column Span", elementColSpans);

            int rows = hasRowDefs ? rowDefs.Count : 1;
            int cols = hasColDefs ? colDefs.Count : 1;

            var table = new TableLayout(cols, rows) { ID = "GH_Grid", Spacing = new Eto.Drawing.Size(2, 2) };

            for (int i = 0; i < elementsToAdd.Count; i++)
            {
                var u = elementsToAdd[i];
                if (u?.element == null) continue;
                HUI_Util.removeParent(u.element);

                int row = hasElementRows && elementRows.Count > 0 ? elementRows[i % elementRows.Count] : (i / Math.Max(1, cols));
                int col = hasElementCols && elementCols.Count > 0 ? elementCols[i % elementCols.Count] : (i % cols);
                row = Math.Max(0, Math.Min(row, rows - 1));
                col = Math.Max(0, Math.Min(col, cols - 1));

                table.Add(u.element, col, row);
            }

            // Apply column scaling (star vs absolute).
            if (hasColDefs)
            {
                for (int c = 0; c < colDefs.Count && c < table.Dimensions.Width; c++)
                {
                    bool scale = IsStarDefinition(colDefs[c], out _);
                    table.SetColumnScale(c, scale);
                }
            }
            if (hasRowDefs)
            {
                for (int r = 0; r < rowDefs.Count && r < table.Dimensions.Height; r++)
                {
                    bool scale = IsStarDefinition(rowDefs[r], out _);
                    table.SetRowScale(r, scale);
                }
            }

            if (hasWidth) table.Width = (int)width;
            if (hasHeight) table.Height = (int)height;

            DA.SetData("Grid", new UIElement_Goo(table, "Grid", InstanceGuid, DA.Iteration));
        }

        /// <summary>
        /// Parse a row/column definition string. Returns true if the entry uses
        /// star sizing ("1*", "*", "2*"); the out value is the ratio (1 for "*").
        /// Plain numbers ("50", "100") return false and leave the table cell at
        /// its absolute size.
        /// </summary>
        private static bool IsStarDefinition(string def, out double ratio)
        {
            ratio = 1.0;
            if (string.IsNullOrWhiteSpace(def)) return true;
            def = def.Trim();
            if (def == "*") return true;
            if (def.EndsWith("*"))
            {
                double.TryParse(def.Substring(0, def.Length - 1), out ratio);
                if (ratio <= 0) ratio = 1;
                return true;
            }
            return false;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.createGrid;

        public override Guid ComponentGuid => new Guid("{B618569A-868D-4A88-A035-FAA1416A841F}");
    }
}
