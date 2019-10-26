using System;
using System.Text;

using java.io;
using javax.xml.parsers;

using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.pdmodel.common.filespecification;

using org.w3c.dom;
using org.xml.sax;

namespace NE4ZUGFeRD.Logic
{
    class NE4ZUGFeRDImporter
    {
        /*
        call extract(importFilename).
        containsMeta() will return if ZUGFeRD data has been found,
        afterwards you can call getBIC(), getIBAN() etc.
        */
        /**@var if metadata has been found */
        private bool containsMeta = false;
        /**@var the reference (i.e. invoice number) of the sender */
        private String foreignReference;
        private String BIC;
        private String IBAN;
        private String holder;
        private String amount;
        /** Raw XML form of the extracted data - may be directly obtained. */
        private byte[] rawXML = null;
        private String bankName;
        private bool amountFound;
        /**
        * Extracts a ZUGFeRD invoice from a PDF document represented by a file name.
        * Errors are just logged to STDOUT.
        */
        public void extract(String pdfFilename)
        {
            try
            {
                extractLowLevel(new BufferedInputStream(new FileInputStream(pdfFilename)));
            }
            catch (IOException ioe)
            {
                ioe.printStackTrace();
            }
        }
        /**
        * Extracts a ZUGFeRD invoice from a PDF document represented by an input stream.
        * Errors are reported via exception handling.
        */
        public void extractLowLevel(InputStream pdfStream)
        {
            PDDocument doc = null;
            try
            {
                doc = PDDocument.load(pdfStream);
                // PDDocumentInformation info = doc.getDocumentInformation();
                PDDocumentNameDictionary names = new PDDocumentNameDictionary(
                doc.getDocumentCatalog());
                PDEmbeddedFilesNameTreeNode etn;
                etn = names.getEmbeddedFiles();
                if (etn == null)
                {
                    doc.close();
                    return;
                }
                //Set efMap = etn.getNames().keySet();
                Object[] efMap = etn.getNames().keySet().toArray();
                //Map<String, COSObjectable> efMap = etn.getNames();
                // String filePath = "/tmp/";
                //for (String filename : efMap.keySet()) {
                foreach (String filename in efMap)
                {
                    ///**
                    //* currently (in the release candidate of version 1) only one
                    //* attached file with the name ZUGFeRD-invoice.xml is allowed
                    //* */
                    if (filename.Equals("ZUGFeRD-invoice.xml"))
                    { //$NON-NLS-1$
                        containsMeta = true;
                        PDComplexFileSpecification fileSpec = (PDComplexFileSpecification)etn.getNames().get(filename);
                        PDEmbeddedFile embeddedFile = fileSpec.getEmbeddedFile();
                        // String embeddedFilename = filePath + filename;
                        // File file = new File(filePath + filename);
                        // System.out.println("Writing " + embeddedFilename);
                        // ByteArrayOutputStream fileBytes=new
                        // ByteArrayOutputStream();
                        // FileOutputStream fos = new FileOutputStream(file);
                        rawXML = embeddedFile.getByteArray();
                        setMeta(Encoding.UTF8.GetString(rawXML));
                        // fos.write(embeddedFile.getByteArray());
                        // fos.close();
                    }
                }
            }
            catch (IOException e1)
            {
                throw e1;
            }
            finally
            {
                try
                {
                    if (doc != null)
                    {
                        doc.close();
                    }
                }
                catch (IOException e) { }
            }
        }
        public void parse()
        {
            DocumentBuilderFactory factory = null;
            DocumentBuilder builder = null;
            Document document = null;
            factory = DocumentBuilderFactory.newInstance();
            factory.setNamespaceAware(true); //otherwise we can not act namespace independend, i.e. use document.getElementsByTagNameNS("*",...
            try
            {
                builder = factory.newDocumentBuilder();
            }
            catch (ParserConfigurationException ex3)
            {
                // TODO Auto-generated catch block
                ex3.printStackTrace();
            }
            try
            {
                InputStream bais = new ByteArrayInputStream(rawXML);
                document = builder.parse(bais);
            }
            catch (SAXException ex1)
            {
                ex1.printStackTrace();
            }
            catch (IOException ex2)
            {
                ex2.printStackTrace();
            }
            NodeList ndList;
            // rootNode = document.getDocumentElement();
            // ApplicableSupplyChainTradeSettlement
            ndList = document.getDocumentElement()
            .getElementsByTagNameNS("*", "PaymentReference"); //$NON-NLS-1$
            for (int bookingIndex = 0; bookingIndex < ndList
            .getLength(); bookingIndex++)
            {
                Node booking = ndList.item(bookingIndex);
                // if there is a attribute in the tag number:value
                setForeignReference(booking.getTextContent());
            }
            /*
            ndList = document
            .getElementsByTagName("GermanBankleitzahlID"); //$NON-NLS-1$
            for (int bookingIndex = 0; bookingIndex < ndList
            .getLength(); bookingIndex++) {
            Node booking = ndList.item(bookingIndex);
            // if there is a attribute in the tag number:value
            setBIC(booking.getTextContent());
            }
            ndList = document.getElementsByTagName("ProprietaryID"); //$NON-NLS-1$
            for (int bookingIndex = 0; bookingIndex < ndList
            .getLength(); bookingIndex++) {
            Node booking = ndList.item(bookingIndex);
            // if there is a attribute in the tag number:value
            setIBAN(booking.getTextContent());
            }
            <ram:PayeePartyCreditorFinancialAccount>
            <ram:IBANID>DE1234</ram:IBANID>
            </ram:PayeePartyCreditorFinancialAccount>
            <ram:PayeeSpecifiedCreditorFinancialInstitution>
            <ram:BICID>DE5656565</ram:BICID>
            <ram:Name>Commerzbank</ram:Name>
            </ram:PayeeSpecifiedCreditorFinancialInstitution>
            */
            ndList = document.getElementsByTagNameNS("*", "PayeePartyCreditorFinancialAccount"); //$NON-NLS-1$
            for (int bookingIndex = 0; bookingIndex < ndList
            .getLength(); bookingIndex++)
            {
                Node booking = ndList.item(bookingIndex);
                // there are many "name" elements, so get the one below
                // SellerTradeParty
                NodeList bookingDetails = booking.getChildNodes();
                for (int detailIndex = 0; detailIndex < bookingDetails
                .getLength(); detailIndex++)
                {
                    Node detail = bookingDetails.item(detailIndex);
                    if ((detail.getLocalName() != null) && (detail.getLocalName().Equals("IBANID")))
                    { //$NON-NLS-1$
                        setIBAN(detail.getTextContent());
                    }
                }
            }
            ndList = document.getElementsByTagNameNS("*", "PayeeSpecifiedCreditorFinancialInstitution"); //$NON-NLS-1$
            for (int bookingIndex = 0; bookingIndex < ndList
            .getLength(); bookingIndex++)
            {
                Node booking = ndList.item(bookingIndex);
                // there are many "name" elements, so get the one below
                // SellerTradeParty
                NodeList bookingDetails = booking.getChildNodes();
                for (int detailIndex = 0; detailIndex < bookingDetails
                .getLength(); detailIndex++)
                {
                    Node detail = bookingDetails.item(detailIndex);
                    if ((detail.getLocalName() != null) && (detail.getLocalName().Equals("BICID")))
                    { //$NON-NLS-1$
                        setBIC(detail.getTextContent());
                    }
                    if ((detail.getLocalName() != null) && (detail.getLocalName().Equals("Name")))
                    { //$NON-NLS-1$
                        setBankName(detail.getTextContent());
                    }
                }
            }
            //
            ndList = document.getElementsByTagNameNS("*", "SellerTradeParty"); //$NON-NLS-1$
            for (int bookingIndex = 0; bookingIndex < ndList
            .getLength(); bookingIndex++)
            {
                Node booking = ndList.item(bookingIndex);
                // there are many "name" elements, so get the one below
                // SellerTradeParty
                NodeList bookingDetails = booking.getChildNodes();
                for (int detailIndex = 0; detailIndex < bookingDetails
                .getLength(); detailIndex++)
                {
                    Node detail = bookingDetails.item(detailIndex);
                    if ((detail.getLocalName() != null) && (detail.getLocalName().Equals("Name")))
                    { //$NON-NLS-1$
                        setHolder(detail.getTextContent());
                    }
                }
            }
            ndList = document.getElementsByTagNameNS("*", "DuePayableAmount"); //$NON-NLS-1$
            for (int bookingIndex = 0; bookingIndex < ndList
            .getLength(); bookingIndex++)
            {
                Node booking = ndList.item(bookingIndex);
                // if there is a attribute in the tag number:value
                amountFound = true;
                setAmount(booking.getTextContent());
            }
            if (!amountFound)
            {
                /* there is apparently no requirement to mention DuePayableAmount,,
                * if it's not there, check for GrandTotalAmount
                */
                ndList = document.getElementsByTagNameNS("*", "GrandTotalAmount"); //$NON-NLS-1$
                for (int bookingIndex = 0; bookingIndex < ndList
                .getLength(); bookingIndex++)
                {
                    Node booking = ndList.item(bookingIndex);
                    // if there is a attribute in the tag number:value
                    amountFound = true;
                    setAmount(booking.getTextContent());
                }
            }
        }
        public bool ContainsMeta()
        {
            return containsMeta;
        }
        public String getForeignReference()
        {
            return foreignReference;
        }
        public void setForeignReference(String foreignReference)
        {
            this.foreignReference = foreignReference;
        }
        public String getBIC()
        {
            return BIC;
        }
        private void setBIC(String bic)
        {
            this.BIC = bic;
        }
        private void setBankName(String bankname)
        {
            this.bankName = bankname;
        }
        public String getIBAN()
        {
            return IBAN;
        }
        public String getBankName()
        {
            return bankName;
        }
        public void setIBAN(String IBAN)
        {
            this.IBAN = IBAN;
        }
        public String getHolder()
        {
            return holder;
        }
        private void setHolder(String holder)
        {
            this.holder = holder;
        }
        public String getAmount()
        {
            return amount;
        }
        private void setAmount(String amount)
        {
            this.amount = amount;
        }
        public void setMeta(String meta)
        {
            this.rawXML = Encoding.UTF8.GetBytes(meta);
        }
        public String getMeta()
        {
            if (rawXML == null)
            {
                return null;
            }
            else
            {
                return Encoding.UTF8.GetString(rawXML);
            }
        }
        /**
        * Returns the raw XML data as extracted from the ZUGFeRD PDF file.
        */
        public byte[] getRawXML()
        {
            return rawXML;
        }
        /**
        * will return true if the metadata (just extract-ed or set with setMeta) contains ZUGFeRD XML
        * */
        public bool canParse()
        {
            //SpecifiedExchangedDocumentContext is in the schema, so a relatively good indication if zugferd is present - better than just invoice
            String meta = getMeta();
            return (meta != null) && (meta.Length > 0) && (meta.Contains("SpecifiedExchangedDocumentContext")); //$NON-NLS-1$
        }
    }
}
