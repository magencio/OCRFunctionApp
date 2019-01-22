# OCRFunctionApp
Example of a Function App which extracts text from TIFF files with both MSOCR and OneOCR APIs.

- Tiff2TextOneOCR: takes each TIFF file pushed into an Azure Blob Storage, extracts each page in the TIFF file, scales them down to the size supported by the API, converts them to one of the formats supported by the API (e.g. JPG), does OCR on the JPG image with *OneOCR* API, puts all the results together and uploads the resultant JSON to a CosmosDB database.
- DisplayTextOneOCR: Reads each JSON document uploaded into the CosmosDB database by Tiff2TextOneOCR and shows the recognized text contained in it.

- Tiff2TextMSOCR: takes each TIFF file pushed into an Azure Blob Storage, extracts each page in the TIFF file, scales them down to the size supported by the API, converts them to one of the formats supported by the API (e.g. JPG), does OCR on the JPG image with *MSOCR* API, puts all the results together and uploads the resultant JSON to a CosmosDB database.
- DisplayTextMSOCR:  Reads each JSON document uploaded into the CosmosDB database by Tiff2TextMSOCR and shows the recognized text contained in it.