Imports System.IO
Imports System.Text.Json
Imports System.Collections.Generic

Public Class Localization
    Public Property CurrentLang As String = "en"

    Private ReadOnly configPath As String = "config.json"
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
            {"OK", "OK"},
            {"Cancel", "취소"}
        }
    End Sub
End Class
