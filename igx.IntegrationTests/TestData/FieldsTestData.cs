using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace igx.IntegrationTests.TestData
{
    public sealed class FieldsTestData
    {
        private static JObject _model = null;
        private static JObject _assetTypeInsert = null;
        public static string ExecutionUrl = string.Empty;


        private static readonly object padlock = new object();
        private static readonly object padlockAssetType = new object();



        public static JObject FieldsModel
        {
            get
            {
                lock (padlock)
                {
                    if (_model == null)
                    {
                        JObject model = new JObject();
                        model.Add(new JProperty("Action", "Merge"));

                        JArray fieldTypes = new JArray();
                        model.Add(new JProperty("Fields", fieldTypes));
                        //add text field
                        var field1 = new JObject();
                        field1.Add(new JProperty("Category", "string"));
                        field1.Add(new JProperty("FriendlyName", "FriendlyFieldName_" + Guid.NewGuid()));
                        field1.Add(new JProperty("Name", "ApiName" + Guid.NewGuid()));

                        var type = new JObject();
                        field1.Add(new JProperty("Type", type));

                        var text = new JObject();
                        type.Add(new JProperty("Text", text));

                        text.Add(new JProperty("Default value", "Defaul value"));
                        fieldTypes.Add(field1);

                        //add date field
                        var field2 = new JObject();
                        field2.Add(new JProperty("Category", "string"));
                        field2.Add(new JProperty("FriendlyName", "FriendlyFieldName_" + Guid.NewGuid()));
                        field2.Add(new JProperty("Name", "ApiName" + Guid.NewGuid()));

                        var type2 = new JObject();
                        field2.Add(new JProperty("Type", type2));

                        var dateField = new JObject();
                        type2.Add(new JProperty("Date", dateField));

                        dateField.Add(new JProperty("Default value", DateTime.UtcNow));
                        fieldTypes.Add(field2);

                        _model = model;
                    }
                    return _model;
                }
            }
        }
        public static JObject AssetTypeInsert
        {
            get
            {
                lock (padlock)
                {
                    if (_assetTypeInsert == null)
                    {
                        JObject jObject = new JObject();
                        jObject.Add(new JProperty("Name", "FieldsTest_AssetTypeIntegrationTest-" + Guid.NewGuid().ToString()));
                        jObject.Add(new JProperty("Class", "Glossary"));
                        jObject.Add(new JProperty("DisplayFormat", "{Name}"));
                        jObject.Add(new JProperty("Description", "Integration test description!"));

                        JObject iconStyle = new JObject();
                        iconStyle.Add(new JProperty("BackColor", "#FFF"));
                        iconStyle.Add(new JProperty("ForeColor", "#000"));

                        jObject.Add(new JProperty("IconStyle", iconStyle));

                        _assetTypeInsert = jObject;
                    }
                    return _assetTypeInsert;
                }
            }
        }

        public static JObject GetJsonForDelete(List<string> names, string assetTypeUid)
        {
            var obj = new JObject();
            obj.Add("AssetTypeUid", assetTypeUid);

            var fieldArr = new JArray();

            names.ForEach(
                x =>
                {
                    var field = new JObject();
                    field.Add(new JProperty("Name", x));
                    fieldArr.Add(field);
                });


            obj.Add("Fields", fieldArr);
            return obj;
        }
    }
}
