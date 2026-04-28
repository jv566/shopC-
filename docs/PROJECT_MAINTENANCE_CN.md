# 商城桌面项目说明（长期维护版）

本文档用于团队长期协作，目标是让项目在功能持续迭代时仍然可维护、可测试、可交接。

## 1. 项目目标与边界

- 客户端：`WPF (.NET 8)` 桌面应用。
- 服务端：`ASP.NET Core (.NET 8)` API。
- 领域核心：围绕商品、户型、3D 展示、经销商自营业务。
- 当前阶段：已完成第一批页面占位布局，下一步进行页面切换骨架和交互细化。

## 2. 当前里程碑（2026-03-24）

已完成内容：

- 解决方案分层结构已搭建完成（Desktop/Domain/Application/Infrastructure/Contracts/Backend）。
- `MainWindow` 已改为宿主壳，仅保留全屏 `ContentControl`，默认加载首页。
- 7 个页面已按产品设计图完成“相框/占位布局”（仅界面，不含业务逻辑）。
- 当前桌面项目可正常编译通过。

对应页面文件：

- `HomePageView`：主界面
- `ProductDetailView`：产品详情页
- `ProductListView`：产品列表页
- `FloorPlanDesignView`：户型板块设计页
- `ProductPanoramaReplacementView`：更换产品全景界面
- `Product3DShowcaseView`：3D 产品展示页
- `DealerSelfOperatedView`：经销商自营页

## 3. 代码结构总览

```text
src/
  Shop.Desktop/        # 桌面端 UI
  Shop.Domain/         # 领域模型与业务规则
  Shop.Application/    # 用例层与抽象接口
  Shop.Infrastructure/ # 数据访问与外部系统实现
  Shop.Contracts/      # DTO/契约
  Shop.Backend/        # Web API
tests/
  Shop.Domain.Tests/
  Shop.Application.Tests/
docs/
  PROJECT_GUIDE_CN.md
  PROJECT_STRUCTURE.md
  PROJECT_MAINTENANCE_CN.md
```

## 4. 分层职责与依赖规则

- `Domain`：只存业务规则，不依赖 UI/数据库/网络。
- `Application`：编排业务用例，依赖 Domain。
- `Infrastructure`：实现数据库与外部依赖，依赖 Application/Domain。
- `Desktop`：界面与交互，调用 Application。
- `Backend`：API 接口层，调用 Application。
- `Contracts`：跨层通信对象。

依赖方向必须保持：

```text
Desktop -> Application -> Domain
Backend -> Application -> Domain
Infrastructure -> Application + Domain
```

## 5. UI 页面规划（第一批）

已完成占位布局页面（`UserControl`）：

- `HomePageView`：主界面
- `ProductDetailView`：产品详情页
- `ProductListView`：产品列表页
- `FloorPlanDesignView`：户型板块设计页
- `ProductPanoramaReplacementView`：更换产品全景界面
- `Product3DShowcaseView`：3D 产品展示页
- `DealerSelfOperatedView`：经销商自营页

补充说明：

- 当前页面均为静态占位稿，用于还原设计结构与空间关系。
- 暂未接入导航、命令、数据绑定和后端接口。

建议后续为每个页面补充对应 `ViewModel` 与 `Service`。

## 6. 命名与目录约定

- `Views/Pages/*View.xaml`：页面视图。
- `ViewModels/*ViewModel.cs`：页面状态与命令。
- `Application/<Module>/Commands|Queries`：应用层用例。
- `Domain/Entities|ValueObjects|Enums`：业务核心。
- `Contracts/<Module>`：请求/响应 DTO。

命名建议：

- 类型名使用 PascalCase。
- 异步方法后缀 `Async`。
- DTO 以 `Dto` 结尾。

## 7. 迭代流程（每个功能都按这套）

1. 在 `docs` 补需求与验收条件。
2. 在 `Domain` 先定义规则。
3. 在 `Application` 定义用例和接口。
4. 在 `Infrastructure` 实现数据访问。
5. 在 `Backend/Desktop` 接入。
6. 在 `tests` 增加关键测试。
7. 最后更新文档与变更记录。

## 8. 测试与质量门槛

每次提交至少保证：

- `dotnet build shop.sln` 通过。
- `dotnet test shop.sln` 通过。
- 新增业务规则必须有测试。
- 复杂页面逻辑优先写 ViewModel 单测。

## 9. 文档维护机制

建议长期保留以下文档并同步更新：

- `PROJECT_GUIDE_CN.md`：新同学入门。
- `PROJECT_STRUCTURE.md`：结构总览。
- `PROJECT_MAINTENANCE_CN.md`：协作规范与维护策略（本文）。
- `CHANGELOG.md`（建议后续新增）：记录每次版本变化。

文档更新触发条件：

- 新增模块或页面。
- 调整目录结构。
- 变更依赖方向或技术栈。
- 发布版本。

## 10. 版本与分支建议

- `main`：稳定可发布。
- `develop`：日常集成。
- `feature/*`：功能分支。
- `hotfix/*`：线上修复。

提交建议：

- 单次提交只做一件事。
- 提交信息包含模块名与动作，例如：
  - `feat(desktop): scaffold ProductListView`
  - `docs: update maintenance guide`

## 11. 风险与防退化策略

- 禁止在 ViewModel 直接写 SQL。
- 禁止 Domain 依赖 UI 或 HTTP 框架。
- 禁止跳过测试直接合并主干。
- 对关键模块建立最小回归清单（商品、详情、3D、经销商）。

## 12. 下一阶段推荐执行顺序

1. 建立临时页面切换菜单（用于快速预览 7 个页面）。
2. 抽取页面级 ViewModel（先空实现，统一命名与目录）。
3. 统一页面尺寸、字号和边框样式（形成基础视觉规范）。
4. 打通“产品列表 -> 产品详情 -> 3D 展示”最小交互链路。
5. 接入应用层用例与后端接口（先用 InMemory，再替换数据库）。
