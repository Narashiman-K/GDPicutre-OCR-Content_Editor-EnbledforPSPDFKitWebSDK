using ConsoleApp2;
using GdPicture14;
using System.Runtime.CompilerServices;
using static ConsoleApp2.JsonStructure;

LicenseManager lm = new();
lm.RegisterKEY("");


bool InputIsPDF = true;
string filePath = @"Z:\temps\input.pdf";
string ocrResourcesFolder = @"C:\GdPicture.NET 14\Redist\OCR";

try
{
    using (GdPictureImaging gdpimg = new GdPictureImaging())
    {
        int imageID = 0;
        if (InputIsPDF)
        {
            using (GdPicturePDF pdf = new GdPicturePDF())
            {
                GdPictureStatus status = pdf.LoadFromFile(filePath);
                if (status != GdPictureStatus.OK)
                {
                    throw new Exception("Failed to load PDF: " + status.ToString());
                }

                imageID = pdf.RenderPageToGdPictureImageEx(300, true);
                if (imageID == 0)
                {
                    throw new Exception("Failed to render PDF page to image.");
                }
            }
        }
        else
        {
            imageID = gdpimg.CreateGdPictureImageFromFile(filePath);
            if (imageID == 0)
            {
                throw new Exception("Failed to create image from file.");
            }
        }

        using (GdPictureOCR ocr = new GdPictureOCR())
        {
            ocr.ResourcesFolder = ocrResourcesFolder;
            ocr.AddLanguage(OCRLanguage.English);
            ocr.SetImage(imageID);

            string ocrid = ocr.RunOCR();
            if (string.IsNullOrEmpty(ocrid))
            {
                throw new Exception("OCR process failed to generate an ID.");
            }

            string serializedResult = ocr.GetSerializedResult(ocrid);

            if (string.IsNullOrEmpty(serializedResult))
            {
                throw new Exception("OCR process returned an empty serialized result.");
            }

            JsonStructure rootObject = JsonStructure.FromJson(serializedResult);
            if (rootObject == null)
            {
                throw new Exception("Failed to deserialize OCR result.");
            }

            using (GdPicturePDF pdf = new GdPicturePDF())
            {
                pdf.NewPDF();
                pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitInch);
                pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
                pdf.AddImageFromGdPictureImage(imageID, false, true);


                string fontRes = pdf.AddTrueTypeFontU("Arial", false, false, true);

                string fontName = pdf.AddStandardFont(PdfStandardFont.PdfStandardFontTimesBold);
                Dictionary<Roi, (string value, float fontSize)> words = new Dictionary<Roi, (string, float)>();

                foreach (var word in rootObject.Words)
                {
                    var wordValue = "";

                    foreach (var character in rootObject.Characters)
                    {
                        if (IsInBBox(word.BBox, character.BBox))
                        {
                            wordValue += character.Value;
                        }
                        else if (wordValue.Length > 0)
                        {
                            words.Add(word.BBox, (wordValue, word.FontPointSize));
                            break;
                        }
                    }
                }

                
                foreach (var block in words)
                {
                    Console.WriteLine($"{block.Value}");

                    // ---- This part is to put a white rectangle where the OCR detected the text, and once the rectangle has been applied, we draw the text ----
                    // ps: if you want to delete teh image directly instead of deleting the text on the image, comment that code and uncomment the code line 125-131
                    pdf.SetFillColor(255, 255, 255);
                    pdf.DrawRectangle(block.Key.Left / 300, block.Key.Top / 300, block.Key.Width / 300, block.Key.Height / 300, true, false);
                    pdf.SetFillColor(0, 0, 0);

                    // ------------------------------------------------------------------------------------------------

                    pdf.SetTextSize(block.Value.fontSize);
                    pdf.DrawText(fontRes, block.Key.Left/300, (((block.Key.Top - (block.Key.Top - block.Key.Bottom) / 2)) / 300), block.Value.value);
                    if (pdf.GetStat() != GdPictureStatus.OK)
                        Console.WriteLine("Error: " + pdf.GetStat());

                }


                //----- Activate this part is you want to completely delete the background image -----//

                //string resName = "";
                //resName = pdf.GetPageImageResName(0);
                //pdf.DeleteImage(resName);

                // ----------------------------------------------------------------------------------------

                if (pdf.GetStat() != GdPictureStatus.OK)
                    Console.WriteLine("Error delete image: " + pdf.GetStat());

                if (pdf.SaveToFile(@"C:\temp\output.pdf") != GdPictureStatus.OK)
                {
                    Console.WriteLine("Error: " + pdf.GetStat());
                }
            }
        }
        gdpimg.ReleaseGdPictureImage(imageID);
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}

bool IsInBBox(Roi roi1, Roi roi2)
{
    // Check if there is no intersection along the X axis
    if (roi1.Right < roi2.Left || roi1.Left > roi2.Right)
    {
        return false;
    }

    // Check if there is no intersection along the Y axis
    if (roi1.Bottom < roi2.Top || roi1.Top > roi2.Bottom)
    {
        return false;
    }

    // If all tests pass, the bounding boxes intersect
    return true;
}
