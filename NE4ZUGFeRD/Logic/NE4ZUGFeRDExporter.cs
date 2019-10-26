/**
 * Mustangproject's ZUGFeRD implementation ZUGFeRD exporter Licensed under the
 * APLv2 (partly ported to C# using IKVM)
 *
 * @date 2014-07-12
 * @version 1.2.0
 * @author jstaerk
 *
 */
using System;
using System.Linq;
using System.Diagnostics;

using java.io;
using java.math;
using java.util;
using java.text;

using org.apache.jempbox.xmp;
using org.apache.jempbox.xmp.pdfa;

using org.apache.pdfbox.cos;

using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.pdmodel.common;
using org.apache.pdfbox.pdmodel.common.filespecification;

namespace NE4ZUGFeRD.Logic
{
    class NE4ZUGFeRDExporter
    {
        /**
        * * You will need Apache PDFBox. To use the ZUGFeRD exporter, implement
        * IZUGFeRDExportableTransaction in yourTransaction (which will require you
        * to implement Product, Item and Contact) then call doc =
        * PDDocument.load(PDFfilename); // automatically add Zugferd to all
        * outgoing invoices ZUGFeRDExporter ze = new ZUGFeRDExporter();
        * ze.PDFmakeA3compliant(doc, "Your application name",
        * System.getProperty("user.name"), true); ze.PDFattachZugferdFile(doc,
        * yourTransaction);
        *
        * doc.save(PDFfilename);
        *
        * @author jstaerk
        * @throws javax.xml.bind.JAXBException
        *
        */

        // // MAIN CLASS
        private String conformanceLevel = "U";
        private String versionStr = "1.3.0";

        // BASIC, COMFORT etc - may be set from outside.
        private String ZUGFeRDConformanceLevel = null;

        /**
         * Data (XML invoice) to be added to the ZUGFeRD PDF. It may be externally
         * set, in which case passing a IZUGFeRDExportableTransaction is not
         * necessary. By default it is null meaning the caller needs to pass a
         * IZUGFeRDExportableTransaction for the XML to be populated.
         */
        byte[] zugferdData = null;
        private bool isTest;
        private bool ignoreA1Errors;
        private PDDocument doc;
        private String currency = "EUR";

        private BigDecimal nDigitFormat(BigDecimal value, int scale)
        {
            /*
             * I needed 123,45, locale independent.I tried
             * NumberFormat.getCurrencyInstance().format( 12345.6789 ); but that is
             * locale specific.I also tried DecimalFormat df = new DecimalFormat(
             * "0,00" ); df.setDecimalSeparatorAlwaysShown(true);
             * df.setGroupingUsed(false); DecimalFormatSymbols symbols = new
             * DecimalFormatSymbols(); symbols.setDecimalSeparator(',');
             * symbols.setGroupingSeparator(' ');
             * df.setDecimalFormatSymbols(symbols);
             * 
             * but that would not switch off grouping. Although I liked very much
             * the (incomplete) "BNF diagram" in
             * http://docs.oracle.com/javase/tutorial/i18n/format/decimalFormat.html
             * in the end I decided to calculate myself and take eur+sparator+cents
             * 
             * This function will cut off, i.e. floor() subcent values Tests:
             * System.err.println(utils.currencyFormat(new BigDecimal(0),
             * ".")+"\n"+utils.currencyFormat(new BigDecimal("-1.10"),
             * ",")+"\n"+utils.currencyFormat(new BigDecimal("-1.1"),
             * ",")+"\n"+utils.currencyFormat(new BigDecimal("-1.01"),
             * ",")+"\n"+utils.currencyFormat(new BigDecimal("20000123.3489"),
             * ",")+"\n"+utils.currencyFormat(new BigDecimal("20000123.3419"),
             * ",")+"\n"+utils.currencyFormat(new BigDecimal("12"), ","));
             * 
             * results 0.00 -1,10 -1,10 -1,01 20000123,34 20000123,34 12,00
             */
            value = value.setScale(scale, BigDecimal.ROUND_HALF_UP); // first, round
                                                                     // so that
                                                                     // e.g.
                                                                     // 1.189999999999999946709294817992486059665679931640625
                                                                     // becomes
                                                                     // 1.19
            char[] repeat = new char[scale];
            Arrays.fill(repeat, '0');

            DecimalFormatSymbols otherSymbols = new DecimalFormatSymbols();
            otherSymbols.setDecimalSeparator('.');
            DecimalFormat dec = new DecimalFormat("0." + new String(repeat),
                    otherSymbols);
            return new BigDecimal(dec.format(value));

        }

