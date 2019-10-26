using org.apache.pdfbox.pdmodel;
using s2industries.ZUGFeRD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NE4ZUGFeRD.Logic
{
    class NE4ZUGFeRDConverter
    {
        public Exception CreateSampleZugferdXML(out byte[] zugferdData)
        {
            try
            {
                InvoiceDescriptor desc = InvoiceDescriptor.CreateInvoice("36643", new DateTime(2019, 06, 25), CurrencyCodes.EUR, "M20170012-36643");

                desc.Profile = Profile.Basic;
                desc.ReferenceOrderNo = "001";
                desc.AddNote("Rechnung gemäß Kontakt-Nr. 20170012");
                desc.AddNote("Es bestehen Rabatt- und Bonusvereinbarungen.", SubjectCodes.Unknown);
                desc.SetBuyer("Staatliches Hofbräuhaus in München", "81808", "München", "Postfach 82 08 49", CountryCodes.DE, "88");
                desc.AddBuyerTaxRegistration("XXX", TaxRegistrationSchemeID.VA);
                // desc.SetBuyerContact("Hans Muster");
                desc.SetSeller("Schwabenmalz GmbH", "80333", "München", "Lieferantenstraße 20", CountryCodes.DE, "88");
                desc.AddSellerTaxRegistration("201/XXX/XXXXX", TaxRegistrationSchemeID.FC);
                desc.AddSellerTaxRegistration("DE212171817", TaxRegistrationSchemeID.VA);
                desc.SetBuyerOrderReferenceDocument("M20170012001", new DateTime(2017, 02, 01));
                desc.SetDeliveryNoteReferenceDocument("M20170012001", new DateTime(2017, 02, 09));
                desc.ActualDeliveryDate = new DateTime(2013, 6, 3);
                // Netto, Frachtkosten, Abschläge, Nettoges, VAT, Bruttoges, Vorschuss, Gesamt
                // Netto, - Fracht, - Abschläge, = Nettoges, VAT, Nettoges + VAT, - Voschuss, = Gesamt
                desc.SetTotals(100m, 0m, 0m, 100m, 19.00m, 119.00m, 50.0m, 69m);
                //desc.AddApplicableTradeTax(129.37m, 7m, TaxTypes.VAT, TaxCategoryCodes.S);
                //desc.AddApplicableTradeTax(64.46m, 19m, TaxTypes.VAT, TaxCategoryCodes.S);
                //desc.AddLogisticsServiceCharge(5.80m, "Versandkosten", TaxTypes.VAT, TaxCategoryCodes.S, 7m);
                desc.SetTradePaymentTerms("Die Zahlung erfolgt innerhalb von 20 Tagen", new DateTime(2019, 06, 25));

                //desc.addTradeLineCommentItem("Test");
                desc.addTradeLineItem("Pilsner Malz", "", QuantityCodes.TNE, 1m, 100.00m, 84.03m, 1m, TaxTypes.VAT, TaxCategoryCodes.S, 19m, "", null, "", "", "", null, "", null);
                //desc.addTradeLineItem("Pilsner Malz 2", "", QuantityCodes.TNE, 6m, 10.74m, 9.03m, 6m, TaxTypes.VAT, TaxCategoryCodes.S, 19m, "", null, "", "", "", null, "", null);

                String pathZugferdFile = Path.Combine(Path.GetTempPath(), "zugferd.xml");
                desc.Save(pathZugferdFile);

                zugferdData = File.ReadAllBytes(pathZugferdFile);

                try { File.Delete(pathZugferdFile); }
                catch (Exception) { };

                //return ok
                return null;
            }
            catch (Exception ex)
            {
                zugferdData = null;
                return ex;
            }
        }

        public Exception AttachZUGFeRD(string filepath, byte[] zugferdData)
        {
            try
            {
                NE4ZUGFeRDExporter ze = new NE4ZUGFeRDExporter();
                ze.PDFmakeA3compliant(filepath, "IT-neltec", Environment.UserName, true);
                ze.PDFattachZugferdFile(zugferdData);
                ze.export(filepath);

                //return ok
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public Exception ShowZUGFeRD(string filepath, out string message)
        {
            try
            {
                PDDocument doc = PDDocument.load(filepath);

                // now check the contents (like MustangReaderTest)
                NE4ZUGFeRDImporter zi = new NE4ZUGFeRDImporter();
                zi.extract(filepath);

                // ZUGFeRD lesen
                if (zi.canParse())
                {
                    zi.parse();

                    // ZUGFeRD Daten als string zurück
                    message = string.Format("Menge: {0}\nRechnungsempfänger: {1}\nReferenz: {2}",
                            zi.getAmount(), zi.getHolder(), zi.getForeignReference());
                }
                else
                {
                    message = "Keine ZUGFeRD Daten gefunden!";
                }

                //return ok
                return null;
            }
            catch (Exception ex)
            {
                message = ex.InnerException.ToString();
                return ex;
            }
        }
    }
}
