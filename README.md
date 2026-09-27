# PromptRun for Command Palette

PowerToys **Command Palette (CmdPal)** 提示词管理扩展：全局唤起、毫秒级搜索提示词、一键复制、GitHub 私库同步。

- 独立项目，与 PromptPal / PromptRun（PowerToys Run 版）零代码、零数据链接
- 数据完全自持：单个 `prompts.json`，位置 `%LOCALAPPDATA%\Microsoft\PowerToys\CmdPal\PromptRun\prompts.json`
- 首次启动可**一键导入** Run 版 `prompts.json`（含收藏与 useCount 历史，不修改原件）

## 功能

- **搜索**：多词 AND 匹配（title / tags / content），收藏 → 使用次数 → 匹配权重 → 更新时间 四级排序
- **复制**：Enter 复制正文（`{{占位符}}` 原样复制，副标题有标注），复制自动 useCount +1
- **详情**：Markdown 渲染正文预览 + 元信息
- **管理**：空 query 列出全部提示词 + 管理项（打开数据文件夹 / Push / Pull / 首次导入）
- **同步**：GitHub 私库单文件同步（Contents API），Pull 前自动备份 `.bak`

## 安装（从 GitHub Release）

1. 下载 Release 中的 `PromptRun.CmdPal_<版本>_x64.msix` 与 `promptrun-sign.cer`
2. 双击 `.cer` → 安装证书 → **本地计算机** → 将所有的证书都放入下列存储 → **受信任的人**（Trusted People）
3. 双击 `.msix` 安装（或 `Add-AppxPackage -Path <msix 路径>`）
4. 打开 Command Palette（`Win+Alt+Space`）→ 键入 PromptRun → 使用

升级版本：下载新 msix 直接安装覆盖；数据文件不受影响。

## 首次使用

- 若检测到 Run 版数据（`...\PowerToys Run\Settings\Plugins\PromptRun\prompts.json`），列表顶部出现 **Import from PowerToys Run**，确认即完成迁移；本地文件一旦存在该入口不再出现
- 无旧数据时自动生成含示例的模板 `prompts.json`，可直接手工编辑（UTF-8、2 空格缩进），保存后 300ms 内自动热重载

## GitHub 同步配置

1. 建一个**私有**仓库（如 `prompts-vault`，不用初始化 README）
2. GitHub → Settings → Developer settings → Fine-grained personal access tokens：
   - Repository access：只选该仓库
   - Permissions → Contents：**Read and write**
3. Command Palette → PromptRun 条目右键/上下文键 → **Settings** → 填入 `owner/repo` 与 PAT

Push = 本地覆盖远端；Pull = 远端覆盖本地（自动生成 `prompts.json.bak`）。未配置凭据时动作会给出引导提示。

## 开发

```powershell
# 构建（含 Core/Tests 引用链）
dotnet build src/PromptRun.CmdPal/PromptRun.CmdPal.csproj -p:Platform=x64

# 测试
dotnet test tests/PromptRun.Tests/PromptRun.Tests.csproj

# 出侧载 MSIX（签名另行 signtool，见 .github/workflows/release.yml）
dotnet build src/PromptRun.CmdPal/PromptRun.CmdPal.csproj -p:Platform=x64 -p:Configuration=Release `
  -p:RuntimeIdentifier=win-x64 -p:UapAppxPackageBuildMode=SideloadOnly `
  -p:AppxPackageSigningEnabled=false -p:GenerateAppxPackageOnBuild=true `
  -p:AppxPackageDir="outpkg/"
```

结构：`src/PromptRun.Core`（纯逻辑：schema/搜索/同步，零宿主依赖）· `src/PromptRun.CmdPal`（CmdPal 宿主适配）· `tests`（xUnit）。规格见 `openspec/changes/add-promptrun-cmdpal/`。

## License

MIT
