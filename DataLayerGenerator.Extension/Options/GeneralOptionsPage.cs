using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DataLayerGenerator.Options
{
    /// <summary>
    /// Options page for Data Layer Generator settings
    /// </summary>
    [ComVisible(true)]
    [Guid("B8C3D4E5-F6A7-8901-BCDE-F12345678901")]
    public class GeneralOptionsPage : DialogPage
    {
        private readonly GeneratorOptions _options = new GeneratorOptions();

        [Category("General")]
        [DisplayName("Data Layer Folder Name")]
        [Description("The name of the folder where data layer files will be created.")]
        [DefaultValue("Data")]
        public string DataLayerFolderName
        {
            get => _options.DataLayerFolderName;
            set => _options.DataLayerFolderName = value;
        }

        [Category("General")]
        [DisplayName("Data Layer Suffix")]
        [Description("The suffix to append to model names for data layer classes (e.g., 'Data', 'Repository').")]
        [DefaultValue("Data")]
        public string DataLayerSuffix
        {
            get => _options.DataLayerSuffix;
            set => _options.DataLayerSuffix = value;
        }

        [Category("General")]
        [DisplayName("Namespace Suffix")]
        [Description("The suffix to append to the model namespace for data layer files.")]
        [DefaultValue(".Data")]
        public string DataLayerNamespaceSuffix
        {
            get => _options.DataLayerNamespaceSuffix;
            set => _options.DataLayerNamespaceSuffix = value;
        }

        [Category("General")]
        [DisplayName("DbContext Name")]
        [Description("The name of your DbContext class (e.g., 'ApplicationDbContext').")]
        [DefaultValue("ApplicationDbContext")]
        public string DbContextName
        {
            get => _options.DbContextName;
            set => _options.DbContextName = value;
        }

        [Category("Code Generation")]
        [DisplayName("Generate Interfaces")]
        [Description("Generate interfaces for data layer classes.")]
        [DefaultValue(true)]
        public bool GenerateInterfaces
        {
            get => _options.GenerateInterfaces;
            set => _options.GenerateInterfaces = value;
        }

        [Category("Code Generation")]
        [DisplayName("Create Interfaces Folder")]
        [Description("Create a separate 'Interfaces' subfolder for interface files.")]
        [DefaultValue(true)]
        public bool CreateInterfacesFolder
        {
            get => _options.CreateInterfacesFolder;
            set => _options.CreateInterfacesFolder = value;
        }

        [Category("CRUD Methods")]
        [DisplayName("Generate GetAll Method")]
        [Description("Generate GetAll() and GetAllAsync() methods.")]
        [DefaultValue(true)]
        public bool GenerateGetAll
        {
            get => _options.GenerateGetAll;
            set => _options.GenerateGetAll = value;
        }

        [Category("CRUD Methods")]
        [DisplayName("Generate GetById Method")]
        [Description("Generate GetByIdAsync() method.")]
        [DefaultValue(true)]
        public bool GenerateGetById
        {
            get => _options.GenerateGetById;
            set => _options.GenerateGetById = value;
        }

        [Category("CRUD Methods")]
        [DisplayName("Generate Add Method")]
        [Description("Generate AddAsync() method.")]
        [DefaultValue(true)]
        public bool GenerateAdd
        {
            get => _options.GenerateAdd;
            set => _options.GenerateAdd = value;
        }

        [Category("CRUD Methods")]
        [DisplayName("Generate Update Method")]
        [Description("Generate UpdateAsync() method.")]
        [DefaultValue(true)]
        public bool GenerateUpdate
        {
            get => _options.GenerateUpdate;
            set => _options.GenerateUpdate = value;
        }

        [Category("CRUD Methods")]
        [DisplayName("Generate Delete Method")]
        [Description("Generate DeleteAsync() method.")]
        [DefaultValue(true)]
        public bool GenerateDelete
        {
            get => _options.GenerateDelete;
            set => _options.GenerateDelete = value;
        }

        [Category("Advanced")]
        [DisplayName("Use Primary Constructor")]
        [Description("Use C# 12 primary constructor syntax for dependency injection.")]
        [DefaultValue(true)]
        public bool UsePrimaryConstructor
        {
            get => _options.UsePrimaryConstructor;
            set => _options.UsePrimaryConstructor = value;
        }

        [Category("Advanced")]
        [DisplayName("Include Custom Query Placeholder")]
        [Description("Add commented placeholder for custom query methods.")]
        [DefaultValue(false)]
        public bool IncludeCustomQueryPlaceholder
        {
            get => _options.IncludeCustomQueryPlaceholder;
            set => _options.IncludeCustomQueryPlaceholder = value;
        }

        [Category("Advanced")]
        [DisplayName("Use AsNoTracking")]
        [Description("Use AsNoTracking() for read-only queries.")]
        [DefaultValue(true)]
        public bool UseAsNoTracking
        {
            get => _options.UseAsNoTracking;
            set => _options.UseAsNoTracking = value;
        }

        [Category("Advanced")]
        [DisplayName("Add XML Documentation")]
        [Description("Add XML documentation comments to generated methods.")]
        [DefaultValue(true)]
        public bool AddXmlDocumentation
        {
            get => _options.AddXmlDocumentation;
            set => _options.AddXmlDocumentation = value;
        }

        /// <summary>
        /// Gets the underlying options data model
        /// </summary>
        public GeneratorOptions GetOptions()
        {
            return _options;
        }

        /// <summary>
        /// Validates settings when applied
        /// </summary>
        protected override void OnApply(PageApplyEventArgs e)
        {
            // Validate folder name
            if (string.IsNullOrWhiteSpace(DataLayerFolderName))
            {
                e.ApplyBehavior = ApplyKind.CancelNoNavigate;
                return;
            }

            // Validate suffix
            if (string.IsNullOrWhiteSpace(DataLayerSuffix))
            {
                DataLayerSuffix = "Data";
            }

            // Validate DbContext name
            if (string.IsNullOrWhiteSpace(DbContextName))
            {
                DbContextName = "ApplicationDbContext";
            }

            base.OnApply(e);
        }
    }

    /// <summary>
    /// Options data model - can be used without Visual Studio dependencies
    /// </summary>
    public class GeneratorOptions
    {
        public string DataLayerFolderName { get; set; } = "Data";
        public string DataLayerSuffix { get; set; } = "Data";
        public string DataLayerNamespaceSuffix { get; set; } = ".Data";
        public string DbContextName { get; set; } = "ApplicationDbContext";
        public bool GenerateInterfaces { get; set; } = true;
        public bool CreateInterfacesFolder { get; set; } = true;
        public bool GenerateGetAll { get; set; } = true;
        public bool GenerateGetById { get; set; } = true;
        public bool GenerateAdd { get; set; } = true;
        public bool GenerateUpdate { get; set; } = true;
        public bool GenerateDelete { get; set; } = true;
        public bool UsePrimaryConstructor { get; set; } = true;
        public bool IncludeCustomQueryPlaceholder { get; set; } = false;
        public bool UseAsNoTracking { get; set; } = true;
        public bool AddXmlDocumentation { get; set; } = true;
    }

    /// <summary>
    /// Static accessor for options
    /// </summary>
    public static class OptionsProvider
    {
        private static GeneralOptionsPage _optionsPage;

        public static GeneratorOptions GetOptions(Package package)
        {
            if (_optionsPage == null && package != null)
            {
                _optionsPage = (GeneralOptionsPage)package.GetDialogPage(typeof(GeneralOptionsPage));
            }
            return _optionsPage?.GetOptions() ?? new GeneratorOptions();
        }

        public static void ClearCache()
        {
            _optionsPage = null;
        }
    }
}