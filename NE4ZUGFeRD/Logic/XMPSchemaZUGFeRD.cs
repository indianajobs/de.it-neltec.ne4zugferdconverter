/**
 * Mustangproject's ZUGFeRD implementation
 * ZUGFeRD exporter helper class
 * Licensed under the APLv2 (ported to C# using IKVM)
 * @date 2014-05-10
 * @version 1.2.0s
 * @author jstaerk
 * */
using System;

using org.apache.jempbox.impl;
using org.apache.jempbox.xmp;

using org.w3c.dom;

namespace NE4ZUGFeRD.Logic
{
    public class XMPSchemaZUGFeRD : XMPSchemaBasic
    {
        private String conformanceLevel = "BASIC";

        /**
         * This is what needs to be added to the RDF metadata - basically the name of the embedded
         * Zugferd file
         * */
        public XMPSchemaZUGFeRD(org.apache.jempbox.xmp.XMPMetadata parent, String conformanceLevel = null) : base(parent)
        {
            if (conformanceLevel != null)
            {
                this.conformanceLevel = conformanceLevel;
            }

            schema.setAttributeNS(NS_NAMESPACE, "xmlns:zf", //$NON-NLS-1$
                    "urn:ferd:pdfa:CrossIndustryDocument:invoice:1p0#"); //$NON-NLS-1$
                                                                         // the superclass includes this two namespaces we don't need
            schema.removeAttributeNS(NS_NAMESPACE, "xapGImg"); //$NON-NLS-1$
            schema.removeAttributeNS(NS_NAMESPACE, "xmp"); //$NON-NLS-1$
            Element textNode = schema.getOwnerDocument().createElement(
                    "zf:DocumentType"); //$NON-NLS-1$
            XMLUtil.setStringValue(textNode, "INVOICE"); //$NON-NLS-1$
            schema.appendChild(textNode);

            textNode = schema.getOwnerDocument().createElement(
                    "zf:DocumentFileName"); //$NON-NLS-1$
            XMLUtil.setStringValue(textNode, "ZUGFeRD-invoice.xml"); //$NON-NLS-1$
            schema.appendChild(textNode);

            textNode = schema.getOwnerDocument().createElement("zf:Version"); //$NON-NLS-1$
            XMLUtil.setStringValue(textNode, "1.0"); //$NON-NLS-1$
            schema.appendChild(textNode);

            textNode = schema.getOwnerDocument().createElement(
                    "zf:ConformanceLevel"); //$NON-NLS-1$
            XMLUtil.setStringValue(textNode, this.conformanceLevel); //$NON-NLS-1$
            schema.appendChild(textNode);

        }

    }
}
