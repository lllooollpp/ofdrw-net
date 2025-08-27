using OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;
using OfdrwNet.Layout.Element;

namespace OfdrwNet.Layout.Element.Canvas
{
    /// <summary>
    /// 画板
    /// <para>
    /// 用于更加自由的向页面中加入内容
    /// </para>
    /// <para>
    /// 绘制行为详见渲染器：CanvasRender
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-05-01 11:04:46
    /// </summary>
    public class Canvas : Div
    {
        /// <summary>
        /// 绘制器
        /// </summary>
        private IDrawer? drawer;

        /// <summary>
        /// 优先使用的页面块
        /// <para>
        /// Canvas 生成的所有图元都将存储与该区块中
        /// </para>
        /// <para>
        /// 默认为：null，表示不指定，每次绘制都会创建新的页面块。
        /// </para>
        /// </summary>
        private CtPageBlock? preferBlock;

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private Canvas() : base()
        {
        }

        /// <summary>
        /// 创建Canvas对象，并指定绘制器
        /// <para>
        /// Canvas的宽度和高度必须在创建时指定
        /// </para>
        /// </summary>
        /// <param name="width">宽度（单位：毫米mm）</param>
        /// <param name="height">高度（单位：毫米mm）</param>
        /// <param name="drawer">Canvas内容的绘制器</param>
        public Canvas(double width, double height, IDrawer drawer) : base(width, height)
        {
            this.drawer = drawer;
            preferBlock = null;
        }

        /// <summary>
        /// 创建Canvas对象
        /// <para>
        /// Canvas的宽度和高度必须在创建时指定
        /// </para>
        /// </summary>
        /// <param name="width">宽度（单位：毫米mm）</param>
        /// <param name="height">高度（单位：毫米mm）</param>
        public Canvas(double width, double height) : base(width, height)
        {
        }

        /// <summary>
        /// 在指定位置 创建Canvas对象
        /// </summary>
        /// <param name="x">画布左上角的x坐标</param>
        /// <param name="y">画布左上角的y坐标</param>
        /// <param name="w">画布的宽度</param>
        /// <param name="h">画布的高度</param>
        public Canvas(double x, double y, double w, double h) : base(x, y, w, h)
        {
        }

        /// <summary>
        /// 创建Canvas对象
        /// </summary>
        /// <param name="style">页面样式属性</param>
        public Canvas(PageLayout style) : this(style.GetWidth(), style.GetHeight())
        {
        }

        /// <summary>
        /// 获取绘制器
        /// </summary>
        /// <returns>绘制器</returns>
        public IDrawer? GetDrawer()
        {
            return drawer;
        }

        /// <summary>
        /// 在进入渲染器之前可以对Canvas的绘制进行重设
        /// </summary>
        /// <param name="drawer">新的绘制器</param>
        /// <returns>this</returns>
        public virtual Canvas SetDrawer(IDrawer drawer)
        {
            this.drawer = drawer;
            return this;
        }

        /// <summary>
        /// Canvas 不接受宽度重设
        /// </summary>
        public override Rectangle DoPrepare(double widthLimit)
        {
            var w = GetWidth() + WidthPlus();
            var h = GetHeight() + HeightPlus();
            return new Rectangle(w, h);
        }

        /// <summary>
        /// 获取 优先使用的页面块
        /// <para>
        /// Canvas 生成的所有图元都将存储与该区块中，通常在渲染完成后可以获得非空的页面块。
        /// </para>
        /// </summary>
        /// <returns>优先使用的页面块，null。</returns>
        public CtPageBlock? GetPreferBlock()
        {
            return preferBlock;
        }

        /// <summary>
        /// 设置 优先使用的页面块
        /// <para>
        /// Canvas 生成的所有图元都将存储与该区块中
        /// </para>
        /// <para>
        /// 注意：该方法具有一定危险性，若您不清楚该方法的作用，请勿使用。
        /// </para>
        /// </summary>
        /// <param name="preferBlock">页面块</param>
        public void SetPreferBlock(CtPageBlock preferBlock)
        {
            this.preferBlock = preferBlock;
        }

        /// <summary>
        /// 获取元素类型
        /// <para>
        /// 关联绘制器：CanvasRender
        /// </para>
        /// </summary>
        /// <returns>Canvas</returns>
        public override string ElementType()
        {
            return "Canvas";
        }
    }
}