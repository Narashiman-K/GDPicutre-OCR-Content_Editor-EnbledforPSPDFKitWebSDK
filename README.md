# GDPicutre-OCR-Content_Editor-EnbledforPSPDFKitWebSDK
Using GDPicutre OCRed the scanned images/documents. These documents are enabled or modified to support the PSPDFKit Web for content editing.

Please follow the link: 
https://www.gdpicture.com/ocr-sdk/

Issue: After OCR when the document is loaded in PSPDFKit for content editing it's not working. 
Resolution: The issue encountered stems from the OCR process, which overlays transparent text on top of the image text.
This method is standard to avoid clutter by having both the image's text and the actual PDF text in the same area, which could become quite messy.

The implementation process is somewhat complex. It involves exporting the OCR results as a JSON file, deserializing it, and manually drawing each detected word onto the document. Here’s a brief overview of the steps

1. Rendered the first page of the PDF as an image. This method also works if the input file is an image.
2. Used the GetSerializedResult() method to obtain the full OCR JSON response.
3. Deserialized the JSON (details of the structure are in JSONStructure.cs).
4. Created a new PDF and added the extracted image.
5. Looped through all words detected by the OCR in the JSON. Initially, I used characters, but it proved too chaotic.
6. For each word, I obscured the underlying text with a white rectangle and overlaid it with black text.
   * Note: There’s a commented-out section of code that removes the entire image. You can activate this if the background is unnecessary.
7. Saved the output PDF.
