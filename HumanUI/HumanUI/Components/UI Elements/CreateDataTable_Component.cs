using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

namespace HumanUI.Components
{
    /// <summary>
    /// Read-only Eto GridView. Each Grasshopper data branch becomes a column; the
    /// rows are zipped from the parallel branches. Column sizing modes map to
    /// either a fixed width per column (positive integer input) or a single
    /// "auto" / "star" behavior (-1/-2/-3/-4 sentinel values from the legacy
    /// component).
    /// </summary>
    public class CreateDataTable_Component : GH_Component
    {
        public CreateDataTable_Component()
            : base("Create Data Table", "DataTable",
                "Creates a Data Table view",
                "Human UI", "UI Elements")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Data", "D", "The data to display in the table, organized in branches by column.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Column Headings", "C", "The heading for each column, one for each column in Data", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Allow Sorting", "S", "Set to true to allow sorting by column values", GH_ParamAccess.item, true);
            pManager.AddIntegerParameter("Column Sizing Mode", "CS", "Sizing Mode for columns. Pick from predefined values or supply a fixed numerical width.", GH_ParamAccess.item, -1);
            var sizingMode = pManager[3] as Param_Integer;
            sizingMode.AddNamedValue("Equal", -1);
            sizingMode.AddNamedValue("Size to Cells", -2);
            sizingMode.AddNamedValue("Size to Header", -3);
            sizingMode.AddNamedValue("Auto", -4);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("DataTable", "DT", "The Data Table element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_String> data;
            var columnHeadings = new List<string>();
            bool allowSorting = true;
            int columnWidth = -1;

            if (!DA.GetDataTree("Data", out data)) return;
            DA.GetDataList("Column Headings", columnHeadings);
            DA.GetData("Allow Sorting", ref allowSorting);
            DA.GetData("Column Sizing Mode", ref columnWidth);

            if (columnHeadings.Count != data.Branches.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Column Heading Count doesn't match the data");
                return;
            }
            if (columnHeadings.Distinct().Count() != columnHeadings.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Column headings must be unique.");
                return;
            }

            var gv = BuildGridView(data, columnHeadings, allowSorting, columnWidth);
            DA.SetData("DataTable", new UIElement_Goo(gv, "Data Table", InstanceGuid, DA.Iteration));
        }

        internal static GridView BuildGridView(GH_Structure<GH_String> data, List<string> columnHeadings, bool allowSorting, int columnWidth)
        {
            int colCount = columnHeadings.Count;
            int rowCount = 0;
            for (int c = 0; c < data.Branches.Count; c++) rowCount = Math.Max(rowCount, data.Branches[c].Count);

            var rows = new ObservableCollection<string[]>();
            for (int r = 0; r < rowCount; r++)
            {
                var row = new string[colCount];
                for (int c = 0; c < colCount; c++)
                {
                    if (c < data.Branches.Count && r < data.Branches[c].Count)
                        row[c] = data.Branches[c][r]?.Value ?? string.Empty;
                    else row[c] = string.Empty;
                }
                rows.Add(row);
            }

            var gv = new GridView
            {
                DataStore = rows,
                AllowMultipleSelection = false,
                AllowColumnReordering = true,
                ShowHeader = true,
                ID = "GH_DataTable",
            };

            for (int c = 0; c < colCount; c++)
            {
                int captured = c;
                var col = new GridColumn
                {
                    HeaderText = columnHeadings[c],
                    Sortable = allowSorting,
                    Resizable = true,
                    Editable = false,
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Delegate<string[], string>(row => row != null && captured < row.Length ? row[captured] : string.Empty),
                    },
                };
                if (columnWidth > 0) col.Width = columnWidth;
                else if (columnWidth == -2 || columnWidth == -3 || columnWidth == -4) col.AutoSize = true;
                gv.Columns.Add(col);
            }
            return gv;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.DataTable;

        public override Guid ComponentGuid => new Guid("{b29e654e-b952-4d58-acf2-a60c4358d6e3}");
    }
}
