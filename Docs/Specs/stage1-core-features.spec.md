# BackpackSystem 阶段一技术规范:核心功能实现

**版本**:v1.0
**对应阶段**:阶段一(共三阶段)
**目标读者**:负责实现的开发者或 AI code agent

---

## 1. 背景与目标

### 1.1 项目背景

本项目是一个 Unity 背包系统模块,目标是沉淀为可在多个项目中复用的 UPM 包。整个项目分三个阶段:

- **阶段一(本规范):** 实现核心功能(列表展示、分类筛选、点击反馈),建立可扩展的接口骨架
- **阶段二:** 实现进阶功能(分帧加载、详情弹窗、底部按钮栏占位)
- **阶段三:** 完成 UPM 包化,实现 drop-in 复用

### 1.2 阶段一目标

完成以下闭环:

1. 在 Unity 编辑器 Play 模式下打开 Demo 场景
2. 通过调试 HUD 添加任意类型/数量的测试道具到背包
3. 背包 UI 正确显示道具图标和数量
4. 点击分类页签,列表切换为对应分类
5. 点击道具,Console 打印该道具的 instanceId、typeId、name
6. 通过 HUD 的"清空"按钮清空背包

### 1.3 不在本阶段范围

- 详情弹窗(阶段二)
- 分帧加载、对象池、无限滚动(阶段二/未来)
- 底部按钮栏(阶段二)
- 整理排序按钮(阶段二可选)
- UPM 元数据(package.json 等,阶段三)
- 单元测试(阶段三可选)
- 持久化、存档(未来)
- RemoveItem 接口(未来需要时再加)

---

## 2. 技术约束

### 2.1 技术栈

- **引擎**:Unity(版本不限,建议 2021 LTS 及以上)
- **语言**:C#
- **UI 系统**:UGUI(uGUI / Image / Button / ScrollRect / InputField / Dropdown)
- **资源加载**:`Resources.Load`(仅 Sample 层使用)

### 2.2 实现者能力假设

- 熟悉 C# 基础和 .NET event/Action 机制
- 熟悉 Unity MonoBehaviour、Prefab、ScriptableObject 基本用法
- 熟悉 UGUI 基础组件
- **不假设**熟悉:对象池模式、协程进阶用法、Addressables、第三方事件总线

### 2.3 禁用的库/模式

- **禁止**引入第三方依赖(UniTask、Zenject、Odin Inspector 等)
- **禁止**使用 async/await(阶段一所有操作均为同步)
- **禁止**在 Runtime 层引用 Samples 层任何类
- **禁止**在 Runtime 层**直接**使用 `Resources.Load`(必须通过 IIconLoader 接口)

### 2.4 命名空间

- Runtime 层:`BackpackSystem`
- Sample 层:`BackpackSystem.Samples`

---

## 3. 模块设计

### 3.1 模块清单

| 模块 | 归属 | 类型 | 职责一句话 |
|---|---|---|---|
| `ItemCategory` | Runtime/Data | enum | 道具分类枚举 |
| `ItemTypeData` | Runtime/Data | class (POCO) | 道具类型静态数据 |
| `ItemData` | Runtime/Data | class (POCO) | 道具实例运行时数据 |
| `IItemTypeProvider` | Runtime/Data | interface | 类型表数据源契约 |
| `IInstanceIdGenerator` | Runtime/Data | interface | 实例 id 生成策略 |
| `IIconLoader` | Runtime/Loading | interface | 图标加载契约 |
| `Backpack` | Runtime/Core | class | 背包核心逻辑 |
| `SimpleIncrementalIdGenerator` | Runtime/Core | class | 默认 id 生成器实现 |
| `BackpackPanelView` | Runtime/View | MonoBehaviour | 主面板装配 |
| `ItemCellView` | Runtime/View | MonoBehaviour | 单个道具格子 |
| `CategoryTabView` | Runtime/View | MonoBehaviour | 分类页签 |
| `ItemTypeDatabase` | Samples | ScriptableObject | 类型表的 SO 容器 |
| `ScriptableObjectItemTypeProvider` | Samples | class | 从 SO 读取的 provider 实现 |
| `ResourcesIconLoader` | Samples | class | Resources.Load 的 loader 实现 |
| `DebugBackpackHUD` | Samples | MonoBehaviour | 调试 HUD(添加/清空按钮) |
| `SampleBootstrap` | Samples | MonoBehaviour | Demo 场景启动装配脚本 |

