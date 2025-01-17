﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FileDBReader.src
{
    public class XmlDocumentMarking
    {
        private List<XmlNode> unmarked;
        XmlDocument OriginalDocument;

        private XmlDocumentMarking()
        {

        }

        public void Mark(XmlNode n)
        {
            ThrowIfNotInDocument(n);
            unmarked.Remove(n);
        }

        public bool IsMarked(XmlNode n)
        {
            ThrowIfNotInDocument(n);
            return !unmarked.Contains(n);
        }

        public IEnumerable<XmlNode> GetUnmarkedTextNodes()
        {
            return unmarked.ToArray();
        }

        public void ThrowIfNotInDocument(XmlNode n)
        {
            if (!n.OwnerDocument.Equals(OriginalDocument))
                throw new InvalidOperationException("The node {n} is not part of the original document!");
        }

        public static XmlDocumentMarking InitFrom(XmlDocument doc)
        {
            XmlDocumentMarking marking = new XmlDocumentMarking();
            marking.OriginalDocument = doc;
            marking.unmarked = doc.SelectNodes("//*[text()]").Cast<XmlNode>().ToList();
            return marking;
        }


    }
}
