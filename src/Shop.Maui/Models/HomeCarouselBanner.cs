// 1. 命名空间：相当于给这个类建一个专属文件夹，防止重名
namespace Shop.Maui.Models;

// 2. 声明一个类：密封类（不能被继承），名字叫 HomeCarouselBanner
// 翻译：首页轮播横幅（就是APP首页滑动的轮播图）
public sealed class HomeCarouselBanner
{
    // 3. 只读属性：只能赋值一次，后续不能修改
    // 轮播图唯一编号（比如 1、2、3，用来区分不同轮播图）
    public string Id { get; }
    
    // 轮播图标题（比如 "夏季大促销"）
    public string Title { get; }
    
    // 轮播图描述（比如 "全场8折，限时3天"）
    // ? 表示可以为空（没有描述也可以）
    public string? Description { get; }
    
    // 轮播图图片路径（比如 "banner1.png" 或网络图片地址）
    // ? 表示可以为空（没有图片也可以）
    public string? ImagePath { get; }

    // 4. 构造函数：创建轮播图对象时，必须传的参数
    // 作用：给上面的 Id/Title/Description/ImagePath 赋值
    public HomeCarouselBanner(string id, string title, string? imagePath, string? description)
    {
        // 把传进来的参数 赋值给 类的属性
        Id = id;          // 编号赋值
        Title = title;    // 标题赋值
        Description = description; // 描述赋值
        ImagePath = imagePath;     // 图片地址赋值
    }
}