### 3.2 模块依赖关系

```
[Samples 层]
SampleBootstrap ──创建并装配──┐
                              │
DebugBackpackHUD ◄────调用 Backpack──────┤
                              ▼
ScriptableObjectItemTypeProvider ──实现──> IItemTypeProvider
ResourcesIconLoader ──实现──> IIconLoader
                              │
                              ▼
[Runtime 层]
Backpack (持有 IItemTypeProvider, IInstanceIdGenerator)
  ▲
  │ 订阅事件、调用方法
  │
BackpackPanelView ◄── ItemCellView (子)
              ◄── CategoryTabView (子)
              ◄── 持有 IIconLoader

依赖原则:
- Runtime 不依赖 Samples
- Core 不依赖 View(通过事件解耦)
- View 通过接口依赖外部能力(IIconLoader)
- Samples 实现接口、装配 Runtime
```

---

## 4. 数据流

### 4.1 场景 A:Demo 启动

```
Unity 加载 DemoScene
  → SampleBootstrap.Awake() 触发
    1. new ScriptableObjectItemTypeProvider("BackpackSystem/SampleItemTypeDatabase")
       (内部用 Resources.Load 加载 ItemTypeDatabase 资源)
    2. new ResourcesIconLoader()
    3. new SimpleIncrementalIdGenerator()
    4. new Backpack(typeProvider, idGenerator)
    5. 订阅 backpack.OnItemClicked → Debug.Log
    6. 找到场景中的 BackpackPanelView,调用 panel.Init(backpack, typeProvider, iconLoader)
       BackpackPanelView 内部:
         - 订阅 backpack.OnContentsChanged / OnCategoryChanged
         - 为每个 ItemCategory 创建 CategoryTabView,设置默认选中"All"
         - 触发首次 RenderItems(空列表)
    7. 找到场景中的 DebugBackpackHUD,调用 hud.Init(backpack, typeProvider)
       DebugBackpackHUD 内部:
         - 用 typeProvider.GetAllTypes() 填充 dropdown
         - 注册添加/清空按钮事件
```

### 4.2 场景 B:用户通过 HUD 添加道具

```
[用户操作]
  在 dropdown 选"大血瓶",在 input 输入"60",点"添加"

[DebugBackpackHUD]
  读取 dropdown.value → typeId(从 dropdown 数据反查)
  读取 input.text → 解析为 int count(失败时打警告 return)
  调用 backpack.AddItem(typeId, 60)

[Backpack.AddItem]
  1. 校验 count > 0,typeId 在 typeProvider 中存在
  2. 查 typeProvider.GetTypeById(typeId) 拿到 maxStackCount
  3. 堆叠合并算法:
     a. 扫描现有 items,找到 typeId 相同且 count < maxStackCount 的格子
     b. 优先填满已有未满格子
     c. 还有剩余,新建格子(每格最多 maxStackCount,instanceId 由 idGenerator 生成)
  4. 触发 OnContentsChanged

[BackpackPanelView 收到事件]
  调用 backpack.GetItemsInCurrentCategory() 拿到当前分类的可见列表
  调用 RenderItems(items):
    - 销毁所有现有 ItemCellView 子节点
    - 为每个 ItemData 实例化 ItemCellView prefab
    - 调用 cell.SetData(item, typeProvider, iconLoader)
    - 注册 cell.OnClicked 回调(转发给 Backpack.NotifyItemClicked)
```

### 4.3 场景 C:用户切换分类

```
[用户操作]
  点击"装备"页签

[CategoryTabView]
  Button 点击触发 → OnSelected(ItemCategory.Equipment)

[BackpackPanelView 监听到]
  调用 backpack.SetCategory(Equipment)
    Backpack 内部更新 currentCategory
    触发 OnCategoryChanged(Equipment)

  在 OnCategoryChanged 回调中:
    - 更新所有 CategoryTabView 的选中态(只有 Equipment 选中)
    - 调用 RenderItems(backpack.GetItemsInCurrentCategory())
```