        private BigDecimal vatFormat(BigDecimal value)
        {
            return nDigitFormat(value, 2);
        }

        private BigDecimal currencyFormat(BigDecimal value)
        {
            return nDigitFormat(value, 2);
        }

        private BigDecimal priceFormat(BigDecimal value)
        {
            return nDigitFormat(value, 4);
        }

        private BigDecimal quantityFormat(BigDecimal value)
        {
            return nDigitFormat(value, 4);
        }

        /**
         * All files are PDF/A-3, setConformance refers to the level conformance.
         *
         * PDF/A-3 has three coformance levels, called "A", "U" and "B".
         *
         * PDF/A-3-B where B means only visually preservable, U -standard for
         * Mustang- means visually and unicode preservable and A means full
         * compliance, i.e. visually, unicode and structurally preservable and
         * tagged PDF, i.e. useful metainformation for blind people.
         *
         * Feel free to pass "A" as new level if you know what you are doing :-)
         *
         *
         */
        public void setConformanceLevel(String newLevel)
        {
            conformanceLevel = newLevel;
        }

        /**
         * enables the flag to indicate a test invoice in the XML structure
         *
         */
        public void setTest()
        {
            isTest = true;
        }

        public void IgnoreA1Errors()
        {
            ignoreA1Errors = true;
        }

        public void loadPDFA3(String filename)
        {

            try
            {
                doc = PDDocument.load(filename);

            }
            catch (IOException e)
            {
                // TODO Auto-generated catch block
                e.printStackTrace();
            }

        }

        public void loadPDFA3(InputStream file)
        {

            try
            {
                doc = PDDocument.load(file);

            }
            catch (IOException e)
            {
                // TODO Auto-generated catch block
                e.printStackTrace();
            }

        }

        /**
         * Makes A PDF/A3a-compliant document from a PDF-A1 compliant document (on
         * the metadata level, this will not e.g. convert graphics to JPG-2000)
         *
         */
        public PDDocumentCatalog PDFmakeA3compliant(String filename,
                String producer, String creator, bool attachZugferdHeaders)
        {
            loadPDFA3(filename);

            return makeDocPDFA3compliant(producer, creator, attachZugferdHeaders);
        }

        public PDDocumentCatalog PDFmakeA3compliant(InputStream file,
                String producer, String creator, bool attachZugferdHeaders)
        {
            /* cache the file content in memory, unfortunately the next step, isValidA1,
		        * will close the input stream but the step thereafter (loadPDFA3) needs
		        * and open one*/
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            byte[] buf = new byte[1024];
            int n = 0;
            while ((n = file.read(buf)) >= 0)
                baos.write(buf, 0, n);
            byte[] content = baos.toByteArray();

            InputStream is1 = new ByteArrayInputStream(content);
            InputStream is2 = new ByteArrayInputStream(content);

            loadPDFA3(is2);

            return makeDocPDFA3compliant(producer, creator, attachZugferdHeaders);
        }

