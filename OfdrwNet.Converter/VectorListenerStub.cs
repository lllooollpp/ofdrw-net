using System.Collections.Generic;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;

namespace OfdrwNet.Converter
{
    // 空的矢量监听器桩，用于编译与 TDD 引用占位
    public class VectorListenerStub : IEventListener
    {
        public void EventOccurred(IEventData data, EventType type)
        {
            // TODO: 实现路径解析并生成 OfdWriter 调用
        }

        public ICollection<EventType> GetSupportedEvents()
        {
            return new List<EventType> { EventType.RENDER_PATH };
        }
    }
}