### 4.4 场景 D:用户点击道具

```
[用户操作]
  点击某个道具格子

[ItemCellView]
  Button 点击触发 → OnClicked(itemData)

[BackpackPanelView 监听到]
  调用 backpack.NotifyItemClicked(itemData.InstanceId)

[Backpack.NotifyItemClicked]
  校验 instanceId 在背包中存在(否则抛 InvalidOperationException)
  触发 OnItemClicked(itemData)

[SampleBootstrap 订阅者收到]
  Debug.Log($"Clicked: instanceId={item.InstanceId}, typeId={item.TypeId}, name={typeProvider.GetTypeById(item.TypeId).Name}")
```

### 4.5 场景 E:用户清空背包

```
[用户操作]
  点击 HUD 的"清空"按钮

[DebugBackpackHUD]
  调用 backpack.Clear()

[Backpack.Clear]
  清空内部 items 列表
  触发 OnContentsChanged

[BackpackPanelView 收到事件]
  RenderItems(空列表)
  → 销毁所有 ItemCellView 子节点
```

---

## 5. 接口契约

### 5.1 文件清单

```
Assets/BackpackSystem/
├── Runtime/
│   ├── BackpackSystem.Runtime.asmdef
│   ├── Data/
│   │   ├── ItemCategory.cs
│   │   ├── ItemTypeData.cs
│   │   ├── ItemData.cs
│   │   ├── IItemTypeProvider.cs
│   │   └── IInstanceIdGenerator.cs
│   ├── Loading/
│   │   └── IIconLoader.cs
│   ├── Core/
│   │   ├── Backpack.cs
│   │   └── SimpleIncrementalIdGenerator.cs
│   └── View/
│       ├── BackpackPanelView.cs
│       ├── ItemCellView.cs
│       └── CategoryTabView.cs
└── Samples/BasicUsage/
    ├── BackpackSystem.Samples.asmdef
    ├── DemoScene.unity
    ├── Resources/
    │   ├── BackpackSystem/SampleItemTypeDatabase.asset
    │   └── BackpackSystem/Icons/(若干 .png)
    ├── Scripts/
    │   ├── ItemTypeDatabase.cs
    │   ├── ScriptableObjectItemTypeProvider.cs
    │   ├── ResourcesIconLoader.cs
    │   ├── DebugBackpackHUD.cs
    │   └── SampleBootstrap.cs
    └── Prefabs/
        ├── BackpackPanel.prefab
        ├── ItemCell.prefab
        └── CategoryTab.prefab
```

### 5.2 完整接口定义

> 所有方法体均为 `throw new NotImplementedException();` 占位,实现者填充。

#### Runtime/Data/ItemCategory.cs

```csharp
namespace BackpackSystem
{
    public enum ItemCategory
    {
        All = 0,
        Equipment = 1,
        Usable = 2,
        Material = 3,
        Fragment = 4,
        ExpPill = 5,
        Other = 99
    }
}
```

#### Runtime/Data/ItemTypeData.cs

```csharp
using System;

namespace BackpackSystem
{
    /// <summary>道具类型数据(静态)。Inspector 可序列化以支持 ScriptableObject 编辑。</summary>
    [Serializable]
    public class ItemTypeData
    {
        public int Id;
        public string Name;
        public string IconPath;
        public string Description;
        public int MaxStackCount;
        public ItemCategory Category;
    }
}
```

#### Runtime/Data/ItemData.cs

```csharp
namespace BackpackSystem
{
    /// <summary>道具实例数据(运行时,背包每一格对应一个)。</summary>
    public class ItemData
    {
        public string InstanceId;
        public int TypeId;
        public int Count;
    }
}
```

#### Runtime/Data/IItemTypeProvider.cs

```csharp
using System.Collections.Generic;

namespace BackpackSystem
{
    /// <summary>道具类型表数据源契约。</summary>
    public interface IItemTypeProvider
    {
        /// <summary>返回所有已注册类型(实现方应缓存,避免每次返回新集合)。</summary>
        IReadOnlyList<ItemTypeData> GetAllTypes();

        /// <summary>按 typeId 查询单个类型。未找到返回 null。</summary>
        ItemTypeData GetTypeById(int typeId);
    }
}
```

