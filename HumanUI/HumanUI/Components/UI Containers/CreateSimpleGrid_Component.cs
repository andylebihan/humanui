using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace HumanUI.Components.UI_Containers
{
    /// <summary>
    /// Lays out a GH tree as a grid: each branch becomes a column, each item in the
    /// branch becomes a row. Eto.Forms.TableLayout backs the implementation.
    /// </summary>
    public class CreateSimpleGrid_Component : GH_Component
    {
        public CreateSimpleGrid_Component()
          : base("Create Simple Grid", "SimpleGrid",
                "Create a container with elements in a grid according to the path structure provided. Each branch path will be treated as a column and Elements will be placed in the column from top to bottom. Use the \"Adjust Element Positioning\" component to locate elements inside the grid cell. Use column and row definitions to control sizing.",
                "Human UI", "UI Containers")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Elements", "E", "The UI elements to place in the grid. Each path branch will form a column of the provided UI Elements from top to bottom.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Grid Membership", "M", "List of index numbers to place elements in different Grids. List length must match the number of Element paths.", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddNumberParameter("Width", "W", "The width of the grid", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddNumberParameter("Height", "H", "The height of the grid", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddTextParameter("Row Definitions", "RD", "An optional repeating pattern of Row Heights - use 'Auto' to inherit, numbers for absolute sizes, and numbers with * for ratios (like 1* and 2* for a 1/3 2/3 split)", GH_ParamAccess.list, "Auto");
            pManager[4].Optional = true;
            pManager.AddTextParameter("Column Definitions", "CD", "An optional flat list of Column Widths - use 'Auto' to inherit, use numbers for absolute sizes, and numbers with * for ratios (like 1* and 2* for a 1/3 2/3 split)", GH_ParamAccess.list, "Auto");
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Simple Grid", "S", "The combined group of elements", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetDataTree(0, out GH_Structure<IGH_Goo> elementsToAdd)) return;

            var memberships = new List<int>();
            bool hasMemberships = DA.GetDataList("Grid Membership", memberships);
            double width = 0, height = 0;
            bool hasWidth = DA.GetData("Width", ref width);
            bool hasHeight = DA.GetData("Height", ref height);
            var rowDefs = new List<string>();
            var colDefs = new List<string>();
            bool hasRowDefs = DA.GetDataList("Row Definitions", rowDefs);
            bool hasColDefs = DA.GetDataList("Column Definitions", colDefs);

            if (hasMemberships && memberships.Count != elementsToAdd.Branches.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Grid Membership list length must equal the number of Elements branches.");
                return;
            }

            // Group branches by membership index. Branches with no membership all go to 0.
            var groups = Enumerable.Range(0, elementsToAdd.Branches.Count)
                .GroupBy(i => hasMemberships ? memberships[i] : 0)
                .OrderBy(g => g.Key);

            var output = new List<UIElement_Goo>();
            foreach (var group in groups)
            {
                var columns = group.Select(i => elementsToAdd.get_Branch(elementsToAdd.get_Path(i))).ToList();
                int rowCount = columns.Max(b => b.Count);
                var table = new TableLayout(columns.Count, rowCount) { Spacing = new Eto.Drawing.Size(4, 4) };
                if (hasWidth) table.Width = (int)width;
                if (hasHeight) table.Height = (int)height;
                table.ID = "GH_Grid";

                // Sizing flags from RowDefinitions/ColumnDefinitions. A "*" entry marks the
                // axis as proportionally scaling.
                ApplyAxisScales(table, colDefs, rowDefs);

                for (int col = 0; col < columns.Count; col++)
                {
                    for (int row = 0; row < columns[col].Count; row++)
                    {
                        if (columns[col][row] is UIElement_Goo goo && goo.element != null)
                        {
                            HUI_Util.removeParent(goo.element);
                            table.Add(goo.element, col, row);
                        }
                    }
                }

                output.Add(new UIElement_Goo(table, "Simple Grid", InstanceGuid, DA.Iteration));
            }

            DA.SetDataList("Simple Grid", output);
        }

        // Translate the historical RowDefinitions / ColumnDefinitions string syntax to
        // Eto's per-row / per-column scale-or-not booleans. Numbers ending in '*' scale,
        // numbers and 'Auto' do not. Eto's TableLayout doesn't expose per-cell ratios the
        // way WPF does so this is an approximation -- precise ratios will need a
        // follow-up pass on a more capable layout primitive.
        private static void ApplyAxisScales(TableLayout table, List<string> colDefs, List<string> rowDefs)
        {
            if (colDefs != null)
            {
                for (int c = 0; c < table.Dimensions.Width && c < colDefs.Count; c++)
                {
                    bool scale = colDefs[c % colDefs.Count].TrimEnd().EndsWith("*");
                    table.SetColumnScale(c, scale);
                }
            }
            if (rowDefs != null)
            {
                for (int r = 0; r < table.Dimensions.Height && r < rowDefs.Count; r++)
                {
                    bool scale = rowDefs[r % rowDefs.Count].TrimEnd().EndsWith("*");
                    table.SetRowScale(r, scale);
                }
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.simpleGrid;

        public override Guid ComponentGuid => new Guid("4df77b45-0d74-44ea-9445-6d5d8b1d17ad");
    }
}
