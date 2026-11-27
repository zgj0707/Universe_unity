# 工作区协作规范（AGENTS.md）

## 1. 模型分工与工具调用
- **并列按难度调度**：以下三种编码与审查手段地位并列，按任务复杂度灵活选用：
  - **最低档（轻量/简单任务）**：调用 `codex.exe -m gpt-6-luna`（适合单模块小改、静态数据补充、简单脚本与生图驱动）。
  - **通用档（常规/中等任务）**：调用 `codebuddy`（适合日常功能实现、常规逻辑调整与中等规模改动）。
  - **最高档（复杂/高难任务）**：调用 `codex.exe -m gpt-6.1-terra`（适合核心算法、跨文件复杂整合、疑难 Bug 排查与深度 Diff 审查）。
- **额度耗尽回退**：仅当上述对应工具额度均耗尽时，才由主 Agent 直接接管编写；严禁擅自使用高价模型（如 `gpt-6.1-sol`）。

## 2. 美术生图规范
- **优先使用 Codex 生图**：**Codex 本身就能生图，只需要给它提示词**。后续所有新增美术素材生图任务（区域卡片、车辆立绘、人物立绘、界面插画等），只要 Codex 仍有可用额度，必须优先使用 `codex.exe` 调用 `gpt-6-luna` / `gpt-6.1-terra` 自身内置的 `image_gen` 生图。
- **Codex 生图标准调用方法（防踩坑）**：
  1. 在 `codex.exe exec` 中必须附加 `--add-dir 'C:\Users\Administrator\.codex\generated_images'`（否则默认 `workspace-write` 沙盒无法访问生图输出目录）。
  2. 提示词中明确要求**仅使用 Codex 内置 `image_gen`，禁止调用 `taptap-maker` 等 MCP 生图工具**（无交互模式下 MCP 会因审批策略被拒）。
  3. Codex 内置 `image_gen` 生成的图像默认落盘在 `C:\Users\Administrator\.codex\generated_images\<session_id>\exec-*.png`，由 Codex（或主 Agent）按时间排序取最新文件转存/复制到项目目标路径（如 `Assets/Resources/PageArt/...`）。
- **回退条件**：仅当 Codex 额度耗尽或内置 `image_gen` 明确不可用时，才回退内置 `image-generator` 并向用户说明。

## 3. 剧情与文案规范
- **剧情文案使用 Hydrogen Low**：后续所有剧情对话、开局引导台词、档案手记与角色叙事文案创作任务，统一优先使用 **Hydrogen Low** 撰写。

## 4. 交互与进度清单规范
- **默认不使用步骤清单（`update_steps`）**：除非用户明确要求拉出待办清单（todolist），否则禁止主动调用 `update_steps`。

## 5. 界面与设计准则
- **克制次级标签与冗余说明**：Avoid unnecessary secondary labels and descriptions; let the layout do the explaining.（避免堆砌不必要的副标题、辅助说明小字与冗余标签，依靠清晰的布局、留白与视觉层级本身来传达信息。）
- **纯预览界面极简铁律**：所有纯预览界面/弹窗仅显示**名称标题**与**立绘大图**，不堆砌副标题、状态提示或底部大块留白（需要装配操作的界面除外）；该规则适用于后续所有类似预览。