#### Runtime/Data/IInstanceIdGenerator.cs

```csharp
namespace BackpackSystem
{
    /// <summary>实例 id 生成策略。同一 generator 应保证生成的 id 互不重复。</summary>
    public interface IInstanceIdGenerator
    {
        string Generate();
    }
}
```

#### Runtime/Loading/IIconLoader.cs

```csharp
using System;
using UnityEngine;

namespace BackpackSystem
{
    /// <summary>图标加载契约。回调形式以兼容未来异步加载。</summary>
    public interface IIconLoader
    {
        /// <summary>加载图标。失败时回调传 null。</summary>
        void Load(string iconPath, Action<Sprite> onLoaded);
    }
}
```

#### Runtime/Core/Backpack.cs

```csharp
using System;
using System.Collections.Generic;

namespace BackpackSystem
{
    /// <summary>
    /// 背包核心逻辑。纯 C# 类,不依赖 Unity MonoBehaviour。
    /// </summary>
    public class Backpack
    {
        public Backpack(IItemTypeProvider typeProvider, IInstanceIdGenerator idGenerator)
        {
            throw new NotImplementedException();
        }

        /// <summary>当前选中分类(默认 ItemCategory.All)。</summary>
        public ItemCategory CurrentCategory
        {
            get => throw new NotImplementedException();
        }

        /// <summary>
        /// 添加道具(自动堆叠合并)。
        /// 堆叠规则:优先填满已有未满堆 → 满了开新格 → 每格上限 = MaxStackCount。
        /// </summary>
        /// <exception cref="ArgumentException">count ≤ 0</exception>
        /// <exception cref="InvalidOperationException">typeId 在 typeProvider 中不存在</exception>
        public void AddItem(int typeId, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>清空背包。触发 OnContentsChanged。</summary>
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <summary>当前分类下的可见道具列表(只读)。</summary>
        public IReadOnlyList<ItemData> GetItemsInCurrentCategory()
        {
            throw new NotImplementedException();
        }

        /// <summary>切换分类。新分类与当前相同时不触发事件。</summary>
        public void SetCategory(ItemCategory category)
        {
            throw new NotImplementedException();
        }

        /// <summary>View 层调用:通知点击事件,触发 OnItemClicked。</summary>
        /// <exception cref="InvalidOperationException">instanceId 不存在</exception>
        public void NotifyItemClicked(string instanceId)
        {
            throw new NotImplementedException();
        }

        /// <summary>道具内容变化(添加、清空)后触发。</summary>
        public event Action OnContentsChanged;

        /// <summary>分类切换后触发,参数为新分类。</summary>
        public event Action<ItemCategory> OnCategoryChanged;

        /// <summary>道具被点击后触发(经 NotifyItemClicked 转发)。</summary>
        public event Action<ItemData> OnItemClicked;
    }
}
```

#### Runtime/Core/SimpleIncrementalIdGenerator.cs

```csharp
namespace BackpackSystem
{
    /// <summary>
    /// 简单递增整数 id 生成器。形式 "1", "2", "3", ...
    /// 仅适用于 demo / 非持久化场景。非线程安全。
    /// </summary>
    public class SimpleIncrementalIdGenerator : IInstanceIdGenerator
    {
        public string Generate()
        {
            throw new System.NotImplementedException();
        }
    }
}
```

#### Runtime/View/BackpackPanelView.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace BackpackSystem
{
    /// <summary>背包主面板。挂在 BackpackPanel.prefab 根节点。</summary>
    public class BackpackPanelView : MonoBehaviour
    {
        [SerializeField] private Transform _categoryTabContainer;
        [SerializeField] private Transform _itemCellContainer;
        [SerializeField] private ItemCellView _itemCellPrefab;
        [SerializeField] private CategoryTabView _categoryTabPrefab;

        /// <summary>装配面板。Backpack 创建后、面板显示前调用一次。</summary>
        public void Init(Backpack backpack, IItemTypeProvider typeProvider, IIconLoader iconLoader)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>销毁时取消事件订阅,防止 Backpack 持有死引用。</summary>
        private void OnDestroy()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 渲染指定道具列表。阶段一:全量清空重建。阶段二会替换内部实现。
        /// </summary>
        private void RenderItems(IReadOnlyList<ItemData> items)
        {
            throw new System.NotImplementedException();
        }
    }
}
```

#### Runtime/View/ItemCellView.cs

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSystem
{
    /// <summary>单个道具格子。挂在 ItemCell.prefab 根节点。</summary>
    public class ItemCellView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _countText;
        [SerializeField] private Button _button;

        /// <summary>设置此格子要显示的数据。</summary>
        public void SetData(ItemData itemData, IItemTypeProvider typeProvider, IIconLoader iconLoader)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>点击事件,参数为本格子持有的 ItemData。</summary>
        public event Action<ItemData> OnClicked;
    }
}
```

