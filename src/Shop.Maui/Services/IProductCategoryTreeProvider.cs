using Shop.Maui.Models; // 引入 ProductCategoryGroup 模型

namespace Shop.Maui.Services;

// 商品分类树提供者接口
// interface 表示“规范/合同”
// 它只规定你必须有什么功能，不写具体怎么实现
public interface IProductCategoryTreeProvider
{
    // 获取商品分类树
    // 返回值：
    // Task 表示这是异步方法，需要 await
    // IReadOnlyList<ProductCategoryGroup> 表示返回一个只读分类组列表
    Task<IReadOnlyList<ProductCategoryGroup>> GetCategoryTreeAsync(
        CancellationToken cancellationToken = default);
    //cancellationToken这个参数是用来“取消任务”的。

    //比如请求后端接口时，页面关闭了，就可以取消请求
}