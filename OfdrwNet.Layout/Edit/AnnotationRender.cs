using OfdrwNet.Packaging.Container;
using OfdrwNet.Layout.Engine;
using System.Xml.Linq;

namespace OfdrwNet.Layout.Edit
{
    public class AnnotationRender
    {
        private readonly DocDir docDir;
        private readonly ResManager resManager;
        private readonly Func<int> idProvider;

        public AnnotationRender(DocDir docDir, ResManager resManager, Func<int> idProvider)
        {
            this.docDir = docDir;
            this.resManager = resManager;
            this.idProvider = idProvider;
        }

        public void Render(OfdrwNet.Reader.PageInfo pageInfo, object annotation)
        {
            // minimal stub: add annotation to Annotations.xml
            var ann = new XElement("Annotation");
            var anns = new OfdrwNet.Core.Annotation.Annotations();
            anns.AddPage(new OfdrwNet.Core.Annotation.PageAnnot.AnnPage().SetPageID(pageInfo.Id));
            docDir.SetAnnotations(anns);
        }
    }
}
