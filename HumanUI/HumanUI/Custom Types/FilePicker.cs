using System;
using System.IO;
using Eto.Forms;

namespace HumanUI
{
    public enum fileDialogType
    {
        OpenFileDialog = 0,
        SaveFileDialog = 1,
        FolderBrowserDialog = 2,
    }

    /// <summary>
    /// Eto FilePicker composite: an immutable label-ish path TextBox plus a Browse
    /// button. Opens an Eto file dialog matching the configured mode. The Path
    /// property fires PathChanged when set programmatically or by the dialog so
    /// ValueListener can react.
    /// </summary>
    public class FilePicker : Panel
    {
        private readonly TextBox _tb;
        private readonly fileDialogType _type;
        private string _filter;
        private string _path = string.Empty;

        public bool fileMustExist { get; set; }
        public string StartingPath { get; set; }
        public string filter
        {
            get => _filter;
            set => _filter = value?.Contains("|") == true ? value : (value + "|" + value);
        }

        public event EventHandler PathChanged;

        public string Path
        {
            get => _path;
            set
            {
                if (_path == value) return;
                _path = value ?? string.Empty;
                if (_tb.Text != _path) _tb.Text = _path;
                PathChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public FilePicker(string buttonLabelTxt = "Browse...", fileDialogType type = fileDialogType.OpenFileDialog,
            bool fileMustExist = true, string filterPattern = "All Files|*.*", string startingPath = "")
        {
            this._type = type;
            this.fileMustExist = fileMustExist;
            this.filter = filterPattern;

            bool isFile = false;
            if (!string.IsNullOrEmpty(startingPath))
            {
                try
                {
                    var attr = File.GetAttributes(startingPath);
                    if (!attr.HasFlag(FileAttributes.Directory)) isFile = true;
                }
                catch { /* path may not exist yet; treat as folder */ }
            }
            StartingPath = isFile ? System.IO.Path.GetDirectoryName(startingPath) : startingPath;

            _tb = new TextBox { ReadOnly = true, Text = isFile ? startingPath : string.Empty };
            if (isFile) _path = startingPath;

            var browse = new Button { Text = string.IsNullOrEmpty(buttonLabelTxt) ? "Browse..." : buttonLabelTxt };
            browse.Click += OnBrowseClick;

            var layout = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            layout.Items.Add(new StackLayoutItem(_tb, true));
            layout.Items.Add(browse);
            this.Padding = new Eto.Drawing.Padding(2);
            Content = layout;
            ID = "GH_FilePicker";
        }

        private void OnBrowseClick(object sender, EventArgs e)
        {
            switch (_type)
            {
                case fileDialogType.OpenFileDialog:
                    using (var d = new OpenFileDialog())
                    {
                        if (!string.IsNullOrEmpty(StartingPath)) d.Directory = new Uri(StartingPath);
                        ApplyFilter(d);
                        d.CheckFileExists = fileMustExist;
                        d.MultiSelect = false;
                        if (d.ShowDialog(this) == DialogResult.Ok && !string.IsNullOrEmpty(d.FileName))
                            Path = d.FileName;
                    }
                    break;
                case fileDialogType.SaveFileDialog:
                    using (var d = new SaveFileDialog())
                    {
                        if (!string.IsNullOrEmpty(StartingPath)) d.Directory = new Uri(StartingPath);
                        ApplyFilter(d);
                        if (d.ShowDialog(this) == DialogResult.Ok && !string.IsNullOrEmpty(d.FileName))
                            Path = d.FileName;
                    }
                    break;
                case fileDialogType.FolderBrowserDialog:
                    using (var d = new SelectFolderDialog())
                    {
                        if (!string.IsNullOrEmpty(StartingPath)) d.Directory = StartingPath;
                        if (d.ShowDialog(this) == DialogResult.Ok && !string.IsNullOrEmpty(d.Directory))
                            Path = d.Directory;
                    }
                    break;
            }
        }

        /// <summary>
        /// Translate HUI's pipe-encoded filter string into Eto FileFilter entries.
        /// Format: "Name|*.ext;*.ext2|OtherName|*.foo"
        /// </summary>
        private void ApplyFilter(FileDialog dialog)
        {
            if (string.IsNullOrEmpty(_filter)) return;
            var parts = _filter.Split('|');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                var name = parts[i];
                var patterns = parts[i + 1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                dialog.Filters.Add(new FileFilter(name, patterns));
            }
        }
    }
}
