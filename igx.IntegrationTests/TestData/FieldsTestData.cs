using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.TestData
{
    public sealed class FieldsTestData
    {
        private static FieldTypesApiEditModel _model = null;
        private static AssetTypeInsert _assetTypeInsert = null;
        public static string ExecutionUrl = string.Empty;


        private static readonly object padlock = new object();
        private static readonly object padlockAssetType = new object();



        public static FieldTypesApiEditModel FieldsModel
        {
            get
            {
                lock (padlock)
                {
                    if (_model == null)
                    {
                        _model = new FieldTypesApiEditModel()
                        {
                            Action = FieldTypesApiEditAction.Merge,
                            Fields = new List<FieldTypeApiEditModel>()
                            {
                                new FieldTypeApiEditModel()
                                {
                                    Category = "string",
                                    FriendlyName = "FriendlyFieldName_"+Guid.NewGuid(),
                                    Name = "ApiName"+Guid.NewGuid(),
                                    Type = new FieldTypeDataTypeApiViewModel()
                                    {
                                        Text = new FieldTypeDataTypeTextApiViewModel()
                                        {
                                            DefaultValue = "Default value"
                                        }
                                    }
                                },
                                new FieldTypeApiEditModel()
                                {
                                    Category = "boolean",
                                    FriendlyName = "FriendlyFieldName_"+Guid.NewGuid(),
                                    Name = "ApiName"+Guid.NewGuid(),
                                    Type = new FieldTypeDataTypeApiViewModel()
                                    {
                                        Date = new FieldTypeDataTypeDateApiViewModel()
                                        {
                                            DefaultValue = DateTime.UtcNow
                                        }
                                    }
                                }
                            }
                        };
                    }
                    return _model;
                }
            }
        }
        public static AssetTypeInsert AssetTypeInsert
        {
            get
            {
                lock (padlockAssetType)
                {
                    if (_assetTypeInsert == null)
                    {
                        _assetTypeInsert = new AssetTypeInsert()
                        {
                            Name = "FieldsTest_AssetTypeIntegrationTest-" + Guid.NewGuid().ToString(),
                            Class = AssetTypeClass.Glossary,
                            DisplayFormat = "{Name}",
                            Description = "Integration test description!",
                            ParentUid = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                            IconStyle = new IconStyleInsert()
                            {
                                BackColor = "#FFF",
                                ForeColor = "#000"
                            }
                        };
                    }
                    return _assetTypeInsert;
                }
            }
        }

    }
}