#### Runtime/View/CategoryTabView.cs

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSystem
{
    /// <summary>单个分类页签。挂在 CategoryTab.prefab 根节点。</summary>
    public class CategoryTabView : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [SerializeField] private GameObject _selectedIndicator;
        [SerializeField] private Button _button;

        public ItemCategory Category { get; private set; }

        public void SetCategory(ItemCategory category, string displayName)
        {
            throw new System.NotImplementedException();
        }

        public void SetSelected(bool selected)
        {
            throw new System.NotImplementedException();
        }

        public event Action<ItemCategory> OnSelected;
    }
}
```

#### Runtime/BackpackSystem.Runtime.asmdef

```json
{
    "name": "BackpackSystem.Runtime",
    "rootNamespace": "BackpackSystem",
    "references": [],
    "autoReferenced": true
}
```

#### Samples/BasicUsage/Scripts/ItemTypeDatabase.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace BackpackSystem.Samples
{
    [CreateAssetMenu(fileName = "ItemTypeDatabase", menuName = "BackpackSystem/Item Type Database", order = 1)]
    public class ItemTypeDatabase : ScriptableObject
    {
        public List<ItemTypeData> Types = new List<ItemTypeData>();
    }
}
```

#### Samples/BasicUsage/Scripts/ScriptableObjectItemTypeProvider.cs

```csharp
using System.Collections.Generic;

namespace BackpackSystem.Samples
{
    /// <summary>从 Resources 路径下的 ItemTypeDatabase 读取的 provider 实现。</summary>
    public class ScriptableObjectItemTypeProvider : IItemTypeProvider
    {
        /// <summary>构造时立即从 Resources 加载,失败抛 FileNotFoundException。</summary>
        public ScriptableObjectItemTypeProvider(string resourcesPath)
        {
            throw new System.NotImplementedException();
        }

        public IReadOnlyList<ItemTypeData> GetAllTypes()
        {
            throw new System.NotImplementedException();
        }

        public ItemTypeData GetTypeById(int typeId)
        {
            throw new System.NotImplementedException();
        }
    }
}
```

#### Samples/BasicUsage/Scripts/ResourcesIconLoader.cs

```csharp
using System;
using UnityEngine;

namespace BackpackSystem.Samples
{
    /// <summary>用 Resources.Load 加载图标。同步加载,callback 立即调用。</summary>
    public class ResourcesIconLoader : IIconLoader
    {
        public void Load(string iconPath, Action<Sprite> onLoaded)
        {
            throw new System.NotImplementedException();
        }
    }
}
```

#### Samples/BasicUsage/Scripts/DebugBackpackHUD.cs

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSystem.Samples
{
    /// <summary>调试 HUD:dropdown 选道具 + input 数量 + 添加/清空按钮。</summary>
    public class DebugBackpackHUD : MonoBehaviour
    {
        [SerializeField] private Dropdown _typeDropdown;
        [SerializeField] private InputField _countInput;
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _clearButton;

        /// <summary>装配 HUD。在 Backpack 和 typeProvider 创建后调用。</summary>
        public void Init(Backpack backpack, IItemTypeProvider typeProvider)
        {
            throw new System.NotImplementedException();
        }
    }
}
```

#### Samples/BasicUsage/Scripts/SampleBootstrap.cs

```csharp
using UnityEngine;

