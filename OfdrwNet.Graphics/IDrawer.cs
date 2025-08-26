namespace OfdrwNet.Graphics;

/// <summary>
/// Canvas 内容绘制器接口
/// 对应 Java 版本的 org.ofdrw.layout.element.canvas.Drawer
/// 用于绘制Canvas中的实际内容
/// </summary>
public interface IDrawer
{
    /// <summary>
    /// 绘制方法
    /// </summary>
    /// <param name="context">绘制上下文</param>
    /// <returns>绘制任务</returns>
    Task DrawAsync(DrawContext context);
}

/// <summary>
/// 同步绘制器接口
/// </summary>
public interface ISyncDrawer
{
    /// <summary>
    /// 同步绘制方法
    /// </summary>
    /// <param name="context">绘制上下文</param>
    void Draw(DrawContext context);
}

/// <summary>
/// 绘制器的基础实现
/// </summary>
public abstract class DrawerBase : IDrawer, ISyncDrawer
{
    /// <summary>
    /// 异步绘制方法的默认实现
    /// </summary>
    /// <param name="context">绘制上下文</param>
    /// <returns>绘制任务</returns>
    public virtual async Task DrawAsync(DrawContext context)
    {
        await Task.Run(() => Draw(context));
    }

    /// <summary>
    /// 同步绘制方法，需要子类实现
    /// </summary>
    /// <param name="context">绘制上下文</param>
    public abstract void Draw(DrawContext context);
}