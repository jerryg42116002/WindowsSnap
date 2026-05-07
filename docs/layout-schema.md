# Layout Schema

WindowSnapper 使用 JSON 文件定义用户自定义布局。布局文件放在：

```text
%APPDATA%/WindowSnapper/layouts/*.json
```

应用启动时会读取该目录下的 `*.json` 文件。合法布局会合并到布局注册表中；非法布局会跳过并记录文件名级别的错误。用户布局不能覆盖内置布局 id。

## JSON 结构

```json
{
  "id": "dev-layout",
  "name": "开发布局",
  "version": 1,
  "gap": 8,
  "margin": 8,
  "zones": [
    {
      "id": "code",
      "name": "代码区",
      "x": 0,
      "y": 0,
      "width": 0.6,
      "height": 1
    }
  ]
}
```

JSON 序列化使用 camelCase 字段名，读取时大小写不敏感。

## 字段说明

### LayoutDefinition

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | string | 布局稳定 id。用户布局 id 不能与内置布局重复。 |
| `name` | string | 用于托盘菜单显示的布局名称。 |
| `version` | number | 布局 schema 版本。当前布局模型使用 `1`。 |
| `gap` | number | 区域内部边界间距，单位为像素。必须大于等于 `0`。 |
| `margin` | number | 显示器工作区边缘边距，单位为像素。必须大于等于 `0`。 |
| `zones` | array | 布局区域列表，至少包含一个区域。 |

### ZoneDefinition

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | string | 区域稳定 id。 |
| `name` | string | 用于托盘菜单显示的区域名称。 |
| `x` | number | 区域左侧相对坐标，范围从 `0.0` 到 `1.0`。 |
| `y` | number | 区域顶部相对坐标，范围从 `0.0` 到 `1.0`。 |
| `width` | number | 区域相对宽度，必须大于 `0`。 |
| `height` | number | 区域相对高度，必须大于 `0`。 |

坐标相对于当前显示器的 `WorkArea`，不是完整 `Bounds`。任务栏占用区域不会被布局覆盖。

## 示例布局

三栏开发布局：

```json
{
  "id": "dev-three-zone",
  "name": "开发三栏",
  "version": 1,
  "gap": 8,
  "margin": 8,
  "zones": [
    {
      "id": "editor",
      "name": "编辑器",
      "x": 0,
      "y": 0,
      "width": 0.6,
      "height": 1
    },
    {
      "id": "browser",
      "name": "浏览器",
      "x": 0.6,
      "y": 0,
      "width": 0.4,
      "height": 0.5
    },
    {
      "id": "terminal",
      "name": "终端",
      "x": 0.6,
      "y": 0.5,
      "width": 0.4,
      "height": 0.5
    }
  ]
}
```

## 校验规则

布局规则：

- `id` 不能为空。
- `name` 不能为空。
- `gap >= 0`。
- `margin >= 0`。
- `zones` 至少包含 1 个区域。

区域规则：

- `id` 不能为空。
- `name` 不能为空。
- `width > 0`。
- `height > 0`。
- `x >= 0`。
- `y >= 0`。
- `x + width <= 1`。
- `y + height <= 1`。

计算规则：

- `LayoutEngine` 只做纯逻辑计算。
- 目标矩形基于 `MonitorInfo.WorkArea`。
- 支持负坐标显示器。
- 支持横屏和竖屏。
- `margin` 应用于工作区外边缘。
- `gap` 应用于非边缘的内部区域边界。
- 计算后目标宽高必须大于 `0`。

## 内置布局 id

内置布局 id 是稳定 API，发布后不应随意修改。

| id | zone id | 说明 |
| --- | --- | --- |
| `left-half` | `main` | 左半屏 |
| `right-half` | `main` | 右半屏 |
| `top-half` | `main` | 上半屏 |
| `bottom-half` | `main` | 下半屏 |
| `quad-top-left` | `main` | 左上四分区 |
| `quad-top-right` | `main` | 右上四分区 |
| `quad-bottom-left` | `main` | 左下四分区 |
| `quad-bottom-right` | `main` | 右下四分区 |
| `left-two-thirds` | `main` | 左侧三分之二 |
| `right-one-third` | `main` | 右侧三分之一 |
| `left-one-third` | `main` | 左侧三分之一 |
| `right-two-thirds` | `main` | 右侧三分之二 |

## 冲突处理

- 用户布局 id 与内置布局 id 冲突时，用户布局会被跳过。
- 多个用户布局 id 重复时，后出现的重复项会被跳过。
- 单个非法布局文件不会阻止其他布局加载。