namespace BackpackSystem.Samples
{
    /// <summary>
    /// Demo 场景的启动装配脚本。挂在场景中一个空 GameObject 上。
    /// 负责:创建 Backpack、Provider、Loader,然后调用各 View 的 Init。
    /// </summary>
    public class SampleBootstrap : MonoBehaviour
    {
        [SerializeField] private BackpackPanelView _backpackPanel;
        [SerializeField] private DebugBackpackHUD _debugHud;
        [SerializeField] private string _typeDatabaseResourcesPath = "BackpackSystem/SampleItemTypeDatabase";

        private void Awake()
        {
            throw new System.NotImplementedException();
        }
    }
}
```

#### Samples/BasicUsage/BackpackSystem.Samples.asmdef

```json
{
    "name": "BackpackSystem.Samples",
    "rootNamespace": "BackpackSystem.Samples",
    "references": ["BackpackSystem.Runtime"],
    "autoReferenced": true
}
```

---

## 6. 关键决策日志

### 决策 1:数据模型采用类型表 + 实例表分离(方案 A)

- **选择**:`ItemTypeData`(静态)+ `ItemData`(动态),通过 typeId 关联
- **原因**:符合商业游戏数据组织惯例,内存效率高,数据一致性好
- **反面代价**:需要查表才能拿到 name/icon,代码稍多一层间接

### 决策 2:Backpack 是纯 C# 类,不继承 MonoBehaviour

- **选择**:Backpack 是 POCO,通过 event 与 View 通信
- **原因**:逻辑可独立测试,可被多 View 监听,符合 UPM "逻辑可复用" 目标
- **反面代价**:不能用 Unity 序列化,装配要手动写代码(在 SampleBootstrap)

### 决策 3:数据源拆为 IItemTypeProvider(类型) + Backpack 自身(实例操作)

- **选择**:类型表用接口注入,实例操作直接是 Backpack 的 API
- **原因**:类型数据来源多样(SO/JSON/服务器),实例操作是固定的领域行为
- **反面代价**:无;接口和 Backpack 职责清晰

### 决策 4:实例 id 生成抽象为 IInstanceIdGenerator

- **选择**:策略模式,Backpack 通过构造注入
- **原因**:未来切换 GUID/服务器分配 id 零成本,测试可注入固定 id
- **反面代价**:多一个接口和默认实现,5 行代码

### 决策 5:图标加载抽象为 IIconLoader,使用回调而非返回值

- **选择**:View 依赖接口,callback 形式
- **原因**:Runtime 不能写死 Resources.Load(UPM 化要求);回调兼容未来异步
- **反面代价**:同步实现也走回调,稍显啰嗦

### 决策 6:阶段一不做对象池,但 RenderItems 封装为方法

- **选择**:全量清空+重建,但封装在 BackpackPanelView.RenderItems 私有方法
- **原因**:阶段一规模无性能压力;封装让阶段二替换实现时外部无感
- **反面代价**:无

### 决策 7:asmdef 在阶段一加,而不是阶段三

- **选择**:Runtime 和 Samples 各一个 asmdef
- **原因**:asmdef 是代码隔离边界,在阶段一就强制依赖单向,避免阶段三回填踩坑
- **反面代价**:阶段一多两个 JSON 文件,首次编译稍慢

### 决策 8:阶段一不暴露 RemoveItem

- **选择**:只提供 AddItem 和 Clear
- **原因**:本阶段无业务场景需要,YAGNI
- **反面代价**:Backpack 内部数据结构需为未来支持(用 List/Dictionary,而非数组)

### 决策 9:Backpack.NotifyItemClicked 找不到 instanceId 时抛异常

- **选择**:`InvalidOperationException`
- **原因**:这种情况说明 View 和 Backpack 状态不一致,应尽早暴露
- **反面代价**:无,运行时异常会立即提醒开发者

### 决策 10:类型表存储用 ScriptableObject

- **选择**:`ItemTypeDatabase : ScriptableObject`,放 Resources 加载
- **原因**:Unity 标准做法,Inspector 编辑友好,不用写解析代码
- **反面代价**:阶段三 UPM 化时 Resources 路径有约定要遵守

### 决策 11:View 层显式依赖 IItemTypeProvider(v1.1 新增)

- **选择**:`BackpackPanelView.Init` 接收 `IItemTypeProvider` 作为第二参数,Backpack 不暴露任何对 typeProvider 的访问入口
- **原因**:v1.0 spec 内部矛盾——Init 不传 provider,但 ItemCellView.SetData 必需。可选方案对比:
  - A) Backpack 加 `internal TypeProvider` 属性 → 污染 Backpack 职责,违反"纯实例容器"设计意图
  - B) Init 加参数 → View 显式声明依赖,职责清晰
  - 选 B
- **反面代价**:Init 签名增加一个参数,装配代码多一行;原 v1.0 写错的部分需在 §4.1 / §5.2 / §8.2 多处同步修正

### 决策 12:Backpack 内部用 List 存储,线性查找

- **选择**:Backpack 内部用 `List<ItemData>` 保存所有道具实例,`NotifyItemClicked` 用线性扫描查找 instanceId
- **原因**:阶段一规模无性能压力(< 200 道具),List 简单且天然保留插入顺序(满足 §7.1 用例 5);未来若规模上千,可加 `Dictionary<string, ItemData>` 索引并行维护,不影响接口
- **反面代价**:阶段二做分帧加载/对象池时,如果列表频繁变动,线性查找成本会显现;届时再优化即可

### 决策 13:GetItemsInCurrentCategory 始终返回快照(v1.1 行为澄清)

- **选择**:无论当前分类是 All 还是其他,均返回新拷贝的 `List<ItemData>`,不返回内部 list 的活引用
- **原因**:阶段二的分帧加载协程会持有此列表迭代;若返回活引用,迭代期间用户操作 AddItem/Clear 会破坏迭代器或产生不一致快照
- **反面代价**:每次调用产生一次 list 拷贝,阶段一规模下成本可忽略

---

## 7. 验收标准

### 7.1 功能验收

| # | 测试场景 | 期望结果 |
|---|---|---|
| 1 | Play 模式启动 DemoScene | 背包面板显示,所有分类页签可见,默认"All"选中,道具列表为空 |
| 2 | HUD 选"大血瓶",输入"5",点添加 | 背包出现 1 格"大血瓶 ×5",图标和数量正确 |
| 3 | 继续添加同样的 50 个 | 背包变成 1 格"大血瓶 ×55"(假设 MaxStack ≥ 99) |
| 4 | 添加 100 个 MaxStack=99 的道具 | 出现 2 格(99 + 1) |
| 5 | 添加多种不同类型的道具 | 每种独立成格,顺序按添加顺序 |
| 6 | 点"装备"页签 | 列表只显示装备类道具,其他不可见;页签视觉切换 |
| 7 | 点"All" | 所有道具重新显示 |
| 8 | 点击任意道具 | Console 打印 `Clicked: instanceId=X, typeId=Y, name=Z` |
| 9 | 点 HUD"清空" | 背包所有道具消失 |
| 10 | HUD 输入非法数量(0、负数、非数字) | Console 打印警告,Backpack 状态不变 |

### 7.2 代码质量验收

- [ ] Runtime 文件夹下所有 .cs 文件**没有任何** `using BackpackSystem.Samples` 语句
- [ ] Runtime 文件夹下所有 .cs 文件**没有** `Resources.Load` 调用
- [ ] Runtime 文件夹下所有 .cs 文件**没有引用 Sample Prefab/Asset**
- [ ] 所有 public 方法都有 XML 文档注释
- [ ] `Backpack` 类不继承 MonoBehaviour
- [ ] asmdef 配置正确:Runtime references 为空,Samples references 包含 BackpackSystem.Runtime
- [ ] 所有事件订阅在 OnDestroy 中正确取消

### 7.3 异常情况验收

| # | 操作 | 期望异常 |
|---|---|---|
| 1 | `backpack.AddItem(typeId=999999, 1)`(typeId 不存在) | `InvalidOperationException` |
| 2 | `backpack.AddItem(typeId=5001, 0)` | `ArgumentException` |
| 3 | `backpack.AddItem(typeId=5001, -1)` | `ArgumentException` |
| 4 | `backpack.NotifyItemClicked("nonexistent_id")` | `InvalidOperationException` |
| 5 | `new ScriptableObjectItemTypeProvider("wrong/path")` | `FileNotFoundException` |

---

## 8. 实现者自由度(spec 不规定的部分)

以下事项由实现者自行决定,spec 不做规定,阶段二/三也不会因此回头改动:

### 8.1 UI 文案

- 分类页签的显示文本(如"全部 / 装备 / ..."的具体中文/英文/i18n 方案)
- 数量文本格式(如 `5` vs `×5` vs `x5`)
- HUD 提示语、按钮文本

> 实现者通常会在 `BackpackPanelView` 内部维护一个分类→文本的映射;如果未来要做 i18n,把这个映射抽到外部即可,不影响接口。

### 8.2 Prefab 内部布局

- `BackpackPanel.prefab` 子节点的具体层级(ScrollRect、ContentSizeFitter、Layout Group 等)
- 分类页签的横排/竖排
- 选中态指示器是 GameObject、Image 还是 Outline 组件
- 数量文本是否带 "×" 前缀(可在 prefab 加静态 Text)

> spec 只规定 `BackpackPanelView` 通过哪些 SerializeField 字段连接 prefab(`_categoryTabContainer`、`_itemCellContainer` 等),具体的 GameObject 树和组件配置由实现者决定。

### 8.3 实现细节(不影响接口)

- Backpack 内部的具体数据结构(只要满足"按添加顺序"和验收异常约定即可)
- HUD 的输入校验策略(只要保证非法输入不调用 Backpack)
- HUD dropdown 选项的具体显示格式(如 "Name" vs "id - Name" vs name 为空时的 fallback)
- 各 View 是否额外维护私有字段、辅助方法、命名风格

> 这些实现选择可以记录在代码注释或团队内部文档,但不进入 spec。

---

## 9. 阶段二/三的预留与约束

### 9.1 阶段二会做的事(本阶段不要做)

- `BackpackPanelView.RenderItems` 内部改为分帧加载协程
- 新增 `ItemDetailPopupView`,在 `OnItemClicked` 事件订阅者列表中新增弹窗显示
- 新增 `BottomBarView`,作为 BackpackPanel 的子组件
- (可选)新增整理按钮和排序逻辑

### 9.2 阶段一接口禁止改动的部分

以下接口在阶段二/三**只能新增方法,不能修改现有签名**:

- `Backpack` 的所有 public 方法和事件
- `IItemTypeProvider` / `IInstanceIdGenerator` / `IIconLoader`
- `ItemTypeData` / `ItemData`(可加字段,不可删/改字段)
- `BackpackPanelView.Init(Backpack, IItemTypeProvider, IIconLoader)` 三参数签名(v1.1 起)

### 9.3 阶段三的工作清单(本阶段不要做)

- 添加 `package.json`
- 添加 `README.md` / `CHANGELOG.md` / `LICENSE.md`
- 将 `Samples/` 目录改名为 `Samples~/`(波浪线后缀让 Unity 不直接编译,改由 UPM 通过 Package Manager 的 "Import Sample" 机制按需导入)
- 移到 `Packages/com.xxx.backpacksystem/` 或独立 git 仓库
- 在 package.json 注册 sample
- 资源隔离最终审核

---

**文档版本**:v1.2
**对应代码状态**:阶段一验收通过

### 版本历史

**v1.0** — 阶段一开发开始前的初版

**v1.1** — 阶段一实现期间的契约修正
- `BackpackPanelView.Init` 签名:增加 `IItemTypeProvider typeProvider` 参数(详见决策 11)
- `Backpack.GetItemsInCurrentCategory` 行为澄清:始终返回快照(详见决策 13)
- §4.1 数据流第 6 步、§5.2 接口契约、§9.2 冻结条款已同步更新

**v1.2** — 阶段一验收完成后的回顾性整理
- 新增决策 11(View 显式依赖 IItemTypeProvider)、决策 12(Backpack 内部数据结构)、决策 13(GetItemsInCurrentCategory 快照语义),将 v1.1 的临时变更整合进决策日志
- §9.2 冻结条款明确写出 `Init` 三参数签名,避免未来歧义
- 新增第 8 节"实现者自由度",明确划清 spec 不规定的范围(UI 文案、prefab 布局、实现细节)
