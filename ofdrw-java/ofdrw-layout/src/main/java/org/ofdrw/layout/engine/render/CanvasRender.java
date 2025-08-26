package org.ofdrw.layout.engine.render;

import org.ofdrw.core.basicStructure.pageObj.layer.block.CT_PageBlock;
import org.ofdrw.core.basicType.ST_Box;
import org.ofdrw.core.basicType.ST_Loc;
import org.ofdrw.layout.element.Div;
import org.ofdrw.layout.element.canvas.Canvas;
import org.ofdrw.layout.element.canvas.DrawContext;
import org.ofdrw.layout.element.canvas.Drawer;
import org.ofdrw.layout.engine.ResManager;

import java.io.IOException;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Canvas 渲染器
 * <p>
 * {@link org.ofdrw.layout.element.canvas.Canvas} 的渲染器
 *
 * @author 权观宇
 * @since 2020-05-01 17:44:41
 */
public class CanvasRender implements Processor {


    /**
     * 执行Canvas 渲染器
     *
     * @param layer      将要渲染到的图层
     * @param resManager 资源管理器
     * @param canvas     canvas对象
     * @param maxUnitID  最大ID
     * @throws RenderException 渲染发生错误
     */
    public static void render(CT_PageBlock layer,
                              ResManager resManager,
                              Canvas canvas,
                              AtomicInteger maxUnitID) throws RenderException {
        // 获取绘制器
        Drawer drawer = canvas.getDrawer();
        if (drawer == null) {
            return;
        }
        // 尝试获取Canvas中的页块
        CT_PageBlock block = canvas.getPreferBlock();
        if (block == null) {
            // 若页块为空，则创建新的页块
            block = new CT_PageBlock();
            block.setObjID(maxUnitID.incrementAndGet());
            canvas.setPreferBlock(block);
            // 添加到图层中
            layer.addPageBlock(block);
        }


        // 根据盒式模型计算出加上 外边距、边框、内边距
        ST_Box boundary = new ST_Box(
                canvas.getX() + canvas.getMarginLeft() + canvas.getBorderLeft() + canvas.getPaddingLeft(),
                canvas.getY() + canvas.getMarginTop() + canvas.getBorderTop() + canvas.getPaddingTop(),
                canvas.getWidth(),
                canvas.getHeight());
        // 构建上下文
        try (DrawContext ctx = new DrawContext(block, boundary, maxUnitID, resManager)) {
            // 执行绘制工作
            drawer.draw(ctx);
        } catch (IOException e) {
            throw new RenderException("Canvas绘制过程中发生异常", e);
        }
    }

    /**
     * 渲染Canvas对象
     *
     * @param pageLoc    页面在虚拟容器中绝对路径。
     * @param layer      图片将要放置的图层
     * @param resManager 资源管理器
     * @param e          Canvas对象
     * @param maxUnitID  最大元素ID提供器
     * @throws RenderException 渲染发生错误
     */
    @Override
    public void render(ST_Loc pageLoc, CT_PageBlock layer, ResManager resManager, Div e, AtomicInteger maxUnitID)throws RenderException {
        if (e instanceof Canvas) {
            render(layer, resManager, (Canvas) e, maxUnitID);
        }
    }
}

