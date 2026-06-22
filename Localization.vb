Imports System.IO
Imports System.Text.Json
Imports System.Collections.Generic

Public Class Localization
    Public Property CurrentLang As String = "en"

    Private ReadOnly configPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")
    Private ReadOnly dicts As New Dictionary(Of String, Dictionary(Of String, String))()

    Public Sub New()
        ' Initialize dictionaries
        InitializeDictionaries()
        LoadSettings()
    End Sub

    Public Function GetString(key As String) As String
        If dicts.ContainsKey(CurrentLang) AndAlso dicts(CurrentLang).ContainsKey(key) Then
            Return dicts(CurrentLang)(key)
        End If
        ' Fallback to English
        If dicts("en").ContainsKey(key) Then Return dicts("en")(key)
        Return key
    End Function

    Public Sub SaveSettings(lang As String)
        CurrentLang = lang
        Try
            Dim cfg As New Dictionary(Of String, String) From {{"Language", lang}}
            File.WriteAllText(configPath, JsonSerializer.Serialize(cfg))
        Catch
            ' Ignore save errors if file locked
        End Try
    End Sub

    Private Sub LoadSettings()
        If File.Exists(configPath) Then
            Try
                Dim jsonStr = File.ReadAllText(configPath)
                Dim cfg = JsonSerializer.Deserialize(Of Dictionary(Of String, String))(jsonStr)
                If cfg IsNot Nothing AndAlso cfg.ContainsKey("Language") Then
                    CurrentLang = cfg("Language")
                End If
            Catch
            End Try
        End If
    End Sub

    Private Sub InitializeDictionaries()
        ' English
        dicts("en") = New Dictionary(Of String, String) From {
            {"Title", "Demosaic Tool (colMask) - VB.net"},
            {"Path", "Path:"},
            {"ScanAll", "Scan All"},
            {"BackupFile", "Backup File"},
            {"StartDemosaic", "Start Demosaic"},
            {"AvailableShaders", "Available Shaders"},
            {"SelectedShaders", "Selected Shaders"},
            {"ColName", "Name"},
            {"ColPathID", "PathID"},
            {"ColFile", "File"},
            {"ColStatus", "Status"},
            {"MsgSelectPath", "Please select a path."},
            {"MsgNoSelection", "No shaders selected for processing."},
            {"MsgComplete", "Processing complete."},
            {"LogScanStart", "Scanning {0} files for shaders..."},
            {"LogScanComplete", "Scan complete."},
            {"LogApkDetected", "APK mode detected. Processing: {0}"},
            {"LogExtracting", "Extracting APK..."},
            {"LogNormalMode", "Normal mode detected."},
            {"LogReplacing", "Replacing shader in {0}"},
            {"LogSaved", "Saved -> {0}"},
            {"LogBackup", "Backing up original file to {0}"},
            {"LogRebuilding", "Rebuilding APK with {0} modified files..."},
            {"LogSigning", "Signing APK with debug key..."},
            {"LogSignedApk", "Signed APK saved to: {0}"},
            {"LogError", "Error: {0}"},
            {"Log", "Log"},
            {"ReplaceWith", "Replace with..."},
            {"CopyInfo", "Copy Info"},
            {"SelectSourceShader", "Select Source Shader"},
            {"TargetShaderInfo", "Target Shader (to be replaced)"},
            {"Keywords", "Keywords"},
            {"KeywordSettings", "Mosaic Keywords"},
            {"KeywordList", "Auto-select shader name keywords"},
            {"AddKeyword", "Add"},
            {"DeleteKeyword", "Delete"},
            {"RestoreDefaults", "Default"},
            {"Diagnose", "Diagnose"},
            {"RestoreBackup", "Restore"},
            {"DryRunTitle", "Confirm before writing"},
            {"DryRunHeader", "Selected shaders: {0} / target files: {1}"},
            {"DryRunOperations", "colMask edits: {0} / shader replacements: {1}"},
            {"DryRunWriters", "Raw-patch files: {0} / structured-save files: {1}"},
            {"DryRunBackup", "Backup: {0}"},
            {"DryRunTargets", "Targets:"},
            {"DryRunContinue", "Start writing with this plan?"},
            {"On", "On"},
            {"Off", "Off"},
            {"MsgNoBackups", "No .bak files were found for this path."},
            {"RestoreConfirm", "Restore {0} .bak file(s) over their original files?"},
            {"MsgRestoreComplete", "Restore complete. Restored {0} file(s)."},
            {"LogRestored", "Restored backup -> {0}"},
            {"LogDiagnosticsStart", "Diagnostics: scanning {0} files for non-shader mosaic-like assets..."},
            {"LogDiagnosticFinding", "  -> Possible baked/2D mosaic asset: {0} '{1}' (pathId:{2}, file:{3})"},
            {"LogDiagnosticsNone", "Diagnostics: no mosaic-like Texture2D/Sprite/Material names found."},
            {"LogDiagnosticsMore", "  ... and {0} more diagnostic finding(s)."},
            {"LogDiagnosticsSummary", "Diagnostics complete: {0} possible non-shader mosaic asset(s). Shader colMask edits may not affect baked textures/sprites."},
            {"OK", "OK"},
            {"Cancel", "Cancel"}
        }

        ' Japanese
        dicts("ja") = New Dictionary(Of String, String) From {
            {"Title", "モザイク除去ツール (colMask) - VB.net"},
            {"Path", "パス:"},
            {"ScanAll", "全てスキャン"},
            {"BackupFile", "バックアップ"},
            {"StartDemosaic", "モザイク除去開始"},
            {"AvailableShaders", "利用可能なシェーダー"},
            {"SelectedShaders", "選択されたシェーダー"},
            {"ColName", "名前"},
            {"ColPathID", "PathID"},
            {"ColFile", "ファイル"},
            {"ColStatus", "状態"},
            {"MsgSelectPath", "パスを選択してください。"},
            {"MsgNoSelection", "処理するシェーダーが選択されていません。"},
            {"MsgComplete", "処理が完了しました。"},
            {"LogScanStart", "{0} 個のファイルをスキャン中..."},
            {"LogScanComplete", "スキャン完了。"},
            {"LogApkDetected", "APKモード: {0} を処理中"},
            {"LogExtracting", "APKを展開中..."},
            {"LogNormalMode", "通常モード。"},
            {"LogReplacing", "シェーダーを置換中: {0}"},
            {"LogSaved", "保存完了 -> {0}"},
            {"LogBackup", "バックアップを作成中: {0}"},
            {"LogRebuilding", "APKを再構築中（{0} 個のファイルを更新）..."},
            {"LogSigning", "APKに署名中..."},
            {"LogSignedApk", "署名済みAPKを保存しました: {0}"},
            {"LogError", "エラー: {0}"},
            {"Log", "ログ"},
            {"ReplaceWith", "…で置換"},
            {"CopyInfo", "情報をコピー"},
            {"SelectSourceShader", "ソースシェーダーを選択"},
            {"TargetShaderInfo", "ターゲットシェーダー（置換対象）"},
            {"Keywords", "キーワード"},
            {"KeywordSettings", "モザイク判定キーワード"},
            {"KeywordList", "自動選択に使うシェーダー名キーワード"},
            {"AddKeyword", "追加"},
            {"DeleteKeyword", "削除"},
            {"RestoreDefaults", "既定"},
            {"Diagnose", "診断"},
            {"RestoreBackup", "復元"},
            {"DryRunTitle", "書き換え前の確認"},
            {"DryRunHeader", "選択シェーダー: {0} 件 / 対象ファイル: {1} 件"},
            {"DryRunOperations", "colMask編集: {0} 件 / シェーダー置換: {1} 件"},
            {"DryRunWriters", "raw patch予定ファイル: {0} 件 / 構造保存予定ファイル: {1} 件"},
            {"DryRunBackup", "バックアップ: {0}"},
            {"DryRunTargets", "対象:"},
            {"DryRunContinue", "この内容で書き換えを開始しますか？"},
            {"On", "オン"},
            {"Off", "オフ"},
            {"MsgNoBackups", "このパスに復元できる .bak ファイルはありません。"},
            {"RestoreConfirm", "{0} 個の .bak ファイルを元ファイルへ復元しますか？"},
            {"MsgRestoreComplete", "復元が完了しました。{0} 個のファイルを復元しました。"},
            {"LogRestored", "バックアップから復元 -> {0}"},
            {"LogDiagnosticsStart", "診断: Shader以外のモザイク候補アセットを {0} 個のファイルから検索中..."},
            {"LogDiagnosticFinding", "  -> 2D/焼き込みモザイク候補: {0} '{1}' (pathId:{2}, file:{3})"},
            {"LogDiagnosticsNone", "診断: モザイクらしい Texture2D/Sprite/Material 名は見つかりませんでした。"},
            {"LogDiagnosticsMore", "  ... 他 {0} 件の診断候補があります。"},
            {"LogDiagnosticsSummary", "診断完了: Shader以外のモザイク候補が {0} 件あります。Texture/Spriteに焼き込まれている場合、Shader colMask編集では見た目が変わらないことがあります。"},
            {"OK", "OK"},
            {"Cancel", "キャンセル"}
        }

        ' Traditional Chinese
        dicts("zh-Hant") = New Dictionary(Of String, String) From {
            {"Title", "去馬賽克工具 (colMask) - VB.net"},
            {"Path", "路徑:"},
            {"ScanAll", "全部掃描"},
            {"BackupFile", "備份檔案"},
            {"StartDemosaic", "開始去馬賽克"},
            {"AvailableShaders", "可用的著色器"},
            {"SelectedShaders", "已選的著色器"},
            {"ColName", "名稱"},
            {"ColPathID", "PathID"},
            {"ColFile", "檔案"},
            {"ColStatus", "狀態"},
            {"MsgSelectPath", "請選擇路徑。"},
            {"MsgNoSelection", "未選擇要處理的著色器。"},
            {"MsgComplete", "處理完成。"},
            {"LogScanStart", "正在掃描 {0} 個檔案..."},
            {"LogScanComplete", "掃描完成。"},
            {"LogApkDetected", "偵測到APK模式。正在處理: {0}"},
            {"LogExtracting", "正在解壓縮APK..."},
            {"LogNormalMode", "偵測到一般模式。"},
            {"LogReplacing", "正在替換著色器: {0}"},
            {"LogSaved", "已儲存 -> {0}"},
            {"LogBackup", "正在備份原始檔案到 {0}"},
            {"LogRebuilding", "正在重建APK (已修改 {0} 個檔案)..."},
            {"LogSigning", "正在對APK進行簽名..."},
            {"LogSignedApk", "已儲存已簽署的 APK: {0}"},
            {"LogError", "錯誤: {0}"},
            {"Log", "日誌"},
            {"ReplaceWith", "替換為..."},
            {"CopyInfo", "複製資訊"},
            {"SelectSourceShader", "選擇來源着色器"},
            {"TargetShaderInfo", "目標着色器（將被替換）"},
            {"Keywords", "關鍵字"},
            {"KeywordSettings", "馬賽克判定關鍵字"},
            {"KeywordList", "自動選取用的着色器名稱關鍵字"},
            {"AddKeyword", "新增"},
            {"DeleteKeyword", "刪除"},
            {"RestoreDefaults", "預設"},
            {"Diagnose", "診斷"},
            {"RestoreBackup", "還原"},
            {"DryRunTitle", "寫入前確認"},
            {"DryRunHeader", "已選着色器: {0} / 目標檔案: {1}"},
            {"DryRunOperations", "colMask 編輯: {0} / 着色器替換: {1}"},
            {"DryRunWriters", "raw patch 預定檔案: {0} / 結構保存預定檔案: {1}"},
            {"DryRunBackup", "備份: {0}"},
            {"DryRunTargets", "目標:"},
            {"DryRunContinue", "要依此內容開始寫入嗎？"},
            {"On", "開"},
            {"Off", "關"},
            {"MsgNoBackups", "此路徑沒有可還原的 .bak 檔案。"},
            {"RestoreConfirm", "要將 {0} 個 .bak 檔案還原覆蓋到原始檔案嗎？"},
            {"MsgRestoreComplete", "還原完成。已還原 {0} 個檔案。"},
            {"LogRestored", "已從備份還原 -> {0}"},
            {"LogDiagnosticsStart", "診斷: 正在從 {0} 個檔案搜尋 Shader 以外的馬賽克候選資產..."},
            {"LogDiagnosticFinding", "  -> 可能的 2D/烘焙馬賽克資產: {0} '{1}' (pathId:{2}, file:{3})"},
            {"LogDiagnosticsNone", "診斷: 找不到像馬賽克的 Texture2D/Sprite/Material 名稱。"},
            {"LogDiagnosticsMore", "  ... 另有 {0} 個診斷候選。"},
            {"LogDiagnosticsSummary", "診斷完成: 發現 {0} 個 Shader 以外的馬賽克候選資產。若馬賽克已烘焙到 Texture/Sprite，Shader colMask 編輯可能不會改變外觀。"},
            {"OK", "OK"},
            {"Cancel", "取消"}
        }

        ' Simplified Chinese
        dicts("zh-Hans") = New Dictionary(Of String, String) From {
            {"Title", "去马赛克工具 (colMask) - VB.net"},
            {"Path", "路径:"},
            {"ScanAll", "全部扫描"},
            {"BackupFile", "备份文件"},
            {"StartDemosaic", "开始去马赛克"},
            {"AvailableShaders", "可用的着色器"},
            {"SelectedShaders", "已选的着色器"},
            {"ColName", "名称"},
            {"ColPathID", "PathID"},
            {"ColFile", "文件"},
            {"ColStatus", "状态"},
            {"MsgSelectPath", "请选择路径。"},
            {"MsgNoSelection", "未选择要处理的着色器。"},
            {"MsgComplete", "处理完成。"},
            {"LogScanStart", "正在扫描 {0} 个文件..."},
            {"LogScanComplete", "扫描完成。"},
            {"LogApkDetected", "检测到APK模式。正在处理: {0}"},
            {"LogExtracting", "正在解压APK..."},
            {"LogNormalMode", "检测到普通模式。"},
            {"LogReplacing", "正在替换着色器: {0}"},
            {"LogSaved", "已保存 -> {0}"},
            {"LogBackup", "正在备份原始文件到 {0}"},
            {"LogRebuilding", "正在重建APK (已修改 {0} 个文件)..."},
            {"LogSigning", "正在对APK进行签名..."},
            {"LogSignedApk", "已保存已签名的 APK: {0}"},
            {"LogError", "错误: {0}"},
            {"Log", "日志"},
            {"ReplaceWith", "替换为..."},
            {"CopyInfo", "复制信息"},
            {"SelectSourceShader", "选择源着色器"},
            {"TargetShaderInfo", "目标着色器（将被替换）"},
            {"Keywords", "关键字"},
            {"KeywordSettings", "马赛克判断关键字"},
            {"KeywordList", "自动选择用的着色器名称关键字"},
            {"AddKeyword", "添加"},
            {"DeleteKeyword", "删除"},
            {"RestoreDefaults", "默认"},
            {"Diagnose", "诊断"},
            {"RestoreBackup", "还原"},
            {"DryRunTitle", "写入前确认"},
            {"DryRunHeader", "已选着色器: {0} / 目标文件: {1}"},
            {"DryRunOperations", "colMask 编辑: {0} / 着色器替换: {1}"},
            {"DryRunWriters", "raw patch 预定文件: {0} / 结构保存预定文件: {1}"},
            {"DryRunBackup", "备份: {0}"},
            {"DryRunTargets", "目标:"},
            {"DryRunContinue", "要按此内容开始写入吗？"},
            {"On", "开"},
            {"Off", "关"},
            {"MsgNoBackups", "此路径没有可还原的 .bak 文件。"},
            {"RestoreConfirm", "要将 {0} 个 .bak 文件还原覆盖到原始文件吗？"},
            {"MsgRestoreComplete", "还原完成。已还原 {0} 个文件。"},
            {"LogRestored", "已从备份还原 -> {0}"},
            {"LogDiagnosticsStart", "诊断: 正在从 {0} 个文件搜索 Shader 以外的马赛克候选资产..."},
            {"LogDiagnosticFinding", "  -> 可能的 2D/烘焙马赛克资产: {0} '{1}' (pathId:{2}, file:{3})"},
            {"LogDiagnosticsNone", "诊断: 未找到像马赛克的 Texture2D/Sprite/Material 名称。"},
            {"LogDiagnosticsMore", "  ... 另有 {0} 个诊断候选。"},
            {"LogDiagnosticsSummary", "诊断完成: 发现 {0} 个 Shader 以外的马赛克候选资产。如果马赛克已烘焙到 Texture/Sprite，Shader colMask 编辑可能不会改变外观。"},
            {"OK", "OK"},
            {"Cancel", "取消"}
        }

        ' Korean
        dicts("ko") = New Dictionary(Of String, String) From {
            {"Title", "모자이크 제거 도구 (colMask) - VB.net"},
            {"Path", "경로:"},
            {"ScanAll", "전체 스캔"},
            {"BackupFile", "백업 파일"},
            {"StartDemosaic", "모자이크 제거 시작"},
            {"AvailableShaders", "사용 가능한 셰이더"},
            {"SelectedShaders", "선택된 셰이더"},
            {"ColName", "이름"},
            {"ColPathID", "PathID"},
            {"ColFile", "파일"},
            {"ColStatus", "상태"},
            {"MsgSelectPath", "경로를 선택해주세요."},
            {"MsgNoSelection", "처리할 셰이더가 선택되지 않았습니다."},
            {"MsgComplete", "처리가 완료되었습니다."},
            {"LogScanStart", "{0}개의 파일을 스캔 중..."},
            {"LogScanComplete", "스캔 완료."},
            {"LogApkDetected", "APK 모드 감지됨. 처리 중: {0}"},
            {"LogExtracting", "APK 압축 해제 중..."},
            {"LogNormalMode", "일반 모드 감지됨."},
            {"LogReplacing", "셰이더 교체 중: {0}"},
            {"LogSaved", "저장됨 -> {0}"},
            {"LogBackup", "원본 파일을 {0}에 백업 중"},
            {"LogRebuilding", "{0}개의 수정된 파일로 APK 재빌드 중..."},
            {"LogSigning", "디버그 키로 APK 서명 중..."},
            {"LogSignedApk", "서명된 APK 저장됨: {0}"},
            {"LogError", "오류: {0}"},
            {"Log", "로그"},
            {"ReplaceWith", "다음으로 교체..."},
            {"CopyInfo", "정보 복사"},
            {"SelectSourceShader", "소스 셰이더 선택"},
            {"TargetShaderInfo", "대상 셰이더 (교체될 셰이더)"},
            {"Keywords", "키워드"},
            {"KeywordSettings", "모자이크 판정 키워드"},
            {"KeywordList", "자동 선택에 사용할 셰이더 이름 키워드"},
            {"AddKeyword", "추가"},
            {"DeleteKeyword", "삭제"},
            {"RestoreDefaults", "기본값"},
            {"Diagnose", "진단"},
            {"RestoreBackup", "복원"},
            {"DryRunTitle", "쓰기 전 확인"},
            {"DryRunHeader", "선택된 셰이더: {0} / 대상 파일: {1}"},
            {"DryRunOperations", "colMask 편집: {0} / 셰이더 교체: {1}"},
            {"DryRunWriters", "raw patch 예정 파일: {0} / 구조 저장 예정 파일: {1}"},
            {"DryRunBackup", "백업: {0}"},
            {"DryRunTargets", "대상:"},
            {"DryRunContinue", "이 내용으로 쓰기를 시작할까요?"},
            {"On", "켜짐"},
            {"Off", "꺼짐"},
            {"MsgNoBackups", "이 경로에서 복원할 수 있는 .bak 파일을 찾지 못했습니다."},
            {"RestoreConfirm", "{0}개의 .bak 파일을 원본 파일에 덮어써서 복원할까요?"},
            {"MsgRestoreComplete", "복원이 완료되었습니다. {0}개 파일을 복원했습니다."},
            {"LogRestored", "백업에서 복원됨 -> {0}"},
            {"LogDiagnosticsStart", "진단: Shader 이외의 모자이크 후보 에셋을 {0}개 파일에서 검색 중..."},
            {"LogDiagnosticFinding", "  -> 가능한 2D/베이크된 모자이크 에셋: {0} '{1}' (pathId:{2}, file:{3})"},
            {"LogDiagnosticsNone", "진단: 모자이크로 보이는 Texture2D/Sprite/Material 이름을 찾지 못했습니다."},
            {"LogDiagnosticsMore", "  ... 추가 진단 후보 {0}개."},
            {"LogDiagnosticsSummary", "진단 완료: Shader 이외의 모자이크 후보 에셋 {0}개를 찾았습니다. Texture/Sprite에 이미 베이크된 경우 Shader colMask 편집으로 외형이 바뀌지 않을 수 있습니다."},
            {"OK", "OK"},
            {"Cancel", "취소"}
        }
    End Sub
End Class
