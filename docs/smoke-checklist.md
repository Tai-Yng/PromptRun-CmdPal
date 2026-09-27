# 真机冒烟清单（v0.1.0）

前置：扩展已安装（`Get-AppxPackage TaiYng.PromptRun.CmdPal` → status=Ok）。
打开 Command Palette：`Win+Alt+Space`。每项通过打勾，异常记到末尾"问题"区并回填 design.md。

## 加载

- [ ] 1. 键入 `PromptRun`，命令出现在列表（新装的扩展宿主经 PackageCatalog 事件应自动发现；若没有，先在 CmdPal 设置里跑一次 "Reload" 命令再试）
- [ ] 2. Enter 进入主列表页，无报错（空白列表/模板示例均可）

## 数据与导入（对应 prompt-library spec）

- [ ] 3. 首启且 Run 版有旧数据：列表顶部出现 **Import from PowerToys Run**
- [ ] 4. Enter 执行导入 → toast "Import complete..." → 列表出现 Run 版全部提示词（收藏/次数不变）
- [ ] 5. （或）无旧数据：出现模板示例两条；`%LOCALAPPDATA%\Microsoft\PowerToys\CmdPal\PromptRun\prompts.json` 已生成且可手编

## 搜索（对应 prompt-search spec）

- [ ] 6. 空 query：全部提示词按 收藏→次数→时间 排序在前，管理项（Import/Open data folder/Push/Pull）在末尾
- [ ] 7. 键入两词（如 `翻译 专业`）：AND 语义，只留全部命中的条目
- [ ] 8. 复制次数多的条目排前；收藏条目置顶
- [ ] 9. 含 `{{var}}` 的条目副标题有 `{{placeholder}}` 标注

## 复制（对应 clipboard-actions spec）

- [ ] 10. Enter 复制 → toast "Copied ..." → 到记事本粘贴，内容与 prompts.json 中该条逐字一致（含 `{{var}}` 不替换）
- [ ] 11. 复制后重新打开列表，该条副标题 useCount +1；`prompts.json` 中数值已写回
- [ ] 12. 进入详情页：正文 Markdown 渲染 + 元信息可见

## 热重载与降级

- [ ] 13. 手工编辑 `prompts.json`（改一个标题）保存 → 回列表直接键入，改动已生效
- [ ] 14. 把 `prompts.json` 改成非法 JSON 保存 → 列表降级为提示态，CmdPal 不崩
- [ ] 15. 设置（PromptRun 条目上下文键 → Settings）→ 填 repo+PAT → Push/Pull 动作有 toast 反馈（成功或引导）

## 问题记录

| # | 现象 | 处理 |
|---|---|---|
| | | |
