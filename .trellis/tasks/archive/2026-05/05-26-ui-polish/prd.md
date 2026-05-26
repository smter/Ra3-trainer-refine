# UI 视觉层级与交互优化

## Goal

基于 Catppuccin Mocha 暗色主题基础设施，优化按钮语义层级、状态颜色反馈、空间排版和输入框对比度，提升界面专业感与可用性。

## Confirmed Facts

- 主题基础设施已完成（25 色 + 18 语义 Brush + 6 控件 Style）
- `PrimaryButtonStyle` 已定义（Mauve 实心）但**零处引用**——所有按钮用同一个隐式 Style
- 无 `DangerButtonStyle`、无 `IValueConverter`（项目首次）
- `InputBackgroundBrush` = `CardBackgroundBrush` = 同一个 `Surface0`——输入框在卡片上无视觉区分
- 顶栏塞入全部配置：进程检测 + 路径/参数 + Money/Power/SC + 增援ID/数量/星级
- "增援预设"卡片内 10 个按钮 `Margin="8,0,0,0"` 密集排列
- 快捷键文本同状态文本色——无视觉降级

## Requirements

### R1: 按钮语义分层
- PrimaryButtonStyle → "一键启动并装载"、feature "开启/执行"、"执行队列"
- 默认隐式 Style → "重新检测进程"、"浏览"、"保存设置"、"保存预设"等辅助操作
- DangerButtonStyle → "清空队列"、"恢复所有 Patch"、"移除"

### R2: DangerButtonStyle
- 默认：与次要按钮同色系
- IsMouseOver：边框 + 文字 + 背景变 `ErrorBrush` (Red)

### R3: 状态语义色
- 连接状态加颜色圆点（红=断开, 绿=已连接, 黄=处理中）
- feature "开启/关闭"状态文字：启用=SuccessBrush 绿, 未启用=TextDisabledBrush
- 新建 `StatusToBrushConverter`（IValueConverter）

### R4: 布局重组
- 顶栏仅"连接与启动"：标题 + 状态 + 路径 + 参数 + 核心操作按钮
- 新建"基础资源"卡片：Money / Power / SC Point
- 增援预设按钮间距拉大到 12-16px
- TextBox 统一高度、标签列宽对齐

### R5: 输入框层级
- `InputBackgroundBrush` 从 `Surface0` 改为 `Base`，深于卡片背景

### R6: 快捷键弱化
- `Ctrl+F1` 等快捷键文本降至最低对比度

## Acceptance Criteria

- [ ] "一键启动并装载"用 PrimaryButtonStyle（Mauve 实心）
- [ ] feature "开启/执行"用 PrimaryButtonStyle
- [ ] "清空队列" hover 显示红色预警
- [ ] 连接状态前有颜色圆点
- [ ] "基础资源"卡片独立于顶栏
- [ ] 输入框背景深于卡片背景
- [ ] 所有现有功能无回归

## Out of Scope

- 动画过渡
- ViewModel 重构
- 新控件类型

## Notes

复杂任务，需 design.md + implement.md + JSONL。

## Decisions Made

1. **状态颜色方案**: A — `MainViewModel` 加 `ConnectionState` 枚举，`FeatureItemViewModel` 加 `IsActive` 计算 bool
2. **DangerButtonStyle**: A — 默认与次要按钮一致，hover 边框+文字+背景变 Red
3. **Feature 按钮启用态**: A — IsActive=true 时 SuccessBrush 绿底实心，false 时 Mauve 实心
