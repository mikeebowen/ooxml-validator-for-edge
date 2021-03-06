using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

namespace OOXML.Validator
{
    public class Startup
    {
        public enum FormatVersion
        {
            Office2007,
            Office2010,
            Office2013,
            Office2016,
            Office2019
        }

        public async Task<object> Invoke(object input)
        {
            string fileName = JsonDocument.Parse(input.ToString()).RootElement.GetProperty("fileName").ToString();
            string formatString = JsonDocument.Parse(input.ToString()).RootElement.GetProperty("format").ToString();
            bool validInt = int.TryParse(formatString, out int format);
            int defaultFormatVersion = Enum.GetNames(typeof(FormatVersion)).Length - 1;
            if (format < 0 || format > defaultFormatVersion)
            {
                throw new ArgumentOutOfRangeException("Office version must be 0 = Office 2007, 1 = Office 2010, 2 = Office 2013, 3 = Office 2016, 4 = Office 2019");
            }
            if (fileName == null)
            {
                throw new ArgumentNullException();
            }
            string fileExtension = fileName.Substring(Math.Max(0, fileName.Length - 4)).ToLower();

            if (!new string[] { "docx", "pptx", "xlsx" }.Contains(fileExtension))
            {
                throw new ArgumentException("file must be a .docx, .xlsx, or .pptx");
            }
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }
            dynamic doc = null;
            switch (fileExtension)
            {
                case "docx":
                    doc = WordprocessingDocument.Open(fileName, false);
                    break;
                case "pptx":
                    doc = PresentationDocument.Open(fileName, false);
                    break;
                case "xlsx":
                    doc = SpreadsheetDocument.Open(fileName, false);
                    break;
                default:
                    break;
            }
            dynamic ffv;
            int num = validInt && format > 0 && format <= defaultFormatVersion ? format : defaultFormatVersion;
            FormatVersion fv = (FormatVersion)num;
            switch (fv)
            {
                case FormatVersion.Office2007:
                    ffv = FileFormatVersions.Office2007;
                    break;
                case FormatVersion.Office2010:
                    ffv = FileFormatVersions.Office2010;
                    break;
                case FormatVersion.Office2013:
                    ffv = FileFormatVersions.Office2013;
                    break;
                case FormatVersion.Office2016:
                    ffv = FileFormatVersions.Office2016;
                    break;
                case FormatVersion.Office2019:
                    ffv = FileFormatVersions.Office2019;
                    break;
                default:
                    ffv = FileFormatVersions.Office2016;
                    break;
            }
            OpenXmlValidator openXmlValidator = new OpenXmlValidator(ffv);
            IEnumerable<ValidationErrorInfo> validationErrorInfos = openXmlValidator.Validate(doc);
            return await Task.FromResult(JsonSerializer.Serialize(validationErrorInfos, new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.Preserve
            }));
        }
    }
}