        private PDDocumentCatalog makeDocPDFA3compliant(String producer,
                String creator, bool attachZugferdHeaders)
        {
            String fullProducer = producer + " (via mustangproject.org "
                    + versionStr + ")";

            PDDocumentCatalog cat = doc.getDocumentCatalog();
            PDMetadata metadata = new PDMetadata(doc);
            cat.setMetadata(metadata);
            // we're using the jempbox org.apache.jempbox.xmp.XMPMetadata version,
            // not the xmpbox one
            XMPMetadata xmp = new XMPMetadata();

            XMPSchemaPDFAId pdfaid = new XMPSchemaPDFAId(xmp);
            pdfaid.setAbout(""); //$NON-NLS-1$
            xmp.addSchema(pdfaid);

            XMPSchemaDublinCore dc = xmp.addDublinCoreSchema();
            dc.addCreator(creator);
            dc.setAbout(""); //$NON-NLS-1$

            XMPSchemaBasic xsb = xmp.addBasicSchema();
            xsb.setAbout(""); //$NON-NLS-1$

            xsb.setCreatorTool(creator);
            xsb.setCreateDate(GregorianCalendar.getInstance());
            // PDDocumentInformation pdi=doc.getDocumentInformation();
            PDDocumentInformation pdi = new PDDocumentInformation();
            pdi.setProducer(fullProducer);
            pdi.setAuthor(creator);
            doc.setDocumentInformation(pdi);

            XMPSchemaPDF pdf = xmp.addPDFSchema();
            pdf.setProducer(fullProducer);
            pdf.setAbout(""); //$NON-NLS-1$

            /*
		        * // Mandatory: PDF/A3-a is tagged PDF which has to be expressed using
		        * a // MarkInfo dictionary (PDF A/3 Standard sec. 6.7.2.2) PDMarkInfo
		        * markinfo = new PDMarkInfo(); markinfo.setMarked(true);
		        * doc.getDocumentCatalog().setMarkInfo(markinfo);
		        */
            /*
		        * 
		        * To be on the safe side, we use level B without Markinfo because we
		        * can not guarantee that the user correctly tagged the templates for
		        * the PDF.
		        */
            pdfaid.setConformance(conformanceLevel);//$NON-NLS-1$ //$NON-NLS-1$

            pdfaid.setPart(new java.lang.Integer(3));

            if (attachZugferdHeaders)
            {

                addZugferdXMP(xmp); /*
								        * this is the only line where we do something
								        * Zugferd-specific, i.e. add PDF metadata
								        * specifically for Zugferd, not generically for
								        * a embedded file
								        */

            }

            metadata.importXMPMetadata(xmp);
            return cat;
        }

        /**
        * Embeds the Zugferd XML structure in a file named ZUGFeRD-invoice.xml.
        *
        * @param doc
        *            PDDocument to attach an XML invoice to
        * @param trans
        *            a IZUGFeRDExportableTransaction that provides the data-model
        *            to populate the XML. This parameter may be null, if so the XML
        *            data should hav ebeen set via
        *            <code>setZUGFeRDXMLData(byte[] zugferdData)</code>
        */
        public void PDFattachZugferdFile(byte[] zugferdData)
        {
            if (zugferdData == null)
                return;

            if ((zugferdData[0] == (byte)0xEF) && (zugferdData[1] == (byte)0xBB) && (zugferdData[2] == (byte)0xBF))
            {
                // I don't like BOMs, lets remove it
                zugferdData = zugferdData.Skip(3).ToArray();
            }

            PDFAttachGenericFile(
                    doc,
                    "ZUGFeRD-invoice.xml",
                    "Alternative",
                    "Invoice metadata conforming to ZUGFeRD standard (http://www.ferd-net.de/front_content.php?idcat=231&lang=4)",
                    "text/xml", zugferdData);
        }

        public void export(String ZUGFeRDfilename)
        {
            try
            {
                doc.save(ZUGFeRDfilename);
            }
            catch (Exception e)
            {
                // TODO Auto-generated catch block
                Debug.Print(e.StackTrace);
            }
        }

