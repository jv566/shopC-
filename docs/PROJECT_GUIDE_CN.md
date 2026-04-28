# 商城桌面应用项目说明（新手版）

本文档面向“第一次做桌面应用”的开发者，解释当前 `.NET 8 + WPF` 项目的分层结构与开发方式。

## 1. 你现在看到的结构是什么

```text
shop/
  src/
    Shop.Desktop/        # WPF 桌面端（界面 + 交互）
    Shop.Domain/         # 业务核心规则（最稳定）
    Shop.Application/    # 用例层（把业务动作组织起来）
    Shop.Infrastructure/ # 数据访问与外部实现（数据库/API/缓存等）
    Shop.Contracts/      # 跨边界的数据结构（DTO）
    Shop.Backend/        # 后端 Web API（给桌面端/其他端调用）
  tests/
    Shop.Domain.Tests/
    Shop.Application.Tests/
  docs/
    PROJECT_STRUCTURE.md
    PROJECT_GUIDE_CN.md
    PROJECT_MAINTENANCE_CN.md
```

你可以把它理解成“6个小项目协作”，而不是“1个大项目全塞一起”。

## 2. 当前进度（你现在所处阶段）

- 已完成分层项目结构搭建。
- `MainWindow` 已作为宿主壳，只放全屏内容区。
- 7 个核心页面都已完成设计图占位布局（无逻辑）。

这意味着：你现在可以专注做“页面切换 + 局部交互 + 数据绑定”，而不用再反复改目录结构。

## 3. 每一层做什么（重点）

### `Shop.Domain`

- 放“业务真规则”。
- 例如：`Money` 不能是负数、订单状态如何流转。
- 这一层**不依赖**数据库、网络、UI。

### `Shop.Application`

- 放“用例”。
- 例如：创建商品、查询商品列表、提交订单。
- 它调用 Domain 的规则，并声明需要哪些接口（如仓储接口）。

### `Shop.Infrastructure`

- 放“具体实现”。
- 例如：用 EF Core 读数据库、调第三方支付接口。
- 现在先用 `InMemoryProductRepository` 作为过渡。

### `Shop.Contracts`

- 放 DTO（数据传输对象）。
- 用于 Desktop 和 Backend 之间传数据，避免直接暴露 Domain 对象。

### `Shop.Backend`

- 后端 API 主机（ASP.NET Core）。
- 提供 HTTP 接口，未来可被桌面端调用。

### `Shop.Desktop`

- 你的桌面 UI（WPF）。
- 负责页面、交互、ViewModel，不直接写复杂业务规则。

## 4. 依赖方向（最重要的纪律）

推荐长期保持：

```text
Desktop   -> Application -> Domain
Backend   -> Application -> Domain
Infrastructure -> Application + Domain
Contracts -> (被 Application / Backend / Desktop 使用)
Domain    -> (不依赖任何业务层)
```

一句话：**越核心的层，越不应该依赖外层。**

## 5. 已落地页面（占位稿）

- `HomePageView`：主界面
- `ProductDetailView`：产品详情页
- `ProductListView`：产品列表页
- `FloorPlanDesignView`：户型板块设计页
- `ProductPanoramaReplacementView`：更换产品全景界面
- `Product3DShowcaseView`：3D 产品展示页
- `DealerSelfOperatedView`：经销商自营页

## 6. 下一步你最应该做什么

对当前项目最稳妥的顺序：

1. 做一个临时页面切换菜单，能在 7 页之间快速预览。
2. 给每个页面建立对应 `ViewModel` 空类。
3. 把按钮点击、筛选条件等交互从 code-behind 挪到 ViewModel。
4. 再开始接应用层和后端接口。

## 7. 常见误区（提前避坑）

- 把 SQL 直接写在 ViewModel 里（不建议）。
- 把 DTO 当实体到处传（会让规则失控）。
- Domain 里引用 UI/HTTP 相关库（会让核心层被污染）。
- 还没跑通最小流程就急着做复杂框架（容易卡住）。

## 8. 快速运行命令

在仓库根目录执行：

```powershell
dotnet build shop.sln
dotnet test shop.sln
```

如果要启动后端：

```powershell
dotnet run --project src/Shop.Backend/Shop.Backend.csproj
```

## 9. 关于 MainWindow

你现在的 `MainWindow` 是“宿主壳”，不是固定头部/侧边栏模板。

- 它只负责承载当前页面（全屏内容区）。
- 页面本身是 `Views/Pages` 下的 `UserControl`。
- 这样即使设计图风格不统一，也能保持结构清晰。
