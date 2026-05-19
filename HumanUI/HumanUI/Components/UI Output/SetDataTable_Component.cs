using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace HumanUI.Components
{
    /// <summary>
    /// Replace the rows (and optionally the column headers) of an existing Eto
    /// GridView created by CreateDataTable. If column headings are omitted the
    /// existing GridColumn header text is preserved.
    /// </summary>
    public class SetDataTable_Component : GH_Component
    {
        public SetDataTable_Component()
            : base("Set Data Table", "SetDataTable",
                "Update the contents of a Data Table",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Data Table to Set", "DT", "The data table to set", GH_ParamAccess.item);
            pManager.AddTextParameter("Data", "D", "The data to display in the table, organized in branches by column.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Column Headings", "C", "The heading for each column, one for each column in Data", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object dataTableObj = null;
            if (!DA.GetData(0, ref dataTableObj)) return;
            GH_Structure<GH_String> data;
            var columnHeadings = new List<string>();

            DA.GetDataTree("Data", out data);
            bool hasHeadings = DA.GetDataList("Column Headings", columnHeadings);

            var gv = HUI_Util.GetUIElement<GridView>(dataTableObj);
            if (gv == null) return;

            if (!hasHeadings)
            {
                columnHeadings = gv.Columns.Select(c => c.HeaderText).ToList();
            }
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

            if (hasHeadings)
            {
                // Re-build columns when the user supplied new headings; otherwise keep
                // the existing column shape and just swap the row data.
                while (gv.Columns.Count > 0) gv.Columns.RemoveAt(0);
                for (int c = 0; c < columnHeadings.Count; c++)
                {
                    int captured = c;
                    gv.Columns.Add(new GridColumn
                    {
                        HeaderText = columnHeadings[c],
                        Editable = false,
                        Resizable = true,
                        DataCell = new TextBoxCell
                        {
                            Binding = Binding.Delegate<string[], string>(row => row != null && captured < row.Length ? row[captured] : string.Empty),
                        },
                    });
                }
            }

            int rowCount = 0;
            for (int c = 0; c < data.Branches.Count; c++) rowCount = Math.Max(rowCount, data.Branches[c].Count);
            var rows = new ObservableCollection<string[]>();
            for (int r = 0; r < rowCount; r++)
            {
                var row = new string[columnHeadings.Count];
                for (int c = 0; c < columnHeadings.Count; c++)
                {
                    if (c < data.Branches.Count && r < data.Branches[c].Count)
                        row[c] = data.Branches[c][r]?.Value ?? string.Empty;
                    else row[c] = string.Empty;
                }
                rows.Add(row);
            }
            gv.DataStore = rows;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetDataTable;

        public override Guid ComponentGuid => new Guid("{c2462873-d58e-4bb4-a1cd-31e351c133b3}");
    }
}