        /**
         * Embeds an external file (generic - any type allowed) in the PDF.
         *
         * @param doc
         *            PDDocument to attach the file to.
         * @param filename
         *            name of the file that will become attachment name in the PDF
         * @param relationship
         *            how the file relates to the content, e.g. "Alternative"
         * @param description
         *            Human-readable description of the file content
         * @param subType
         *            type of the data e.g. could be "text/xml" - mime like
         * @param data
         *            the binary data of the file/attachment
         */
        public void PDFAttachGenericFile(PDDocument doc, String filename,
                String relationship, String description, String subType, byte[] data)
        {
            PDComplexFileSpecification fs = new PDComplexFileSpecification();
            fs.setFile(filename);

            COSDictionary dict = fs.getCOSDictionary();
            dict.setName("AFRelationship", relationship);
            dict.setString("UF", filename);
            dict.setString("Desc", description);

            ByteArrayInputStream fakeFile = new ByteArrayInputStream(data);
            PDEmbeddedFile ef = new PDEmbeddedFile(doc, fakeFile);
            ef.setSubtype(subType);
            ef.setSize(data.Length);
            ef.setCreationDate(new GregorianCalendar());

            ef.setModDate(GregorianCalendar.getInstance());

            fs.setEmbeddedFile(ef);

            // In addition make sure the embedded file is set under /UF
            dict = fs.getCOSDictionary();
            COSDictionary efDict = (COSDictionary)dict
                        .getDictionaryObject(COSName.EF);
            COSBase lowerLevelFile = efDict.getItem(COSName.F);
            efDict.setItem(COSName.UF, lowerLevelFile);

            // now add the entry to the embedded file tree and set in the document.
            PDDocumentNameDictionary names = new PDDocumentNameDictionary(
                    doc.getDocumentCatalog());
            PDEmbeddedFilesNameTreeNode efTree = names.getEmbeddedFiles();
            if (efTree == null)
            {
                efTree = new PDEmbeddedFilesNameTreeNode();
            }

            //Map<String, COSObjectable> namesMap = new HashMap<String, COSObjectable>();
            //      Map<String, COSObjectable> oldNamesMap = efTree.getNames();
            //if (oldNamesMap != null) {
            // for (String key : oldNamesMap.keySet()) {
            //  namesMap.put(key, oldNamesMap.get(key));
            // }
            //} 
            //efTree.setNames(namesMap);
            //should be ported more exactly...
            efTree.setNames(Collections.singletonMap(filename, fs));

            names.setEmbeddedFiles(efTree);
            doc.getDocumentCatalog().setNames(names);

            // AF entry (Array) in catalog with the FileSpec
            COSArray cosArray = (COSArray)doc.getDocumentCatalog()
                .getCOSDictionary().getItem("AF");
            if (cosArray == null)
            {
                cosArray = new COSArray();
            }
            cosArray.add(fs);
            COSDictionary dict2 = doc.getDocumentCatalog().getCOSDictionary();
            COSArray array = new COSArray();
            array.add(fs.getCOSDictionary()); // see below
            dict2.setItem("AF", array);
            doc.getDocumentCatalog().getCOSDictionary().setItem("AF", cosArray);
        }

        /**
        * Sets the ZUGFeRD conformance level (override).
        *
        * @param ZUGFeRDConformanceLevel
        *            the new conformance level
        */
        public void setZUGFeRDConformanceLevel(String ZUGFeRDConformanceLevel)
        {
            this.ZUGFeRDConformanceLevel = ZUGFeRDConformanceLevel;
        }

        /**
        * * This will add both the RDF-indication which embedded file is Zugferd
        * and the neccessary PDF/A schema extension description to be able to add
        * this information to RDF
        *
        * @param metadata
        */
        private void addZugferdXMP(XMPMetadata metadata)
        {

            XMPSchemaZUGFeRD zf = new XMPSchemaZUGFeRD(metadata,
                    this.ZUGFeRDConformanceLevel);
            zf.setAbout(""); //$NON-NLS-1$
            metadata.addSchema(zf);

            XMPSchemaPDFAExtensions pdfaex = new XMPSchemaPDFAExtensions(metadata);
            pdfaex.setAbout(""); //$NON-NLS-1$
            metadata.addSchema(pdfaex);

        }

        /****
        * Returns the PDFBox PDF Document
        * 
        * @return
        */
        public PDDocument getDoc()
        {
            return doc;
        }
    }
}
